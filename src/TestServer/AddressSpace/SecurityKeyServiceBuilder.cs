using Opc.Ua;
using TestServer.Configuration;
using TestServer.Server;

namespace TestServer.AddressSpace;

/// <summary>
/// Exposes a minimal Security Key Service (Part 14 §8.4) so that
/// subscriber-side clients (e.g. php-opcua/opcua-client-ext-pubsub's
/// SksGroupKeyProvider) can exercise the full GetSecurityKeys RPC
/// against a real OPC UA server rather than only a mock client.
///
/// A single security group is configured from environment variables:
///   OPCUA_SKS_GROUP_ID, OPCUA_SKS_POLICY_URI, OPCUA_SKS_TOKEN_ID,
///   OPCUA_SKS_SIGNING_KEY_HEX, OPCUA_SKS_ENCRYPTING_KEY_HEX,
///   OPCUA_SKS_KEY_NONCE_HEX, OPCUA_SKS_KEY_LIFETIME_MS,
///   OPCUA_SKS_TIME_TO_NEXT_KEY_MS.
///
/// GetSecurityKeys signature mirrors Part 14 §8.4.2:
///   in  securityGroupId   : String
///   in  startingTokenId   : UInt32
///   in  requestedKeyCount : UInt32
///   out securityPolicyUri : String
///   out firstTokenId      : UInt32
///   out keys              : ByteString[]
///   out timeToNextKey     : Duration (Double, ms)
///   out keyLifetime       : Duration (Double, ms)
///
/// This is a test-only implementation. Real SKS deployments should
/// authenticate the caller, scope keys per security group, rotate on
/// schedule, and revoke compromised tokens; none of that is done here.
/// </summary>
public class SecurityKeyServiceBuilder
{
    private readonly TestNodeManager _mgr;
    private readonly FolderState _root;
    private readonly ISystemContext _context;
    private readonly ServerConfig _config;

    public SecurityKeyServiceBuilder(TestNodeManager mgr, FolderState root, ISystemContext context, ServerConfig config)
    {
        _mgr = mgr;
        _root = root;
        _context = context;
        _config = config;
    }

    public void Build()
    {
        var folder = _mgr.CreateFolder(_root, "TestServer/SecurityKeyService", "SecurityKeyService");

        var signingKey = HexToBytes(_config.SksSigningKeyHex);
        var encryptingKey = HexToBytes(_config.SksEncryptingKeyHex);
        var keyNonce = HexToBytes(_config.SksKeyNonceHex);

        _mgr.CreateMethod(
            folder,
            "TestServer/SecurityKeyService/GetSecurityKeys",
            "GetSecurityKeys",
            (input, output) =>
            {
                var requestedGroupId = input[0] as string ?? string.Empty;
                var requestedKeyCount = input.Count > 2 ? Convert.ToUInt32(input[2]) : 1u;

                if (!string.Equals(requestedGroupId, _config.SksGroupId, StringComparison.Ordinal))
                {
                    throw new ServiceResultException(
                        StatusCodes.BadNotFound,
                        $"Unknown securityGroupId '{requestedGroupId}'");
                }

                var keyBlob = ConcatenateKey(signingKey, encryptingKey, keyNonce);
                var keyCount = Math.Max(1, Math.Min((int)requestedKeyCount, 8));
                var keys = new byte[keyCount][];
                for (var i = 0; i < keyCount; i++)
                {
                    keys[i] = (byte[])keyBlob.Clone();
                }

                output[0] = _config.SksPolicyUri;
                output[1] = (uint)_config.SksTokenId;
                output[2] = keys;
                output[3] = _config.SksTimeToNextKeyMs;
                output[4] = _config.SksKeyLifetimeMs;
            },
            new[]
            {
                Arg("securityGroupId", DataTypeIds.String, "Identifier of the security group"),
                Arg("startingTokenId", DataTypeIds.UInt32, "First token id the subscriber already has (0 = request current)"),
                Arg("requestedKeyCount", DataTypeIds.UInt32, "Number of keys requested, including current"),
            },
            new[]
            {
                Arg("securityPolicyUri", DataTypeIds.String, "PubSub security policy URI"),
                Arg("firstTokenId", DataTypeIds.UInt32, "Token id of the first key returned (current key)"),
                ArgArray("keys", DataTypeIds.ByteString, "Current + optional future keys"),
                Arg("timeToNextKey", DataTypeIds.Duration, "Milliseconds until the next key takes over"),
                Arg("keyLifetime", DataTypeIds.Duration, "Total lifetime of one key (milliseconds)"),
            });
    }

    private static byte[] ConcatenateKey(byte[] signing, byte[] encrypting, byte[] nonce)
    {
        var buffer = new byte[signing.Length + encrypting.Length + nonce.Length];
        Buffer.BlockCopy(signing, 0, buffer, 0, signing.Length);
        Buffer.BlockCopy(encrypting, 0, buffer, signing.Length, encrypting.Length);
        Buffer.BlockCopy(nonce, 0, buffer, signing.Length + encrypting.Length, nonce.Length);

        return buffer;
    }

    private static byte[] HexToBytes(string hex)
    {
        if (string.IsNullOrEmpty(hex))
        {
            return [];
        }

        if (hex.Length % 2 != 0)
        {
            throw new ArgumentException($"Hex string has odd length: {hex}");
        }

        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }

        return bytes;
    }

    private static Argument Arg(string name, NodeId dataType, string description)
    {
        return new Argument
        {
            Name = name,
            DataType = dataType,
            ValueRank = ValueRanks.Scalar,
            Description = new LocalizedText("en", description),
        };
    }

    private static Argument ArgArray(string name, NodeId dataType, string description)
    {
        return new Argument
        {
            Name = name,
            DataType = dataType,
            ValueRank = ValueRanks.OneDimension,
            Description = new LocalizedText("en", description),
        };
    }
}

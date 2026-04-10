using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using TestServer.Configuration;
using TestServer.Server;

namespace TestServer;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Health check: just exit 0 if --health flag is passed
        if (args.Length > 0 && args[0] == "--health")
        {
            return 0;
        }

        var config = ServerConfig.FromEnvironment();

        Console.WriteLine($"=== OPC UA .NET Standard Test Server ===");
        Console.WriteLine($"Server Name: {config.ServerName}");
        Console.WriteLine($"Port: {config.Port}");
        Console.WriteLine($"Security Policies: {string.Join(", ", config.SecurityPolicies)}");
        Console.WriteLine($"Security Modes: {string.Join(", ", config.SecurityModes)}");
        Console.WriteLine($"Allow Anonymous: {config.AllowAnonymous}");
        Console.WriteLine($"Auth Users: {config.AuthUsers}");
        Console.WriteLine($"Auth Certificate: {config.AuthCertificate}");
        Console.WriteLine($"Auto Accept Certs: {config.AutoAcceptCerts}");

        try
        {
            var application = new ApplicationInstance
            {
                ApplicationName = config.ServerName,
                ApplicationType = ApplicationType.Server,
                ConfigSectionName = "TestServer"
            };

            SetupPkiDirectories(config);
            var appConfig = await CreateApplicationConfiguration(config);
            application.ApplicationConfiguration = appConfig;

            RegisterEccCertificateTypes(config, appConfig);
            await application.CheckApplicationInstanceCertificates(false);

            var server = new TestServerApp(config);
            await application.StartAsync(server);

            Console.WriteLine($"Server started at opc.tcp://0.0.0.0:{config.Port}{config.ResourcePath}");
            Console.WriteLine("Press Ctrl+C to stop...");

            var exitEvent = new ManualResetEvent(false);
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                exitEvent.Set();
            };

            exitEvent.WaitOne();
            Console.WriteLine("Shutting down...");
            await server.StopAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            var inner = ex;
            while (inner.InnerException != null)
            {
                inner = inner.InnerException;
                Console.Error.WriteLine($"  Inner: {inner.Message}");
            }
            Console.Error.WriteLine(inner.StackTrace);
            return 1;
        }
    }

    private static void SetupPkiDirectories(ServerConfig config)
    {
        // Clean stale auto-generated certificates from previous runs
        // (prevents "certificate is invalid" errors after container stop/start)
        if (Directory.Exists("/tmp/pki/own"))
        {
            Directory.Delete("/tmp/pki/own", true);
        }

        // Create writable PKI directories for UA-.NETStandard cert store
        var dirs = new[]
        {
            "/tmp/pki/own/certs", "/tmp/pki/own/private",
            "/tmp/pki/trusted/certs", "/tmp/pki/trusted/crl",
            "/tmp/pki/issuers/certs", "/tmp/pki/issuers/crl",
            "/tmp/pki/rejected/certs"
        };
        foreach (var dir in dirs)
        {
            Directory.CreateDirectory(dir);
        }

        // NOTE: Do NOT copy pre-generated server certs to own store.
        // UA-.NETStandard SDK auto-generates a server cert via CheckApplicationInstanceCertificates()
        // with correct ApplicationUri, SubjectName, and properly stored private key.
        // We only set up trusted/issuer certs here.
        var certDir = Path.GetDirectoryName(config.CertificateFile) ?? "/app/certs/server";

        // Copy trusted client certs
        if (Directory.Exists(config.TrustedCertsDir))
        {
            foreach (var f in Directory.GetFiles(config.TrustedCertsDir, "*.der"))
                CopyIfExists(f, "/tmp/pki/trusted/certs/");
            foreach (var f in Directory.GetFiles(config.TrustedCertsDir, "*.pem"))
                CopyIfExists(f, "/tmp/pki/trusted/certs/");
        }

        // Copy CA certs to issuers
        if (File.Exists(config.CaCertFile))
        {
            CopyIfExists(config.CaCertFile, "/tmp/pki/issuers/certs/");
            var caDir = Path.GetDirectoryName(config.CaCertFile) ?? "/app/certs/ca";
            CopyIfExists(Path.Combine(caDir, "ca-cert.der"), "/tmp/pki/issuers/certs/");
        }

        Console.WriteLine("PKI directories initialized");
    }

    private static void CopyIfExists(string source, string destDir)
    {
        if (!File.Exists(source)) return;
        var destFile = Path.Combine(destDir, Path.GetFileName(source));
        File.Copy(source, destFile, true);
    }

    private static readonly Dictionary<string, NodeId> EccPolicyToCertificateType = new()
    {
        ["ECC_nistP256"] = ObjectTypeIds.EccNistP256ApplicationCertificateType,
        ["ECC_nistP384"] = ObjectTypeIds.EccNistP384ApplicationCertificateType,
        ["ECC_brainpoolP256r1"] = ObjectTypeIds.EccBrainpoolP256r1ApplicationCertificateType,
        ["ECC_brainpoolP384r1"] = ObjectTypeIds.EccBrainpoolP384r1ApplicationCertificateType,
    };

    private static void RegisterEccCertificateTypes(ServerConfig config, ApplicationConfiguration appConfig)
    {
        var addedTypes = new HashSet<NodeId>();

        foreach (var policy in config.SecurityPolicies)
        {
            if (!EccPolicyToCertificateType.TryGetValue(policy, out var certType))
                continue;

            if (!addedTypes.Add(certType))
                continue;

            var certId = new CertificateIdentifier
            {
                StoreType = CertificateStoreType.Directory,
                StorePath = "/tmp/pki/own",
                SubjectName = config.ServerName,
                CertificateType = certType,
            };

            appConfig.SecurityConfiguration.ApplicationCertificates.Add(certId);
            Console.WriteLine($"Registered ECC certificate type for policy {policy}");
        }
    }

    private static async Task<ApplicationConfiguration> CreateApplicationConfiguration(ServerConfig config)
    {
        var certFile = config.CertificateFile;
        var keyFile = config.PrivateKeyFile;

        var securityPolicies = new ServerSecurityPolicyCollection();
        foreach (var policy in config.SecurityPolicies)
        {
            foreach (var mode in config.SecurityModes)
            {
                if (policy == "None" && mode != "None") continue;
                if (policy != "None" && mode == "None") continue;

                var policyUri = policy switch
                {
                    "None" => SecurityPolicies.None,
                    "Basic128Rsa15" => SecurityPolicies.Basic128Rsa15,
                    "Basic256" => SecurityPolicies.Basic256,
                    "Basic256Sha256" => SecurityPolicies.Basic256Sha256,
                    "Aes128_Sha256_RsaOaep" => SecurityPolicies.Aes128_Sha256_RsaOaep,
                    "Aes256_Sha256_RsaPss" => SecurityPolicies.Aes256_Sha256_RsaPss,
                    "ECC_nistP256" => SecurityPolicies.ECC_nistP256,
                    "ECC_nistP384" => SecurityPolicies.ECC_nistP384,
                    "ECC_brainpoolP256r1" => SecurityPolicies.ECC_brainpoolP256r1,
                    "ECC_brainpoolP384r1" => SecurityPolicies.ECC_brainpoolP384r1,
                    "ECC_curve25519" => SecurityPolicies.ECC_curve25519,
                    "ECC_curve448" => SecurityPolicies.ECC_curve448,
                    _ => SecurityPolicies.None
                };

                var msgMode = mode switch
                {
                    "None" => MessageSecurityMode.None,
                    "Sign" => MessageSecurityMode.Sign,
                    "SignAndEncrypt" => MessageSecurityMode.SignAndEncrypt,
                    _ => MessageSecurityMode.None
                };

                securityPolicies.Add(new ServerSecurityPolicy
                {
                    SecurityMode = msgMode,
                    SecurityPolicyUri = policyUri
                });
            }
        }

        if (securityPolicies.Count == 0)
        {
            securityPolicies.Add(new ServerSecurityPolicy
            {
                SecurityMode = MessageSecurityMode.None,
                SecurityPolicyUri = SecurityPolicies.None
            });
        }

        var userTokenPolicies = new UserTokenPolicyCollection();
        if (config.AllowAnonymous)
        {
            userTokenPolicies.Add(new UserTokenPolicy(UserTokenType.Anonymous));
        }
        if (config.AuthUsers)
        {
            userTokenPolicies.Add(new UserTokenPolicy(UserTokenType.UserName));
        }
        if (config.AuthCertificate)
        {
            userTokenPolicies.Add(new UserTokenPolicy(UserTokenType.Certificate));
        }

        var appConfig = new ApplicationConfiguration
        {
            ApplicationName = config.ServerName,
            ApplicationUri = "urn:opcua:testserver:nodes",
            ProductUri = "urn:opcua:testserver",
            ApplicationType = ApplicationType.Server,

            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = "/tmp/pki/own",
                    SubjectName = config.ServerName
                },
                TrustedIssuerCertificates = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = "/tmp/pki/issuers"
                },
                TrustedPeerCertificates = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = "/tmp/pki/trusted"
                },
                RejectedCertificateStore = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = "/tmp/pki/rejected"
                },
                AutoAcceptUntrustedCertificates = config.AutoAcceptCerts,
                RejectSHA1SignedCertificates = false,
                MinimumCertificateKeySize = 1024
            },

            TransportConfigurations = new TransportConfigurationCollection(),
            TransportQuotas = new TransportQuotas
            {
                OperationTimeout = 120000,
                MaxStringLength = 1048576,
                MaxByteStringLength = 4194304,
                MaxArrayLength = 65535,
                MaxMessageSize = 4194304,
                MaxBufferSize = 65535,
                ChannelLifetime = 300000,
                SecurityTokenLifetime = 3600000
            },

            ServerConfiguration = new ServerConfiguration
            {
                BaseAddresses = { $"opc.tcp://0.0.0.0:{config.Port}{config.ResourcePath}" },
                SecurityPolicies = securityPolicies,
                UserTokenPolicies = userTokenPolicies,
                MinRequestThreadCount = 5,
                MaxRequestThreadCount = 100,
                MaxQueuedRequestCount = 2000,
                MaxSessionCount = config.MaxSessions,
                MaxSubscriptionCount = config.MaxSubscriptions,
                MinPublishingInterval = config.MinPublishingInterval,
                DiagnosticsEnabled = true,
                MaxPublishRequestCount = 20,
                MaxSubscriptionLifetime = 3600000,
                MaxMessageQueueSize = 100,
                MaxNotificationQueueSize = 100,
                MaxNotificationsPerPublish = 1000,
                MaxBrowseContinuationPoints = 100,
                MaxHistoryContinuationPoints = 100
            },

            TraceConfiguration = new TraceConfiguration
            {
                OutputFilePath = null,
                DeleteOnLoad = true,
                TraceMasks = 519
            }
        };

        await appConfig.ValidateAsync(ApplicationType.Server);

        if (config.AutoAcceptCerts)
        {
            appConfig.CertificateValidator.CertificateValidation += (_, e) =>
            {
                e.Accept = true;
            };
        }

        return appConfig;
    }
}

namespace TestServer.Configuration;

public class ServerConfig
{
    // Network
    public int Port { get; set; } = 4840;
    public string Hostname { get; set; } = "0.0.0.0";
    public string ServerName { get; set; } = "OPCUATestServer";
    public string ResourcePath { get; set; } = "/UA/TestServer";

    // Security
    public List<string> SecurityPolicies { get; set; } = new() { "None" };
    public List<string> SecurityModes { get; set; } = new() { "None" };
    public bool AllowAnonymous { get; set; } = true;
    public bool AutoAcceptCerts { get; set; } = false;

    // Authentication
    public bool AuthUsers { get; set; } = false;
    public bool AuthCertificate { get; set; } = false;
    public string UsersFile { get; set; } = "/app/config/users.json";

    // Certificates
    public string CertificateFile { get; set; } = "/app/certs/server/cert.pem";
    public string PrivateKeyFile { get; set; } = "/app/certs/server/key.pem";
    public string TrustedCertsDir { get; set; } = "/app/certs/pki/trusted";
    public string RejectedCertsDir { get; set; } = "/app/certs/pki/rejected";
    public string PkiIssuersDir { get; set; } = "/app/certs/pki/issuers";
    public string CaCertFile { get; set; } = "/app/certs/ca/ca-cert.pem";

    // Session Limits
    public int MaxSessions { get; set; } = 100;
    public int MaxSubscriptions { get; set; } = 100;
    public int MinPublishingInterval { get; set; } = 100;

    // Features
    public bool EnableHistorical { get; set; } = true;
    public bool EnableEvents { get; set; } = true;
    public bool EnableMethods { get; set; } = true;
    public bool EnableDynamic { get; set; } = true;
    public bool EnableStructures { get; set; } = true;
    public bool EnableViews { get; set; } = true;

    // Operation Limits
    public int MaxNodesPerRead { get; set; } = 1000;
    public int MaxNodesPerWrite { get; set; } = 1000;
    public int MaxNodesPerBrowse { get; set; } = 1000;

    // Discovery
    public bool IsDiscovery { get; set; } = false;
    public string? DiscoveryUrl { get; set; }

    public static ServerConfig FromEnvironment()
    {
        var config = new ServerConfig();

        config.Port = GetEnvInt("OPCUA_PORT", config.Port);
        config.Hostname = GetEnv("OPCUA_HOSTNAME", config.Hostname);
        config.ServerName = GetEnv("OPCUA_SERVER_NAME", config.ServerName);
        config.ResourcePath = GetEnv("OPCUA_RESOURCE_PATH", config.ResourcePath);

        config.SecurityPolicies = GetEnvList("OPCUA_SECURITY_POLICIES", config.SecurityPolicies);
        config.SecurityModes = GetEnvList("OPCUA_SECURITY_MODES", config.SecurityModes);
        config.AllowAnonymous = GetEnvBool("OPCUA_ALLOW_ANONYMOUS", config.AllowAnonymous);
        config.AutoAcceptCerts = GetEnvBool("OPCUA_AUTO_ACCEPT_CERTS", config.AutoAcceptCerts);

        config.AuthUsers = GetEnvBool("OPCUA_AUTH_USERS", config.AuthUsers);
        config.AuthCertificate = GetEnvBool("OPCUA_AUTH_CERTIFICATE", config.AuthCertificate);
        config.UsersFile = GetEnv("OPCUA_USERS_FILE", config.UsersFile);

        config.CertificateFile = GetEnv("OPCUA_CERTIFICATE_FILE", config.CertificateFile);
        config.PrivateKeyFile = GetEnv("OPCUA_PRIVATE_KEY_FILE", config.PrivateKeyFile);
        config.TrustedCertsDir = GetEnv("OPCUA_TRUSTED_CERTS_DIR", config.TrustedCertsDir);
        config.RejectedCertsDir = GetEnv("OPCUA_REJECTED_CERTS_DIR", config.RejectedCertsDir);
        config.PkiIssuersDir = GetEnv("OPCUA_PKI_ISSUERS_DIR", config.PkiIssuersDir);
        config.CaCertFile = GetEnv("OPCUA_CA_CERT_FILE", config.CaCertFile);

        config.MaxSessions = GetEnvInt("OPCUA_MAX_SESSIONS", config.MaxSessions);
        config.MaxSubscriptions = GetEnvInt("OPCUA_MAX_SUBSCRIPTIONS", config.MaxSubscriptions);
        config.MinPublishingInterval = GetEnvInt("OPCUA_MIN_PUBLISHING_INTERVAL", config.MinPublishingInterval);

        config.EnableHistorical = GetEnvBool("OPCUA_ENABLE_HISTORICAL", config.EnableHistorical);
        config.EnableEvents = GetEnvBool("OPCUA_ENABLE_EVENTS", config.EnableEvents);
        config.EnableMethods = GetEnvBool("OPCUA_ENABLE_METHODS", config.EnableMethods);
        config.EnableDynamic = GetEnvBool("OPCUA_ENABLE_DYNAMIC", config.EnableDynamic);
        config.EnableStructures = GetEnvBool("OPCUA_ENABLE_STRUCTURES", config.EnableStructures);
        config.EnableViews = GetEnvBool("OPCUA_ENABLE_VIEWS", config.EnableViews);

        config.MaxNodesPerRead = GetEnvInt("OPCUA_MAX_NODES_PER_READ", config.MaxNodesPerRead);
        config.MaxNodesPerWrite = GetEnvInt("OPCUA_MAX_NODES_PER_WRITE", config.MaxNodesPerWrite);
        config.MaxNodesPerBrowse = GetEnvInt("OPCUA_MAX_NODES_PER_BROWSE", config.MaxNodesPerBrowse);

        config.IsDiscovery = GetEnvBool("OPCUA_IS_DISCOVERY", config.IsDiscovery);
        config.DiscoveryUrl = GetEnv("OPCUA_DISCOVERY_URL", null!);

        return config;
    }

    private static string GetEnv(string key, string defaultValue)
        => Environment.GetEnvironmentVariable(key) ?? defaultValue;

    private static int GetEnvInt(string key, int defaultValue)
        => int.TryParse(Environment.GetEnvironmentVariable(key), out var v) ? v : defaultValue;

    private static bool GetEnvBool(string key, bool defaultValue)
        => bool.TryParse(Environment.GetEnvironmentVariable(key), out var v) ? v : defaultValue;

    private static List<string> GetEnvList(string key, List<string> defaultValue)
    {
        var val = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(val)) return defaultValue;
        return val.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }
}

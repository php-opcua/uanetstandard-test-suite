using System.Security.Cryptography.X509Certificates;
using Opc.Ua;
using Opc.Ua.Server;
using TestServer.Configuration;
using TestServer.UserManagement;

namespace TestServer.Server;

public class TestServerApp : StandardServer
{
    private readonly ServerConfig _config;
    private readonly UserManager _userManager;

    public TestServerApp(ServerConfig config)
    {
        _config = config;
        _userManager = new UserManager();
        _userManager.LoadFromFile(config.UsersFile);
    }

    protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
    {
        var nodeManagers = new List<INodeManager>
        {
            new TestNodeManager(server, configuration, _config, _userManager)
        };

        return new MasterNodeManager(server, configuration, null, nodeManagers.ToArray());
    }

    protected override ServerProperties LoadServerProperties()
    {
        return new ServerProperties
        {
            ManufacturerName = "OPC UA Test Suite",
            ProductName = "UA-.NETStandard Test Server",
            ProductUri = "urn:opcua:testserver",
            SoftwareVersion = Utils.GetAssemblySoftwareVersion(),
            BuildNumber = "1.0.0",
            BuildDate = DateTime.UtcNow
        };
    }

    protected override void OnServerStarting(ApplicationConfiguration configuration)
    {
        base.OnServerStarting(configuration);
    }

    protected override void OnServerStarted(IServerInternal server)
    {
        base.OnServerStarted(server);

        // Register user identity validation via the ImpersonateUser event
        server.SessionManager.ImpersonateUser += SessionManager_ImpersonateUser;
    }

    private void SessionManager_ImpersonateUser(object? sender, ImpersonateEventArgs args)
    {
        if (args.NewIdentity is AnonymousIdentityToken)
        {
            if (!_config.AllowAnonymous)
            {
                args.IdentityValidationError = StatusCodes.BadIdentityTokenRejected;
                return;
            }

            return;
        }

        if (args.NewIdentity is UserNameIdentityToken userNameToken)
        {
            if (!_config.AuthUsers)
            {
                args.IdentityValidationError = StatusCodes.BadIdentityTokenRejected;
                return;
            }

            var username = userNameToken.UserName;
            var passwordBytes = userNameToken.DecryptedPassword;
            var password = System.Text.Encoding.UTF8.GetString(passwordBytes);

            if (!_userManager.ValidateCredentials(username, password))
            {
                args.IdentityValidationError = StatusCodes.BadUserAccessDenied;
                return;
            }

            Console.WriteLine($"User '{username}' authenticated (role: {_userManager.GetRole(username)})");
            return;
        }

        if (args.NewIdentity is X509IdentityToken x509Token)
        {
            if (!_config.AuthCertificate)
            {
                args.IdentityValidationError = StatusCodes.BadIdentityTokenRejected;
                return;
            }

            Console.WriteLine($"Certificate user authenticated: {x509Token.Certificate?.Subject}");
            return;
        }

        args.IdentityValidationError = StatusCodes.BadIdentityTokenInvalid;
    }
}

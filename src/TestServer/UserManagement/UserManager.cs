using System.Text.Json;

namespace TestServer.UserManagement;

public record UserAccount(string Username, string Password, string Role, List<string> Permissions);

public class UserManager
{
    private readonly Dictionary<string, UserAccount> _users = new(StringComparer.OrdinalIgnoreCase);

    public void LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Users file not found: {filePath}, using defaults");
            LoadDefaults();
            return;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var users = JsonSerializer.Deserialize<List<UserAccountJson>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (users == null)
            {
                LoadDefaults();
                return;
            }

            foreach (var u in users)
            {
                _users[u.Username] = new UserAccount(
                    u.Username, u.Password, u.Role, u.Permissions ?? new List<string>());
            }

            Console.WriteLine($"Loaded {_users.Count} users from {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading users: {ex.Message}, using defaults");
            LoadDefaults();
        }
    }

    private void LoadDefaults()
    {
        _users["admin"] = new UserAccount("admin", "admin123", "admin",
            new List<string> { "AuthenticatedUser", "ConfigureAdmin", "SecurityAdmin", "Operator", "Engineer" });
        _users["operator"] = new UserAccount("operator", "operator123", "operator",
            new List<string> { "AuthenticatedUser", "Operator" });
        _users["viewer"] = new UserAccount("viewer", "viewer123", "viewer",
            new List<string> { "AuthenticatedUser" });
        _users["test"] = new UserAccount("test", "test", "admin",
            new List<string> { "AuthenticatedUser", "ConfigureAdmin", "SecurityAdmin", "Operator", "Engineer" });

        Console.WriteLine($"Loaded {_users.Count} default users");
    }

    public bool ValidateCredentials(string username, string password)
    {
        return _users.TryGetValue(username, out var user) && user.Password == password;
    }

    public UserAccount? GetUser(string username)
    {
        _users.TryGetValue(username, out var user);
        return user;
    }

    public string GetRole(string username)
    {
        return _users.TryGetValue(username, out var user) ? user.Role : "anonymous";
    }

    public bool HasPermission(string username, string permission)
    {
        if (!_users.TryGetValue(username, out var user)) return false;
        return user.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }

    public bool IsAdmin(string username) => GetRole(username) == "admin";
    public bool IsOperator(string username) => GetRole(username) is "admin" or "operator";

    private record UserAccountJson(string Username, string Password, string Role, List<string>? Permissions);
}

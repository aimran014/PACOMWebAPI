namespace PACOM.WebApp.Model
{
    public class LoginModel
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
    public class UserInfo
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public int OrganizationId { get; set; }
        public string? Role { get; set; }
    }

    public static class FakeUsers
    {
        public static List<UserInfo> Users = new()
    {
        new UserInfo { Username = "admin", Password = "123", OrganizationId = 0, Role = "Admin" },
        new UserInfo { Username = "john", Password = "123", OrganizationId = 1, Role = "Tenant" },
        new UserInfo { Username = "maria", Password = "123", OrganizationId = 2, Role = "Tenant" }
    };
    }
}

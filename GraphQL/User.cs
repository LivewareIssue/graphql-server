using HotChocolate.Authorization;

namespace Server.GraphQL;

public class User
{
    required public string Id { get; set; }

    public string? UserName { get; set; }

    [Authorize]
    public string? Email { get; set; }

    public IEnumerable<string> Roles { get; set; } = [];
}
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using Server.Entities;

namespace Server.GraphQL;

[Node]
public class User
{
    required public string Id { get; set; }

    public string? UserName { get; set; }

    [Authorize]
    public string? Email { get; set; }

    public IEnumerable<string> Roles { get; set; } = [];

    public static async Task<User> GetAsync(string id, [Service] UserManager<EntUser> userManager)
    {
        var user = await userManager.FindByIdAsync(id)
            ?? throw new Exception("Failed to find user.");

        return new User
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Roles = await userManager.GetRolesAsync(user)
        };
    }
}
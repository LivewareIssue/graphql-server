using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using Server.Entities;

namespace Server.GraphQL;

[Node]
public class User
{
    [GraphQLDescription("The user's unique identifier.")]
    required public string Id { get; set; }

    [GraphQLDescription("The user's non-unique display name.")]
    public string? UserName { get; set; }

    [Authorize]
    [GraphQLDescription("The user's current email address.")]
    public string? Email { get; set; }

    [GraphQLDescription("The roles that the user is a member of.")]
    public IEnumerable<string> Roles { get; set; } = [];

    public static async Task<User> FromEntUserAsync(EntUser user, UserManager<EntUser> userManager)
    {
        return new User
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Roles = await userManager.GetRolesAsync(user)
        };
    }

    public static async Task<User> GetAsync(string id, [Service] UserManager<EntUser> userManager)
    {
        var user = await userManager.FindByIdAsync(id)
            ?? throw new Exception("Failed to find user.");

        return await FromEntUserAsync(user, userManager);
    }
}
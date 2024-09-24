using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Server.Entities;

namespace Server.GraphQL;

public class Query
{
    public async ValueTask<User> User(string id, UserManager<EntUser> userManager)
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

    public async Task<User?> Viewer(UserManager<EntUser> userManager, ClaimsPrincipal claimsPrincipal)
    {
        var user = await userManager.GetUserAsync(claimsPrincipal);
        if (user == null)
        {
            return null;
        }

        return new User
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Roles = await userManager.GetRolesAsync(user)
        };
    }
}
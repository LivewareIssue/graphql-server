using System.Security.Claims;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using Server.Entities;

namespace Server.GraphQL;

public class Query
{
    [Authorize(Roles = ["Admin"])]
    public async ValueTask<User> User([ID] string id, UserManager<EntUser> userManager)
        => await GraphQL.User.GetAsync(id, userManager);

    [AllowAnonymous]
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
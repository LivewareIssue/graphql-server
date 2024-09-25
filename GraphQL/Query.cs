using System.Security.Claims;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using Server.Entities;

namespace Server.GraphQL;

public record ViewerContext(EntUser User);

public class Query(ViewerContext? viewerContext)
{
    public Query() : this(default) { }

    [Authorize(Roles = ["Admin"])]
    [GraphQLDescription("Lookup a user by their ID.")]
    public async ValueTask<User> User([ID] string id, UserManager<EntUser> userManager)
        => await GraphQL.User.GetAsync(id, userManager);

    [AllowAnonymous]
    [GraphQLDescription("The currently authenticated user.")]
    public async Task<User?> Viewer(UserManager<EntUser> userManager, ClaimsPrincipal claimsPrincipal)
    {
        var user = viewerContext?.User ?? await userManager.GetUserAsync(claimsPrincipal);
        if (user == null)
        {
            return null;
        }

        return await GraphQL.User.FromEntUserAsync(user, userManager);
    }
}
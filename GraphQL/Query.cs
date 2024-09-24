using System.Security.Claims;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using Server.Entities;

namespace Server.GraphQL;

public class Query
{
    [Authorize(Roles = ["Admin"])]
    [GraphQLDescription("Find a user by their ID.")]
    public async ValueTask<User> User(
        [GraphQLDescription("The user's ID.")]
        [ID]
        string id,
        UserManager<EntUser> userManager)
        => await GraphQL.User.GetAsync(id, userManager);

    [AllowAnonymous]
    [GraphQLDescription("The currently authenticated user.")]
    public async Task<User?> Viewer(
        [Service] UserManager<EntUser> userManager,
        [Service] ClaimsPrincipal claimsPrincipal)
    {
        var user = await userManager.GetUserAsync(claimsPrincipal);
        if (user == null)
        {
            return null;
        }

        return await GraphQL.User.FromEntUserAsync(user, userManager);
    }
}
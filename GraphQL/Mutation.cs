using System.Security.Claims;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Entities;
using Server.Services;

namespace Server.GraphQL;

public class Mutation
{
    [GraphQLDescription("Sign in using an email and password.")]
    [AllowAnonymous]
    public async Task<SignInResult> SignInAsync(
        [GraphQLDescription("The user's email address.")]
        string email,
        [GraphQLDescription("The user's password.")]
        string password,
        [Service]
        ILogger<Mutation> logger,
        [Service]
        AuthenticationService authenticationService,
        [Service]
        UserManager<EntUser> userManager,
        [Service]
        IHttpContextAccessor contextAccessor)
    {
        var user = await userManager.FindByEmailAsync(email)
            ?? throw new Exception("Failed to find user.");
        
        if (!await userManager.CheckPasswordAsync(user, password))
        {
            throw new Exception("Incorrect password.");
        }

        var token = await authenticationService.GetTokenAsync(user);

        return new SignInResult
        {
            Token = token,
            Query = new Query(new ViewerContext(user.Id))
        };
    }
}
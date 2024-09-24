using Microsoft.AspNetCore.Identity;
using Server.Entities;
using Server.Services;

namespace Server.GraphQL;

public class Mutation
{
    [GraphQLDescription("Sign in using an email and password.")]
    public async Task<SignInResult> SignInAsync(
        [GraphQLDescription("The user's email address.")]
        string email,
        [GraphQLDescription("The user's password.")]
        string password,
        [Service]
        AuthenticationService authenticationService,
        [Service]
        UserManager<EntUser> userManager)
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
            Viewer = await User.FromEntUserAsync(user, userManager),
            Token = token
        };
    }
}
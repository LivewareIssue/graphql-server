using Microsoft.AspNetCore.Identity;
using Server.Entities;
using Server.Services;

namespace Server.GraphQL;

public class Mutation
{
    public async Task<SignInResult> SignInAsync(
        string email,
        string password,
        [Service] AuthenticationService authenticationService,
        [Service] UserManager<EntUser> userManager)
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
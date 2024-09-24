using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Server.Entities;

namespace Server.Services;

public class JwtOptions
{
    required public string ValidIssuer { get; set; }
    required public string ValidAudience { get; set; }
    required public string PrivateKey { get; set; }
}

public class AuthenticationService(IOptions<JwtOptions> jwtOptions, UserManager<EntUser> userManager)
{
    public async IAsyncEnumerable<Claim> GetClaims(EntUser user)
        { 
        yield return new (JwtRegisteredClaimNames.UniqueName, user.Id);
        yield return new (JwtRegisteredClaimNames.NameId, user.Id);
        yield return new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString());
        
        if (user.Email is not null)
            yield return new (JwtRegisteredClaimNames.Email, user.Email);

        if (user.UserName is not null)
            yield return new(JwtRegisteredClaimNames.GivenName, user.UserName);

        foreach (var role in await userManager.GetRolesAsync(user))
        {
            yield return new Claim(ClaimTypes.Role, role);
        }
    }

    public static SymmetricSecurityKey GetSecurityKey(JwtOptions jwtOptions)
        => new(Encoding.UTF8.GetBytes(jwtOptions.PrivateKey));
    

    public static SigningCredentials GetSigningCredentials(JwtOptions jwtOptions)
        => new(GetSecurityKey(jwtOptions), SecurityAlgorithms.HmacSha256);
    
    private readonly SigningCredentials _signingCredentials = GetSigningCredentials(jwtOptions.Value);
    public async Task<string> GetTokenAsync(EntUser user)
    {
        var claims = await GetClaims(user).ToListAsync();

        var token = new JwtSecurityToken(
            issuer: "issuer",
            audience: "audience",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(4),
            signingCredentials: _signingCredentials);
    
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
using System.Security.Claims;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using Server.Entities;
using Server.Services;

namespace Server.GraphQL;

public class Query
{
    public Task<Person> Person(string id, [Service] IEntPersonService personService)
        => GraphQL.Person.GetAsync(id, personService);

    public async Task<Person?> Viewer([Service] UserManager<EntPerson> userManager, ClaimsPrincipal user)
    {
        var userId = user.Identity?.Name;
        if (userId is null)
        {
            return null;
        }

        var userEnt = await userManager.FindByIdAsync(userId);
        return userEnt is null ? null : GraphQL.Person.FromEntPerson(userEnt);
    }
}
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Server.Entities;
using Server.Services;

namespace Server.GraphQL;

public class Query
{
    public Task<Person> Person(string id, ClaimsPrincipal claimsPrincipal, [Service] UserManager<EntPerson> userManager, [Service] ILogger<Person> logger, [Service] IEntPersonService personService)
    {
        logger.LogInformation("Viewer name: {name}", claimsPrincipal?.Identity?.Name);
        return GraphQL.Person.GetAsync(id, personService);
    }
}
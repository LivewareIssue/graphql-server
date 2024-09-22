using Server.Services;

namespace Server.GraphQL;

public class Person
{
    required public string Id { get; set; }

    public string? UserName { get; set; }

    public static async Task<Person> GetAsync(string id, [Service] IEntPersonService personService)
    {
        var entPerson = await personService.GetAsync(id);
        return new Person { Id = entPerson.Id, UserName = entPerson.UserName };
    }
}
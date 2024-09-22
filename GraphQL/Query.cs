using Server.Services;

namespace Server.GraphQL;

public class Query
{
    public Task<Person> Person(string id, [Service] IEntPersonService personService)
        => GraphQL.Person.GetAsync(id, personService);
}
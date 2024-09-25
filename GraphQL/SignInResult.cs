namespace Server.GraphQL;

public class SignInResult
{
    [GraphQLDescription("An authentication token.")]
    required public string Token { get; set; }

    required public Query Query { get; set; }
}
namespace Server.GraphQL;

public class SignInResult
{
    [GraphQLDescription("The user that was signed in.")]
    required public User Viewer { get; set; }

    [GraphQLDescription("An authentication token.")]
    required public string Token { get; set; }
}
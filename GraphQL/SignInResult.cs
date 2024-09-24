namespace Server.GraphQL;

public class SignInResult
{
    required public User Viewer { get; set; }
    required public string Token { get; set; }
}
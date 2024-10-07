using System.Security.Claims;
using HotChocolate.Authorization;
using HotChocolate.Data;
using Microsoft.AspNetCore.Identity;
using Server.Entities;
using Server.Services;

namespace Server.GraphQL;

public record ViewerContext(EntUser User);

public class Query(ViewerContext? viewerContext)
{
    public Query() : this(default) { }

    [AllowAnonymous]
    [GraphQLDescription("The currently authenticated user.")]
    [UseProjection]
    [UseFirstOrDefault]
    public IQueryable<EntUser> Viewer(UserManager<EntUser> userManager, ClaimsPrincipal claimsPrincipal)
    {
        var user = viewerContext?.User;
        if (user is not null)
        {
            return new[] { user }.AsQueryable();
        }

        var userId = userManager.GetUserId(claimsPrincipal);
        if (userId is not null)
        {
            return userManager.Users.Where(user => user.Id == userId);
        }

        return Enumerable.Empty<EntUser>().AsQueryable();
    }

    [GraphQLDescription("List all tasks.")]
    [UseProjection]
    public IQueryable<EntTask> Tasks([Service] TaskService taskService)
        => taskService.QueryAll();

    [GraphQLDescription("List all users.")]
    [UseProjection]
    public IQueryable<EntUser> Users([Service] UserManager<EntUser> userManager)
        => userManager.Users;
    
    [GraphQLDescription("List all comments.")]
    [UseProjection]
    public IQueryable<EntComment> Comments([Service] CommentService commentService)
        => commentService.QueryAll();
}
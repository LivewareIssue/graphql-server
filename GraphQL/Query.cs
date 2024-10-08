using System.Security.Claims;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Entities;
using Server.Services;

namespace Server.GraphQL;

public record ViewerContext(string UserId);

public class Query(ViewerContext? viewerContext)
{
    public Query() : this(default) { }

    [AllowAnonymous]
    [UseProjection]
    [UseSingleOrDefault]
    [GraphQLDescription("The currently authenticated user.")]
    public IQueryable<EntUser> Viewer(UserManager<EntUser> userManager, ClaimsPrincipal claimsPrincipal)
    {
        var userId = viewerContext?.UserId ?? userManager.GetUserId(claimsPrincipal);
        if (userId is not null)
        {
            return userManager
                .Users
                .Where(user => user.Id == userId)
                .Include(user => user.Tasks)
                .Include(user => user.Comments);
        }

        return Enumerable.Empty<EntUser>().AsQueryable();
    }

    [GraphQLDescription("List all tasks.")]
    [UseProjection]
    public IQueryable<EntTask> Tasks(
        TaskService taskService)
        => taskService.QueryAll();

    [NodeResolver]
    [UseProjection]
    [UseSingleOrDefault]
    [GraphQLDescription("Lookup a task by its unique identifier.")]
    public IQueryable<EntTask> Task(
        int id,
        TaskService taskService
        ) => taskService
            .QueryAll()
            .Where(task => task.Id == id)
            .Include(task => task.Owner)
            .Include(task => task.Comments);

    [GraphQLDescription("List all users.")]
    [UseProjection]
    public IQueryable<EntUser> Users(
        UserManager<EntUser> userManager)
        => userManager.Users;

    [NodeResolver]
    [UseProjection]
    [UseSingleOrDefault]
    [GraphQLDescription("Lookup a user by their unique identifier.")]
    public IQueryable<EntUser> User(
        string id,
        UserManager<EntUser> userManager) => userManager
            .Users
            .Where(user => user.Id == id)
            .Include(user => user.Tasks)
            .Include(user => user.Comments);
    
    [GraphQLDescription("List all comments.")]
    [UseProjection]
    public IQueryable<EntComment> Comments(
        CommentService commentService)
        => commentService.QueryAll();

    [NodeResolver]
    [UseProjection]
    [UseSingleOrDefault]
    [GraphQLDescription("Lookup a comment by its unique identifier.")]
    public IQueryable<EntComment> Comment(
        int id,
        CommentService commentService) => commentService
            .QueryAll()
            .Where(comment => comment.Id == id)
            .Include(comment => comment.Author);
}
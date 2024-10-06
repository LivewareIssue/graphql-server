using System.Security.Claims;
using HotChocolate.Authorization;
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
    public async Task<EntUser?> Viewer(UserManager<EntUser> userManager, ClaimsPrincipal claimsPrincipal)
        => viewerContext?.User ?? await userManager.GetUserAsync(claimsPrincipal);

    [Authorize(Roles = ["Admin", "Employee"])]
    [GraphQLDescription("Lookup a task by it's ID.")]
    public async ValueTask<EntTask?> EntTask([ID] int id, [Service] TaskService taskService)
        => await taskService.FindByIdAsync(id);
    
    [Authorize(Roles = ["Admin", "Employee"])]
    [GraphQLDescription("Lookup a user by their ID.")]
    public async ValueTask<EntUser?> EntUser([ID] string id, [Service] UserManager<EntUser> userManager)
        => await userManager.FindByIdAsync(id);

    [Authorize(Roles = ["Admin", "Employee"])]
    [GraphQLDescription("Lookup a comment by it's ID.")]
    public async ValueTask<EntComment?> EntComment([ID] int id, [Service] CommentService commentService)
        => await commentService.FindByIdAsync(id);

    [Authorize(Roles = ["Admin", "Employee"])]
    [GraphQLDescription("List all tasks.")]
    public IQueryable<EntTask> Tasks([Service] TaskService taskService)
        => taskService.QueryAll();

    [Authorize(Roles = ["Admin", "Employee"])]
    [GraphQLDescription("List all users.")]
    public IQueryable<EntUser> Users([Service] UserManager<EntUser> userManager)
        => userManager.Users;
    
    [Authorize(Roles = ["Admin", "Employee"])]
    [GraphQLDescription("List all comments.")]
    public IQueryable<EntComment> Comments([Service] CommentService commentService)
        => commentService.QueryAll();
}
using Microsoft.AspNetCore.Identity;
using Server.Services;

namespace Server.Entities;

[Node]
public class EntUser : IdentityUser
{
    [GraphQLIgnore]
    public List<EntTask> Tasks { get; set; } = [];

    [GraphQLDescription("The tasks assigned to this user.")]
    [GraphQLName("tasks")]
    public IQueryable<EntTask> QueryTasks([Service] TaskService taskService)
        => taskService.QueryByOwnerId(Id);

    [GraphQLDescription("This user's comments.")]
    [GraphQLName("comments")]
    public IQueryable<EntComment> QueryComments([Service] CommentService commentService)
        => commentService.QueryByAuthorId(Id);

    [GraphQLDescription("This roles assigned to this user.")]
    public async Task<IList<string>> GetRolesAsync([Service] UserManager<EntUser> userManager)
        => await userManager.GetRolesAsync(this);
}

public class EntUserTypeExtension : ObjectTypeExtension<EntUser>
{
    protected override void Configure(IObjectTypeDescriptor<EntUser> descriptor)
    {
        descriptor
            .Field(user => user.Id)
            .ID(nameof(EntUser.Id))
            .Description("This user's unique identifier.");

        descriptor
            .Field(user => user.UserName)
            .Description("This user's display name.");

        descriptor
            .Field(user => user.Email)
            .Authorize(["Admin", "Employee"])
            .Description("This user's email address.");
        
        descriptor
            .Field(user => user.PhoneNumber)
            .Authorize(["Admin", "Employee"])
            .Description("This user's phone number.");
        
        descriptor.Ignore(user => user.NormalizedUserName);
        descriptor.Ignore(user => user.NormalizedEmail);
        descriptor.Ignore(user => user.EmailConfirmed);
        descriptor.Ignore(user => user.PasswordHash);
        descriptor.Ignore(user => user.SecurityStamp);
        descriptor.Ignore(user => user.ConcurrencyStamp);
        descriptor.Ignore(user => user.PhoneNumberConfirmed);
        descriptor.Ignore(user => user.TwoFactorEnabled);
        descriptor.Ignore(user => user.LockoutEnd);
        descriptor.Ignore(user => user.LockoutEnabled);
        descriptor.Ignore(user => user.AccessFailedCount);
    }
}
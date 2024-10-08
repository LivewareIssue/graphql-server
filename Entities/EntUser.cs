using Microsoft.AspNetCore.Identity;

namespace Server.Entities;

[Node]
public class EntUser : IdentityUser
{
    [GraphQLDescription("The tasks assigned to this user.")]
    public List<EntTask> Tasks { get; set; } = [];

    [GraphQLDescription("The comments made by this user.")]
    public List<EntComment> Comments { get; set; } = [];

    [GraphQLDescription("This roles assigned to this user.")]
    public async Task<IList<string>> GetRolesAsync(UserManager<EntUser> userManager)
        => await userManager.GetRolesAsync(this);
}

public class EntUserTypeExtension : ObjectTypeExtension<EntUser>
{
    protected override void Configure(IObjectTypeDescriptor<EntUser> descriptor)
    {
        descriptor
            .Field(user => user.Id)
            .ID()
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
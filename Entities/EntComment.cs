using Server.Services;

namespace Server.Entities;

[Node]
public class EntComment : IAuditedEntity
{
    [ID]
    [GraphQLDescription("This comment's unique identifier.")]
    public int Id { get; set; }

    [GraphQLDescription("This comment's content.")]
    required public string Content { get; set; }

    [GraphQLDescription("The date and time when this comment was created.")]
    public DateTime CreatedAt { get; set; }

    [GraphQLDescription("The date and time when this comment was last updated.")]
    public DateTime UpdatedAt { get; set; }

    [GraphQLIgnore]
    required public string AuthorId { get; set; }

    [GraphQLIgnore]
    required public EntUser Author { get; set; }

    public static async Task<EntComment?> GetAsync(int id, [Service] CommentService commentService)
    {
        return await commentService.FindByIdAsync(id);
    }
}
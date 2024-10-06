using Server.Services;

namespace Server.Entities;

public enum TaskStatus
{
    Open,
    InProgress,
    Blocked,
    Closed
}

public enum TaskSize { XS, S, M, L, XL}

public enum TaskPriority { Low, Medium, High }

[Node]
public class EntTask : IAuditedEntity
{
    [ID]
    [GraphQLDescription("This task's unique identifier.")]
    public int Id { get; set; }

    [GraphQLDescription("This task's title.")]
    required public string Title { get; set; }

    [GraphQLDescription("This task's content.")]
    required public string Content { get; set; }

    [GraphQLDescription("The current status of this task.")]
    public TaskStatus Status { get; set; }

    [GraphQLDescription("The approximate size of this task.")]
    public TaskSize Size { get; set; }

    [GraphQLDescription("This task's priority.")]
    public TaskPriority Priority { get; set; }

    [GraphQLDescription("The date and time when this task was created.")]
    public DateTime CreatedAt { get; set; }

    [GraphQLDescription("The date and time when this task was last updated.")]
    public DateTime UpdatedAt { get; set; }

    [GraphQLIgnore]
    public string? OwnerId { get; set; }

    [GraphQLIgnore]
    public EntUser? Owner { get; set; }

    [GraphQLIgnore]
    public List<EntComment> Comments { get; set; } = [];

    [GraphQLIgnore]
    public List<EntTask> DependsOn { get; set; } = [];

    public static async Task<EntTask?> GetAsync(int id, [Service] TaskService taskService)
    {
        return await taskService.FindByIdAsync(id);
    }
}
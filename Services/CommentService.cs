
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Entities;

namespace Server.Services;

public class CommentService(ApplicationDbContext dbContext)
{
    public async Task<EntComment?> FindByIdAsync(int id) => await dbContext
        .Comments
        .FindAsync(id);

    public IQueryable<EntComment> QueryAll() => dbContext
        .Comments
        .AsQueryable();

    public IQueryable<EntComment> QueryByAuthorId(string authorId) => dbContext
        .Comments
        .Where(comment => comment.AuthorId == authorId)
        .AsQueryable();

    public IQueryable<EntComment> QueryByTaskId(int taskId) => dbContext
        .Tasks
        .Where(task => task.Id == taskId)
        .SelectMany(task => task.Comments);

    public async Task<EntComment> CreateAsync(EntComment comment)
    {
        await dbContext.Comments.AddAsync(comment);
        await dbContext.SaveChangesAsync();
        return comment;
    }

    public async Task<EntComment> UpdateAsync(EntComment comment)
    {
        dbContext.Comments.Update(comment);
        await dbContext.SaveChangesAsync();
        return comment;
    }

    public async Task DeleteAsync(EntComment comment)
    {
        dbContext.Comments.Remove(comment);
        await dbContext.SaveChangesAsync();
    }
}
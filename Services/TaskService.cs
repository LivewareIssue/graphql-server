
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Entities;

namespace Server.Services;

public class TaskService(ApplicationDbContext dbContext)
{
    public async Task<EntTask?> FindByIdAsync(int id) => await dbContext
        .Tasks
        .FindAsync(id);

    public async Task<EntTask?> FindByTitleAsync(string title) => await dbContext
        .Tasks
        .FirstOrDefaultAsync(task => task.Title == title);

    public IQueryable<EntTask> QueryAll() => dbContext
        .Tasks
        .AsQueryable();

    public IQueryable<EntTask> QueryByOwnerId(string ownerId) => dbContext
        .Tasks
        .Where(task => task.OwnerId == ownerId)
        .AsQueryable();

    public async Task<EntTask> CreateAsync(EntTask task)
    {
        await dbContext.Tasks.AddAsync(task);
        await dbContext.SaveChangesAsync();
        return task;
    }

    public async Task<EntTask> UpdateAsync(EntTask task)
    {
        dbContext.Tasks.Update(task);
        await dbContext.SaveChangesAsync();
        return task;
    }

    public async Task DeleteAsync(EntTask task)
    {
        dbContext.Tasks.Remove(task);
        await dbContext.SaveChangesAsync();
    }
}
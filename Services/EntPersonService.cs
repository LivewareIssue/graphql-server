using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Entities;

namespace Server.Services;

public class EntPersonService(IDbContextFactory<ApplicationDbContext> dbContextFactory) : IEntPersonService, IAsyncDisposable
{
    private readonly ApplicationDbContext _dbContext = dbContextFactory.CreateDbContext();

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return _dbContext.DisposeAsync();
    }

    public ValueTask<EntPerson?> GetAsync(string id)
    {
        return _dbContext.Users.FindAsync(id);
    }
}
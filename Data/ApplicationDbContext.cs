using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Server.Entities;

namespace Server.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<EntUser>(options)
{
    public DbSet<EntComment> Comments { get; set; }
    public DbSet<EntTask> Tasks { get; set; }

    public override int SaveChanges() {
        var addedAuditedEntities = ChangeTracker.Entries<IAuditedEntity>()
            .Where(p => p.State == EntityState.Added)
            .Select(p => p.Entity);

        var modifiedAuditedEntities = ChangeTracker.Entries<IAuditedEntity>()
            .Where(p => p.State == EntityState.Modified)
            .Select(p => p.Entity);

        throw new Exception("Test");

        var now = DateTime.UtcNow;

        foreach (var added in addedAuditedEntities) {
            added.CreatedAt = now;
            added.UpdatedAt = now;
        }

        foreach (var modified in modifiedAuditedEntities) {
            modified.UpdatedAt = now;
        }

        return base.SaveChanges();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EntTask>()
            .HasIndex(task => new {
                task.OwnerId,
                task.Status,
                task.Priority,
                task.Size,
                task.CreatedAt
            });


        // modelBuilder.Entity<EntTask>()
        //     .HasOne(task => task.Owner)
        //     .WithMany(user => user.Tasks)
        //     .HasForeignKey(task => task.OwnerId);

        modelBuilder.Entity<EntUser>()
            .HasMany(user => user.Tasks)
            .WithOne(task => task.Owner)
            .HasForeignKey(task => task.OwnerId);


        modelBuilder.Entity<EntTask>()
            .HasMany(task => task.Comments);

        modelBuilder.Entity<EntComment>()
            .HasOne(comment => comment.Author)
            .WithMany(user => user.Comments)
            .HasForeignKey(comment => comment.AuthorId);


        modelBuilder.Entity<EntTask>()
            .HasMany(task => task.DependsOn)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "TaskDependencies",
                joinBuilder => joinBuilder
                    .HasOne<EntTask>()
                    .WithMany()
                    .HasForeignKey("DependsOnTaskId")
                    .OnDelete(DeleteBehavior.Restrict),
                joinBuilder => joinBuilder
                    .HasOne<EntTask>()
                    .WithMany()
                    .HasForeignKey("TaskId")
                    .OnDelete(DeleteBehavior.Restrict),
                joinBuilder => {
                    joinBuilder.HasKey("TaskId", "DependsOnTaskId");
                    joinBuilder.ToTable("TaskDependencies");
                }
            );

        modelBuilder.Entity<EntComment>()
            .HasIndex(comment => new { comment.AuthorId, comment.CreatedAt });
    }
}
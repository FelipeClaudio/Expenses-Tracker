using Core.Users;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user =>
        {
            user.HasKey(u => u.Id);
            user.HasIndex(u => u.GoogleSubjectId).IsUnique();
            user.Property(u => u.GoogleSubjectId).IsRequired();
            user.Property(u => u.Email).IsRequired();
            user.Property(u => u.DisplayName).IsRequired();
        });
    }
}

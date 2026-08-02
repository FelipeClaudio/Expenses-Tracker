using Core.Topics;
using Core.Users;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Topic> Topics => Set<Topic>();

    public DbSet<TopicMember> TopicMembers => Set<TopicMember>();

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

        modelBuilder.Entity<Topic>(topic =>
        {
            topic.HasKey(t => t.Id);
            topic.Property(t => t.Name).IsRequired();

            // No navigation properties on Topic itself (app logic queries by
            // explicit id, not via EF navigation) - Restrict rather than
            // Cascade for now, since intentional cascade-delete (spec UC-6)
            // is application logic added in the Topic-deletion slice, not a
            // DB-level default.
            topic.HasOne<Topic>()
                .WithMany()
                .HasForeignKey(t => t.ParentTopicId)
                .OnDelete(DeleteBehavior.Restrict);

            topic.HasOne<Topic>()
                .WithMany()
                .HasForeignKey(t => t.RootTopicId)
                .OnDelete(DeleteBehavior.Restrict);

            topic.HasIndex(t => t.InviteCode)
                .IsUnique()
                .HasFilter("\"InviteCode\" IS NOT NULL");
        });

        modelBuilder.Entity<TopicMember>(member =>
        {
            member.HasKey(m => new { m.RootTopicId, m.UserId });

            member.HasOne<Topic>()
                .WithMany()
                .HasForeignKey(m => m.RootTopicId)
                .OnDelete(DeleteBehavior.Restrict);

            member.HasOne<User>()
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

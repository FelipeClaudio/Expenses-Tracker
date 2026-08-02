using Core.Expenses;
using Core.Settlements;
using Core.Topics;
using Core.Users;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Topic> Topics => Set<Topic>();

    public DbSet<TopicMember> TopicMembers => Set<TopicMember>();

    public DbSet<Expense> Expenses => Set<Expense>();

    public DbSet<ExpenseParticipant> ExpenseParticipants => Set<ExpenseParticipant>();

    public DbSet<SettledTransfer> SettledTransfers => Set<SettledTransfer>();

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

        modelBuilder.Entity<Expense>(expense =>
        {
            expense.HasKey(e => e.Id);
            expense.Property(e => e.Description).IsRequired();

            expense.HasOne<Topic>()
                .WithMany()
                .HasForeignKey(e => e.TopicId)
                .OnDelete(DeleteBehavior.Restrict);

            expense.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.PaidByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            expense.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExpenseParticipant>(participant =>
        {
            participant.HasKey(p => new { p.ExpenseId, p.UserId });

            participant.HasOne<Expense>()
                .WithMany()
                .HasForeignKey(p => p.ExpenseId)
                .OnDelete(DeleteBehavior.Restrict);

            participant.HasOne<User>()
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SettledTransfer>(settled =>
        {
            settled.HasKey(s => s.Id);

            settled.HasOne<Topic>()
                .WithMany()
                .HasForeignKey(s => s.RootTopicId)
                .OnDelete(DeleteBehavior.Restrict);

            settled.HasOne<User>()
                .WithMany()
                .HasForeignKey(s => s.FromUserId)
                .OnDelete(DeleteBehavior.Restrict);

            settled.HasOne<User>()
                .WithMany()
                .HasForeignKey(s => s.ToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            settled.HasOne<User>()
                .WithMany()
                .HasForeignKey(s => s.RecordedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

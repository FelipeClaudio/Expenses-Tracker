using Core.Expenses;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class ExpenseRepository(AppDbContext dbContext) : IExpenseRepository
{
    public async Task<Expense> AddAsync(Expense expense, IReadOnlyList<ExpenseParticipant> participants, CancellationToken cancellationToken = default)
    {
        dbContext.Expenses.Add(expense);
        dbContext.ExpenseParticipants.AddRange(participants);
        await dbContext.SaveChangesAsync(cancellationToken);
        return expense;
    }

    public Task<Expense?> FindByIdAsync(Guid expenseId, CancellationToken cancellationToken = default)
    {
        return dbContext.Expenses.SingleOrDefaultAsync(e => e.Id == expenseId, cancellationToken);
    }

    public async Task<IReadOnlyList<ExpenseParticipant>> FindParticipantsAsync(Guid expenseId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ExpenseParticipants
            .Where(p => p.ExpenseId == expenseId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExpenseWithParticipants>> FindByTopicIdsAsync(IReadOnlyList<Guid> topicIds, CancellationToken cancellationToken = default)
    {
        var expenses = await dbContext.Expenses
            .Where(e => topicIds.Contains(e.TopicId))
            .ToListAsync(cancellationToken);

        var expenseIds = expenses.Select(e => e.Id).ToList();
        var allParticipants = await dbContext.ExpenseParticipants
            .Where(p => expenseIds.Contains(p.ExpenseId))
            .ToListAsync(cancellationToken);
        var participantsByExpense = allParticipants.ToLookup(p => p.ExpenseId);

        return expenses
            .Select(e => new ExpenseWithParticipants(e, participantsByExpense[e.Id].ToList()))
            .ToList();
    }

    public Task<int> CountByTopicIdsAsync(IReadOnlyList<Guid> topicIds, CancellationToken cancellationToken = default)
    {
        return dbContext.Expenses.CountAsync(e => topicIds.Contains(e.TopicId), cancellationToken);
    }

    public async Task UpdateAsync(Expense expense, IReadOnlyList<ExpenseParticipant> newParticipants, CancellationToken cancellationToken = default)
    {
        dbContext.Expenses.Update(expense);

        var existingParticipants = await dbContext.ExpenseParticipants
            .Where(p => p.ExpenseId == expense.Id)
            .ToListAsync(cancellationToken);
        dbContext.ExpenseParticipants.RemoveRange(existingParticipants);
        dbContext.ExpenseParticipants.AddRange(newParticipants);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Expense expense, CancellationToken cancellationToken = default)
    {
        var participants = await dbContext.ExpenseParticipants
            .Where(p => p.ExpenseId == expense.Id)
            .ToListAsync(cancellationToken);
        dbContext.ExpenseParticipants.RemoveRange(participants);
        dbContext.Expenses.Remove(expense);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

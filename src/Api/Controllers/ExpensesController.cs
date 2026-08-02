using System.Security.Claims;
using Api.Contracts;
using Core.Expenses;
using Core.Topics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/expenses")]
public sealed class ExpensesController(IExpenseService expenseService, ITopicService topicService) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, [FromBody] EditExpenseRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest("Description is required.");
        }

        if (request.Amount <= 0)
        {
            return BadRequest("Amount must be greater than zero.");
        }

        if (request.ParticipantUserIds.Count == 0)
        {
            return BadRequest("At least one participant is required.");
        }

        var expense = await expenseService.GetByIdAsync(id, cancellationToken);
        if (expense is null)
        {
            return NotFound();
        }

        var topic = await topicService.GetByIdAsync(expense.TopicId, cancellationToken);
        if (topic is null)
        {
            return NotFound();
        }

        // FR-12: any Member of the Topic may edit - no creator restriction
        // (that only applies to delete, FR-12a).
        if (!await topicService.IsMemberAsync(topic, CurrentUserId, cancellationToken))
        {
            return Forbid();
        }

        var involvedUserIds = request.ParticipantUserIds.Append(request.PaidByUserId).Distinct();
        foreach (var userId in involvedUserIds)
        {
            if (!await topicService.IsMemberAsync(topic, userId, cancellationToken))
            {
                return BadRequest($"User {userId} is not a member of this topic and cannot be tagged or set as payer.");
            }
        }

        var result = await expenseService.EditExpenseAsync(
            expense,
            request.Description,
            request.Amount,
            request.PaidByUserId,
            request.ExpenseDate,
            request.ParticipantUserIds,
            cancellationToken);

        return Ok(ToResponse(result));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var expense = await expenseService.GetByIdAsync(id, cancellationToken);
        if (expense is null)
        {
            return NotFound();
        }

        // FR-12a/NFR-12: delete is restricted to the Member who created
        // (logged) this expense - not the payer, unless they're also the
        // one who logged it.
        if (expense.CreatedByUserId != CurrentUserId)
        {
            return Forbid();
        }

        await expenseService.DeleteAsync(expense, cancellationToken);
        return NoContent();
    }

    private static ExpenseResponse ToResponse(ExpenseWithParticipants expenseWithParticipants)
    {
        var (expense, participants) = expenseWithParticipants;
        return new ExpenseResponse(
            expense.Id,
            expense.TopicId,
            expense.Description,
            expense.Amount,
            expense.PaidByUserId,
            expense.ExpenseDate,
            expense.CreatedByUserId,
            expense.CreatedAt,
            participants.Select(p => new ExpenseParticipantResponse(p.UserId, p.ShareAmount)).ToList());
    }
}

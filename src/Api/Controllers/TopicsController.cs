using System.Security.Claims;
using Api.Contracts;
using Core.Expenses;
using Core.Topics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/topics")]
public sealed class TopicsController(ITopicService topicService, IExpenseService expenseService) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTopicRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Name is required.");
        }

        var topic = await topicService.CreateRootTopicAsync(request.Name, request.Description, CurrentUserId, cancellationToken);
        return Ok(ToResponse(topic));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var topic = await topicService.GetByIdAsync(id, cancellationToken);
        if (topic is null)
        {
            return NotFound();
        }

        if (!await topicService.IsMemberAsync(topic, CurrentUserId, cancellationToken))
        {
            return Forbid();
        }

        return Ok(ToResponse(topic));
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Rename(Guid id, [FromBody] RenameTopicRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Name is required.");
        }

        var topic = await topicService.GetByIdAsync(id, cancellationToken);
        if (topic is null)
        {
            return NotFound();
        }

        if (!await topicService.IsMemberAsync(topic, CurrentUserId, cancellationToken))
        {
            return Forbid();
        }

        var renamed = await topicService.RenameAsync(topic, request.Name, cancellationToken);
        return Ok(ToResponse(renamed));
    }

    [HttpGet("{id:guid}/subtopics")]
    public async Task<IActionResult> GetSubtopics(Guid id, CancellationToken cancellationToken)
    {
        var topic = await topicService.GetByIdAsync(id, cancellationToken);
        if (topic is null)
        {
            return NotFound();
        }

        if (!await topicService.IsMemberAsync(topic, CurrentUserId, cancellationToken))
        {
            return Forbid();
        }

        var children = await topicService.GetSubtopicsAsync(id, cancellationToken);
        return Ok(children.Select(ToResponse));
    }

    [HttpPost("{id:guid}/subtopics")]
    public async Task<IActionResult> CreateSubtopic(Guid id, [FromBody] CreateTopicRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Name is required.");
        }

        var parent = await topicService.GetByIdAsync(id, cancellationToken);
        if (parent is null)
        {
            return NotFound();
        }

        if (!await topicService.IsMemberAsync(parent, CurrentUserId, cancellationToken))
        {
            return Forbid();
        }

        var subtopic = await topicService.CreateSubtopicAsync(parent, request.Name, request.Description, CurrentUserId, cancellationToken);
        return Ok(ToResponse(subtopic));
    }

    [HttpPost("{id:guid}/invite/rotate")]
    public async Task<IActionResult> RotateInviteCode(Guid id, CancellationToken cancellationToken)
    {
        var topic = await topicService.GetByIdAsync(id, cancellationToken);
        if (topic is null)
        {
            return NotFound();
        }

        if (!await topicService.IsMemberAsync(topic, CurrentUserId, cancellationToken))
        {
            return Forbid();
        }

        if (!topic.IsRoot)
        {
            return BadRequest("Only a root topic has an invite code to rotate.");
        }

        var newCode = await topicService.RotateInviteCodeAsync(topic, cancellationToken);
        return Ok(new RotateInviteCodeResponse(newCode));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromQuery] bool confirmCascade,
        [FromQuery] string? confirmName,
        CancellationToken cancellationToken)
    {
        var topic = await topicService.GetByIdAsync(id, cancellationToken);
        if (topic is null)
        {
            return NotFound();
        }

        // FR-8a/NFR-12: delete is restricted to the creator of this specific
        // node - stricter than the general membership check used elsewhere.
        if (topic.CreatedByUserId != CurrentUserId)
        {
            return Forbid();
        }

        var descendants = await topicService.GetDescendantsAsync(topic, cancellationToken);
        var allTopicIds = descendants.Select(d => d.Id).Append(topic.Id).ToList();
        var expenseCount = await expenseService.CountByTopicIdsAsync(allTopicIds, cancellationToken);

        if ((descendants.Count > 0 || expenseCount > 0) && !confirmCascade)
        {
            return Conflict(new DeleteTopicWarningResponse(SubtopicCount: descendants.Count, ExpenseCount: expenseCount));
        }

        if (topic.IsRoot && !string.Equals(confirmName, topic.Name, StringComparison.Ordinal))
        {
            return BadRequest("Deleting a root topic requires confirmName (as a query parameter) to exactly match the topic's name.");
        }

        try
        {
            await topicService.DeleteAsync(topic, descendants, cancellationToken);
        }
        catch (TopicDeletionConflictException ex)
        {
            return Conflict(ex.Message);
        }

        return NoContent();
    }

    [HttpGet("{id:guid}/expenses")]
    public async Task<IActionResult> GetExpenses(Guid id, CancellationToken cancellationToken)
    {
        var topic = await topicService.GetByIdAsync(id, cancellationToken);
        if (topic is null)
        {
            return NotFound();
        }

        if (!await topicService.IsMemberAsync(topic, CurrentUserId, cancellationToken))
        {
            return Forbid();
        }

        // UC-13: lists every expense across the whole subtopic tree rooted
        // at this node, not just expenses logged directly on it.
        var descendants = await topicService.GetDescendantsAsync(topic, cancellationToken);
        var topicIds = descendants.Select(d => d.Id).Append(topic.Id).ToList();

        var expenses = await expenseService.GetByTopicIdsAsync(topicIds, cancellationToken);
        return Ok(expenses.Select(ToResponse));
    }

    [HttpPost("{id:guid}/expenses")]
    public async Task<IActionResult> LogExpense(Guid id, [FromBody] LogExpenseRequest request, CancellationToken cancellationToken)
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

        var topic = await topicService.GetByIdAsync(id, cancellationToken);
        if (topic is null)
        {
            return NotFound();
        }

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

        var result = await expenseService.LogExpenseAsync(
            topic,
            request.Description,
            request.Amount,
            request.PaidByUserId,
            request.ExpenseDate,
            request.ParticipantUserIds,
            CurrentUserId,
            cancellationToken);

        return Ok(ToResponse(result));
    }

    [HttpPost("join/{inviteCode}")]
    public async Task<IActionResult> Join(string inviteCode, CancellationToken cancellationToken)
    {
        var topic = await topicService.JoinByInviteCodeAsync(inviteCode, CurrentUserId, cancellationToken);
        if (topic is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(topic));
    }

    private static TopicResponse ToResponse(Topic topic) => new(
        topic.Id,
        topic.ParentTopicId,
        topic.RootTopicId,
        topic.Name,
        topic.Description,
        topic.InviteCode,
        topic.CreatedByUserId,
        topic.CreatedAt);

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

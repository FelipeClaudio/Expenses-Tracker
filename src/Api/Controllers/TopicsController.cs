using System.Security.Claims;
using Api.Contracts;
using Core.Topics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/topics")]
public sealed class TopicsController(ITopicService topicService) : ControllerBase
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

        if (descendants.Count > 0 && !confirmCascade)
        {
            return Conflict(new DeleteTopicWarningResponse(SubtopicCount: descendants.Count, ExpenseCount: 0));
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
}

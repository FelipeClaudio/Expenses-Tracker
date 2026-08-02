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

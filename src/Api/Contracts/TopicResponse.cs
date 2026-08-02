namespace Api.Contracts;

public sealed record TopicResponse(
    Guid Id,
    Guid? ParentTopicId,
    Guid RootTopicId,
    string Name,
    string? Description,
    string? InviteCode,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt);

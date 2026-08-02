namespace Api.Contracts;

public sealed record CreateTopicRequest(string Name, string? Description = null);

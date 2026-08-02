namespace Api.Contracts;

public sealed record AuthenticatedUserResponse(Guid Id, string Email, string DisplayName, string? AvatarUrl);

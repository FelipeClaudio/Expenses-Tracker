namespace Core.Users;

public sealed class User
{
    public required Guid Id { get; init; }

    /// <summary>The Google account's stable subject id ("sub" claim) — the durable identity key (FR-2).</summary>
    public required string GoogleSubjectId { get; init; }

    public required string Email { get; set; }

    public required string DisplayName { get; set; }

    public string? AvatarUrl { get; set; }

    public required DateTimeOffset CreatedAt { get; init; }
}

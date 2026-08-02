namespace Core.Auth;

/// <summary>Wraps <see cref="DateTimeOffset.UtcNow"/> so callers depending on the current time stay unit-testable.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

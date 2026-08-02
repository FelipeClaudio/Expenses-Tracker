namespace Core.Topics;

/// <summary>
/// Thrown when a topic's structure changed concurrently between the caller
/// fetching its descendants and the delete actually committing (e.g. a new
/// subtopic was added under it in the meantime) - the caller should surface
/// this as a conflict and let the client retry with a fresh delete request.
/// </summary>
public sealed class TopicDeletionConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);

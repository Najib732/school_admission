namespace SchoolAdmission.Common;

public class Result<T>
{
    public bool Success { get; }
    public T? Data { get; }
    public string? ErrorMessage { get; }
    public bool IsNotFound { get; }
    public bool IsConflict { get; }

    private Result(bool success, T? data, string? errorMessage, bool isNotFound, bool isConflict)
    {
        Success = success;
        Data = data;
        ErrorMessage = errorMessage;
        IsNotFound = isNotFound;
        IsConflict = isConflict;
    }

    public static Result<T> Ok(T data) => new(true, data, null, false, false);
    public static Result<T> Fail(string errorMessage) => new(false, default, errorMessage, false, false);
    public static Result<T> NotFound(string errorMessage) => new(false, default, errorMessage, true, false);

    // The request is valid, but the current state does not allow it (no seats, already applied, expired).
    public static Result<T> Conflict(string errorMessage) => new(false, default, errorMessage, false, true);
}

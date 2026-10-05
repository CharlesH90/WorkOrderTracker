namespace WorkOrderTracker.Api.Services;

public enum ResultKind
{
    Success,
    NotFound,
    Invalid
}

/// <summary>Outcome of a service call that has no payload.</summary>
public record ServiceResult(ResultKind Kind, IDictionary<string, string[]>? Errors = null)
{
    public static ServiceResult Success() => new(ResultKind.Success);
    public static ServiceResult NotFound() => new(ResultKind.NotFound);
    public static ServiceResult Invalid(IDictionary<string, string[]> errors) => new(ResultKind.Invalid, errors);
}

/// <summary>Outcome of a service call that returns a value on success.</summary>
public record ServiceResult<T>(ResultKind Kind, T? Value = default, IDictionary<string, string[]>? Errors = null)
{
    public static ServiceResult<T> Success(T value) => new(ResultKind.Success, value);
    public static ServiceResult<T> Invalid(IDictionary<string, string[]> errors) => new(ResultKind.Invalid, default, errors);
}

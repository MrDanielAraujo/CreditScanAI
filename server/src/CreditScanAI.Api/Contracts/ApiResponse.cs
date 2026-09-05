namespace CreditScanAI.Api.Contracts;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public ApiError? Error { get; init; }
    public ApiMetadata Metadata { get; init; } = new();

    public static ApiResponse<T> Ok(T data) => new()
    {
        Success = true,
        Data = data
    };

    public static ApiResponse<T> Fail(string code, string message, IDictionary<string, string[]>? details = null) => new()
    {
        Success = false,
        Error = new ApiError { Code = code, Message = message, Details = details }
    };
}

public class ApiError
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public IDictionary<string, string[]>? Details { get; init; }
}

public class ApiMetadata
{
    public string RequestId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string ApiVersion { get; init; } = "v1";
}

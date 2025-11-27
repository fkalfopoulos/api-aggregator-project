namespace api_aggregator.Services.Models;

public class VoidApiResult<TErrorCode>
{
    /// <summary>
    /// The error details when the result has failed.
    /// </summary>
    public ApiResultError<TErrorCode>? Error { get; set; }

    /// <summary>
    /// Returns true when the result is successful.
    /// </summary>
    public bool Success => Error == null;

    /// <summary>
    /// Returns true when the result is failure.
    /// </summary>
    public bool Failed => Error is { };

    public VoidApiResult()
    {
        Error = null;
    }

    /// <summary>
    /// Create a new successful result.
    /// </summary>
    /// <param name="value">The value of the result</param>
    public VoidApiResult(TErrorCode value)
    {
        Error = new ApiResultError<TErrorCode>(value);
    }

    /// <summary>
    /// Create a new failed result.
    /// </summary>
    /// <param name="errorCode">The code of the error</param>
    /// <param name="message">Optionally the human readable error message</param>
    /// <param name="exception">Optionally the related exception that may have been thrown from the service</param>
    public VoidApiResult(TErrorCode errorCode, string? message = null, Exception? exception = null)
    {
        Error = new ApiResultError<TErrorCode>
        {
            Code = errorCode,
            Message = message,
            Exception = exception
        };
    }

    /// <summary>
    /// Create a new service result based n an existing service result
    /// </summary>
    /// <param name="result">Existing service result</param>
    public VoidApiResult(VoidApiResult<TErrorCode> result)
    {
        Error = result?.Error;
    }

    /// <summary>
    /// Create a new error service result
    /// </summary>
    /// <param name="error">Error info</param>
    public VoidApiResult(ApiResultError<TErrorCode> error)
    {
        Error = error;
    }
}


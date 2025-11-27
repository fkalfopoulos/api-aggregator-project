namespace api_aggregator.Services.Models;

/// <summary>
/// This base class defines a common result returned by services.
/// </summary>
/// <typeparam name="TValue">The type of the value of the result</typeparam>
/// <typeparam name="TErrorCode">The type of the error code of the result. It is preferred to be an enumeration.</typeparam>
public class ApiResult<TValue, TErrorCode> : VoidApiResult<TErrorCode>
{
    /// <summary>
    /// The actual value of the result when successful.
    /// </summary>
    public TValue? Value { get; set; }

    /// <summary>
    /// Create a new successful result.
    /// </summary>
    /// <param name="value">The value of the result</param>
    public ApiResult(TValue value)
      : base((VoidApiResult<TErrorCode>)null!)
    {
        Value = value;
    }

    /// <summary>
    /// Create a new failed result.
    /// </summary>
    /// <param name="errorCode">The code of the error</param>
    /// <param name="message">Optionally the human readable error message</param>
    /// <param name="exception">Optionally the related exception that may have been thrown from the service</param>
    public ApiResult(TErrorCode errorCode, string? message = null, Exception? exception = null)
        : base(errorCode, message, exception)
    {
    }

    /// <summary>
    /// Create a new service result based n an existing service result
    /// </summary>
    /// <param name="result">Existing service result</param>
    public ApiResult(ApiResult<TValue, TErrorCode> result)
        : base(result.Error!)
    {
        Value = result.Value;
    }

    /// <summary>
    /// Create a new error service result
    /// </summary>
    /// <param name="error">Error info</param>
    public ApiResult(ApiResultError<TErrorCode> error)
        : base(error)
    {
    }
}

public class ServiceResult<TValue> : ApiResult<TValue, ApiErrorCode>
{
    /// <summary>
    /// Create a new successful result.
    /// </summary>
    /// <param name="value">The value of the result</param>
    public ServiceResult(TValue value)
        : base(value)
    {

    }

    /// <summary>
    /// Create a new failed result.
    /// </summary>
    /// <param name="errorCode">The code of the error</param>
    /// <param name="message">Optionally the human readable error message</param>
    /// <param name="exception">Optionally the related exception that may have been thrown from the service</param>
    public ServiceResult(ApiErrorCode errorCode, string? message = null, Exception? exception = null)
        : base(errorCode, message, exception)
    {

    }

    /// <summary>
    /// Create a new service result based n an existing service result
    /// </summary>
    /// <param name="result">Existing service result</param>
    public ServiceResult(ApiResult<TValue, ApiErrorCode> result) :
        base(result)
    {
        Value = result.Value;
        Error = result.Error;
    }

    /// <summary>
    /// Create a new error service result
    /// </summary>
    /// <param name="error">Error info</param>
    public ServiceResult(ApiResultError<ApiErrorCode> error) :
        base(error)
    {
        Error = error;
    }

    public static implicit operator ServiceResult<TValue>(TValue value) => new(value);
    public static implicit operator ServiceResult<TValue>(ApiResultError<ApiErrorCode> error) => new(error);
    public static implicit operator ServiceResult<TValue>(ApiErrorCode errorCode) => new(new ApiResultError<ApiErrorCode>(errorCode));
}


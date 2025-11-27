using api_aggregator.Services.Models;
using FluentAssertions;

namespace api_aggregator.UnitTests.Models;

public class ServiceResultTests
{
    [Fact]
    public void Success_ShouldCreate_SuccessfulResult()
    {
        // Arrange & Act
        var result = new ServiceResult<string>("test value");

        // Assert
        result.Success.Should().BeTrue();
        result.Failed.Should().BeFalse();
        result.Value.Should().Be("test value");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_ShouldCreate_FailedResult()
    {
        // Arrange & Act
        var result = new ServiceResult<string>(ApiErrorCode.GenericError, "error message");

        // Assert
        result.Success.Should().BeFalse();
        result.Failed.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be(ApiErrorCode.GenericError);
        result.Error.Message.Should().Be("error message");
    }

    [Fact]
    public void ImplicitConversion_FromValue_ShouldCreate_SuccessResult()
    {
        // Act
        ServiceResult<int> result = 42;

        // Assert
        result.Success.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void ImplicitConversion_FromErrorCode_ShouldCreate_FailureResult()
    {
        // Act
        ServiceResult<string> result = ApiErrorCode.NotFoundError;

        // Assert
        result.Failed.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be(ApiErrorCode.NotFoundError);
    }

    [Fact]
    public void ImplicitConversion_FromServiceResultError_ShouldCreate_FailureResult()
    {
        // Arrange
        var error = new ApiResultError<ApiErrorCode>(
            ApiErrorCode.ValidationError,
            "Validation failed");

        // Act
        ServiceResult<string> result = error;

        // Assert
        result.Failed.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void FailureResult_WithException_ShouldStore_Exception()
    {
        var exception = new InvalidOperationException("Test exception");

        var result = new ServiceResult<string>(
            ApiErrorCode.GenericError,
            "Error occurred",
            exception);

        result.Failed.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Exception.Should().Be(exception);
    }

    [Fact]
    public void CopyConstructor_ShouldCopy_AllProperties()
    {
        var original = new ServiceResult<int>(ApiErrorCode.Conflict, "Conflict error");

        var copy = new ServiceResult<int>(original);

        copy.Failed.Should().BeTrue();
        copy.Error.Should().NotBeNull();
        copy.Error!.Code.Should().Be(ApiErrorCode.Conflict);
        copy.Error.Message.Should().Be("Conflict error");
    }
}

public class VoidResultServiceResultTests
{
    [Fact]
    public void Success_ShouldCreate_SuccessfulResult()
    {
        var result = new VoidApiResult<ApiErrorCode>();

        result.Success.Should().BeTrue();
        result.Failed.Should().BeFalse();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_ShouldCreate_FailedResult()
    {
        var result = new VoidApiResult<ApiErrorCode>(
            ApiErrorCode.DatabaseGeneralError,
            "Database error");

        result.Success.Should().BeFalse();
        result.Failed.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be(ApiErrorCode.DatabaseGeneralError);
        result.Error.Message.Should().Be("Database error");
    }

    [Fact]
    public void CopyConstructor_ShouldCopy_ErrorState()
    {
        var original = new VoidApiResult<ApiErrorCode>(
            ApiErrorCode.GenericError,
            "Original error");

        var copy = new VoidApiResult<ApiErrorCode>(original);

        copy.Failed.Should().BeTrue();
        copy.Error.Should().NotBeNull();
        copy.Error!.Code.Should().Be(ApiErrorCode.GenericError);
        copy.Error.Message.Should().Be("Original error");
    }
}

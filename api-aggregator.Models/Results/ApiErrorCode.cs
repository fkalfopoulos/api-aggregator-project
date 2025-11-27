namespace api_aggregator.Services.Models;
public enum ApiErrorCode
{
    GenericError = -1,
    ValidationError = 400,
    NotFoundError = 404,
    Conflict = 409,
    DatabaseGeneralError = 500,
}

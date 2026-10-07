using System.Net;

namespace PushDashboard.Exceptions;

/// <summary>
/// Exception for validation errors and business rule violations.
/// Returns HTTP 400 Bad Request.
/// </summary>
public class ValidationException : BusinessException
{
    public ValidationException(string message) 
        : base(message, HttpStatusCode.BadRequest)
    {
    }

    public ValidationException(string message, Exception innerException) 
        : base(message, innerException, HttpStatusCode.BadRequest)
    {
    }
}

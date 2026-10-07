using System.Net;

namespace PushDashboard.Exceptions;

/// <summary>
/// Exception for resource not found errors.
/// Returns HTTP 404 Not Found.
/// </summary>
public class NotFoundException : BusinessException
{
    public NotFoundException(string message) 
        : base(message, HttpStatusCode.NotFound)
    {
    }

    public NotFoundException(string resourceType, object resourceId) 
        : base($"{resourceType} with ID '{resourceId}' was not found.", HttpStatusCode.NotFound)
    {
    }

    public NotFoundException(string message, Exception innerException) 
        : base(message, innerException, HttpStatusCode.NotFound)
    {
    }
}

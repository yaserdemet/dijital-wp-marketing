using System.Net;

namespace PushDashboard.Exceptions;

/// <summary>
/// Exception for duplicate entity errors.
/// Returns HTTP 409 Conflict.
/// </summary>
public class DuplicateException : BusinessException
{
    public DuplicateException(string message) 
        : base(message, HttpStatusCode.Conflict)
    {
    }

    public DuplicateException(string resourceType, string duplicateField) 
        : base($"A {resourceType} with the same {duplicateField} already exists.", HttpStatusCode.Conflict)
    {
    }

    public DuplicateException(string message, Exception innerException) 
        : base(message, innerException, HttpStatusCode.Conflict)
    {
    }
}

namespace RaceDay.Api.Middleware;

/// <summary>Base class for expected errors. The status code becomes the HTTP response code.</summary>
public abstract class AppException : Exception
{
    public int StatusCode { get; }

    protected AppException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}

public class BadRequestException : AppException
{
    public BadRequestException(string message) : base(StatusCodes.Status400BadRequest, message) { }
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message) : base(StatusCodes.Status401Unauthorized, message) { }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(StatusCodes.Status403Forbidden, message) { }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(StatusCodes.Status404NotFound, message) { }
}

public class ConflictException : AppException
{
    public ConflictException(string message) : base(StatusCodes.Status409Conflict, message) { }
}
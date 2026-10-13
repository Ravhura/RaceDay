namespace RaceDay.Api.Services;

/// <summary>
/// What a service method gives back to the controller.
/// Success: Data plus a status code (200, 201 or 204).
/// Failure: a status code (400, 401, 403, 404, 409) plus an error message.
/// </summary>
public class ServiceResult<T>
{
    public T? Data { get; set; }
    public int StatusCode { get; set; }
    public string? Message { get; set; }

    public bool Success => StatusCode >= 200 && StatusCode < 300;

    public static ServiceResult<T> Ok(T data) =>
        new ServiceResult<T> { Data = data, StatusCode = 200 };

    public static ServiceResult<T> Created(T data) =>
        new ServiceResult<T> { Data = data, StatusCode = 201 };

    public static ServiceResult<T> NoContent() =>
        new ServiceResult<T> { StatusCode = 204 };

    public static ServiceResult<T> BadRequest(string message) =>
        new ServiceResult<T> { StatusCode = 400, Message = message };

    public static ServiceResult<T> Unauthorized(string message) =>
        new ServiceResult<T> { StatusCode = 401, Message = message };

    public static ServiceResult<T> Forbidden(string message) =>
        new ServiceResult<T> { StatusCode = 403, Message = message };

    public static ServiceResult<T> NotFound(string message) =>
        new ServiceResult<T> { StatusCode = 404, Message = message };

    public static ServiceResult<T> Conflict(string message) =>
        new ServiceResult<T> { StatusCode = 409, Message = message };
}
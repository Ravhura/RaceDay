namespace RaceDay.Web.Services;

/// <summary>What every API call gives back: either the data, or a friendly error message.</summary>
public class ApiResult<T>
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public T? Data { get; set; }
    public string Error { get; set; } = string.Empty;

    public static ApiResult<T> Ok(T? data, int statusCode = 200) =>
        new ApiResult<T> { Success = true, StatusCode = statusCode, Data = data };

    public static ApiResult<T> Fail(int statusCode, string error) =>
        new ApiResult<T> { Success = false, StatusCode = statusCode, Error = error };
}
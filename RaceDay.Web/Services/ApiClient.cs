using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Web.Models;

namespace RaceDay.Web.Services;

/// <summary>
/// The ONLY class that talks to the Part 2 API. Controllers call its methods and never use HttpClient themselves.
/// It sends the API's session cookie with every request, and turns API errors into friendly messages.
/// </summary>
public class ApiClient
{
    private const string ApiCookieName = ".RaceDay.Session";
    private const string CannotReach = "We could not reach the RaceDay service. Please try again in a moment.";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;

    public ApiClient(HttpClient http, IHttpContextAccessor accessor)
    {
        _http = http;
        _accessor = accessor;
    }

    private ISession Session => _accessor.HttpContext!.Session;

    // ---------- Authentication ----------

    public async Task<ApiResult<LoginInfo>> LoginAsync(string role, string email, string password)
    {
        var url = role == Roles.Organiser ? "api/auth/login/organiser" : "api/auth/login/participant";
        try
        {
            var response = await _http.PostAsJsonAsync(url, new { email, password }, JsonOptions);
            var result = await ReadAsync<LoginInfo>(response);

            // Keep the API's session cookie, so every later call is recognised as this user.
            if (result.Success && response.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                foreach (var cookie in cookies)
                {
                    if (cookie.StartsWith(ApiCookieName + "="))
                    {
                        var value = cookie.Substring(ApiCookieName.Length + 1).Split(';')[0];
                        Session.SetString(SessionKeys.ApiCookie, value);
                    }
                }
            }
            return result;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ApiResult<LoginInfo>.Fail(0, CannotReach);
        }
    }

    public Task<ApiResult<object>> RegisterAsync(string role, object body)
    {
        var url = role == Roles.Organiser ? "api/auth/register/organiser" : "api/auth/register/participant";
        return SendAsync<object>(HttpMethod.Post, url, Json(body));
    }

    public Task<ApiResult<object>> LogoutAsync() => SendAsync<object>(HttpMethod.Post, "api/auth/logout");

    // ---------- Profile ----------

    public Task<ApiResult<ProfileItem>> GetProfileAsync(string role, int id) =>
        SendAsync<ProfileItem>(HttpMethod.Get, ProfileUrl(role, id));

    public Task<ApiResult<ProfileItem>> UpdateProfileAsync(string role, int id, object body) =>
        SendAsync<ProfileItem>(HttpMethod.Put, ProfileUrl(role, id), Json(body));

    // ---------- Events ----------

    public Task<ApiResult<List<EventItem>>> GetEventsAsync() =>
        SendAsync<List<EventItem>>(HttpMethod.Get, "api/events");

    public Task<ApiResult<EventItem>> GetEventAsync(int id) =>
        SendAsync<EventItem>(HttpMethod.Get, $"api/events/{id}");

    public Task<ApiResult<List<EventItem>>> GetOrganiserEventsAsync(int organiserId) =>
        SendAsync<List<EventItem>>(HttpMethod.Get, $"api/organisers/{organiserId}/events");

    public Task<ApiResult<EventItem>> CreateEventAsync(object body) =>
        SendAsync<EventItem>(HttpMethod.Post, "api/events", Json(body));

    public Task<ApiResult<EventItem>> UpdateEventAsync(int id, object body) =>
        SendAsync<EventItem>(HttpMethod.Put, $"api/events/{id}", Json(body));

    public Task<ApiResult<bool>> DeleteEventAsync(int id) =>
        SendAsync<bool>(HttpMethod.Delete, $"api/events/{id}");

    /// <summary>Sends a banner image to the API. The API stores it in Azure Blob Storage.</summary>
    public async Task<ApiResult<EventItem>> UploadBannerAsync(int eventId, IFormFile file)
    {
        using var content = new MultipartFormDataContent();
        using var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.FileName);
        return await SendAsync<EventItem>(HttpMethod.Post, $"api/events/{eventId}/banner", content);
    }

    // ---------- Categories ----------

    public Task<ApiResult<List<CategoryItem>>> GetCategoriesAsync(int eventId) =>
        SendAsync<List<CategoryItem>>(HttpMethod.Get, $"api/events/{eventId}/categories");

    public Task<ApiResult<CategoryItem>> GetCategoryAsync(int id) =>
        SendAsync<CategoryItem>(HttpMethod.Get, $"api/categories/{id}");

    public Task<ApiResult<CategoryItem>> CreateCategoryAsync(int eventId, object body) =>
        SendAsync<CategoryItem>(HttpMethod.Post, $"api/events/{eventId}/categories", Json(body));

    public Task<ApiResult<CategoryItem>> UpdateCategoryAsync(int id, object body) =>
        SendAsync<CategoryItem>(HttpMethod.Put, $"api/categories/{id}", Json(body));

    public Task<ApiResult<bool>> DeleteCategoryAsync(int id) =>
        SendAsync<bool>(HttpMethod.Delete, $"api/categories/{id}");

    // ---------- Enrolments ----------

    public Task<ApiResult<EnrolmentItem>> EnrolAsync(int eventId, int categoryId) =>
        SendAsync<EnrolmentItem>(HttpMethod.Post, "api/enrolments", Json(new { eventId, categoryId }));

    public Task<ApiResult<List<EnrolmentItem>>> GetMyEnrolmentsAsync(int participantId) =>
        SendAsync<List<EnrolmentItem>>(HttpMethod.Get, $"api/participants/{participantId}/enrolments");

    public Task<ApiResult<bool>> CancelEnrolmentAsync(int id) =>
        SendAsync<bool>(HttpMethod.Delete, $"api/enrolments/{id}");

    public Task<ApiResult<List<EnrolmentItem>>> GetEventEnrolmentsAsync(int eventId) =>
        SendAsync<List<EnrolmentItem>>(HttpMethod.Get, $"api/events/{eventId}/enrolments");

    public Task<ApiResult<List<EnrolmentItem>>> GetOrganiserEnrolmentsAsync(int organiserId) =>
        SendAsync<List<EnrolmentItem>>(HttpMethod.Get, $"api/organisers/{organiserId}/enrolments");

    // ---------- Results ----------

    public Task<ApiResult<ResultItem>> CaptureResultAsync(object body) =>
        SendAsync<ResultItem>(HttpMethod.Post, "api/results", Json(body));

    public Task<ApiResult<ResultItem>> UpdateResultAsync(int id, object body) =>
        SendAsync<ResultItem>(HttpMethod.Put, $"api/results/{id}", Json(body));

    public Task<ApiResult<bool>> DeleteResultAsync(int id) =>
        SendAsync<bool>(HttpMethod.Delete, $"api/results/{id}");

    public Task<ApiResult<List<ResultItem>>> GetMyResultsAsync(int participantId) =>
        SendAsync<List<ResultItem>>(HttpMethod.Get, $"api/participants/{participantId}/results");

    public Task<ApiResult<List<ResultItem>>> GetEventResultsAsync(int eventId) =>
        SendAsync<List<ResultItem>>(HttpMethod.Get, $"api/events/{eventId}/results");

    // ---------- Helpers ----------

    private static string ProfileUrl(string role, int id) =>
        role == Roles.Organiser ? $"api/organisers/{id}" : $"api/participants/{id}";

    private static HttpContent Json(object body) => JsonContent.Create(body, options: JsonOptions);

    /// <summary>Sends one request to the API, adding the saved session cookie.</summary>
    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content = null)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url) { Content = content };

            var cookie = Session.GetString(SessionKeys.ApiCookie);
            if (!string.IsNullOrEmpty(cookie))
            {
                request.Headers.Add("Cookie", $"{ApiCookieName}={cookie}");
            }

            using var response = await _http.SendAsync(request);

            // 401 here means the API no longer knows this user (for example, the API was restarted).
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                Session.Clear();
                return ApiResult<T>.Fail(401, "Your session has expired. Please log in again.");
            }

            return await ReadAsync<T>(response);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ApiResult<T>.Fail(0, CannotReach);
        }
    }

    private static async Task<ApiResult<T>> ReadAsync<T>(HttpResponseMessage response)
    {
        var code = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return ApiResult<T>.Ok(default, code);
            }
            var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
            return ApiResult<T>.Ok(data, code);
        }

        return ApiResult<T>.Fail(code, await ReadErrorAsync(response));
    }

    /// <summary>Turns the API's error body into one readable sentence.</summary>
    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var text = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            // The API's own errors look like { "message": "..." }
            if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
            {
                return message.GetString() ?? "";
            }

            // Validation errors look like { "errors": { "Field": ["..."] } }
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var messages = new List<string>();
                foreach (var field in errors.EnumerateObject())
                {
                    foreach (var item in field.Value.EnumerateArray())
                    {
                        messages.Add(item.GetString() ?? "");
                    }
                }
                if (messages.Count > 0)
                {
                    return string.Join(" ", messages);
                }
            }
        }
        catch (JsonException)
        {
            // The body was not JSON. Fall through to the generic messages below.
        }

        return (int)response.StatusCode switch
        {
            403 => "You do not have permission to do that.",
            404 => "That item could not be found.",
            409 => "That conflicts with existing data.",
            _ => "Something went wrong. Please try again."
        };
    }
}
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Authentication;
using RaceDay.Api.Data;
using RaceDay.Api.Middleware;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Database (EF Core Code First -> SQL Server LocalDB) ----------
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RaceDayDb")));

// ---------- Server-side session ----------
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;                              // JavaScript cannot read the cookie
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// ---------- Authentication / authorisation (session-based) ----------
builder.Services.AddHttpContextAccessor();
builder.Services
    .AddAuthentication(SessionAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(SessionAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorization(options =>
{
    // Anything not explicitly marked [AllowAnonymous] requires a logged-in user.
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});

// ---------- Error handling ----------
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ---------- Application services ----------
builder.Services.AddSingleton<PasswordService>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<EnrolmentService>();
builder.Services.AddScoped<ResultService>();

builder.Services.AddControllers();

// ---------- Swagger / OpenAPI ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "RESTful API for RaceDay - event management for road running, walking and cycling events.\n\n" +
                      "**How to authenticate:** call one of the POST /api/auth/login/... endpoints below. " +
                      "The server starts a session and your browser keeps the session cookie, so the other " +
                      "endpoints work straight away. Use logout (or log in as the other role) to switch users."
    });

    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    // Development convenience: create/update RaceDayDB from the migrations and load the sample data.
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        db.Database.Migrate();
        await DbSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<PasswordService>());
    }

    app.UseSwagger();
    app.UseSwaggerUI(options => options.DocumentTitle = "RaceDay API");
}

app.UseHttpsRedirection();
app.UseSession();          // must come before authentication
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Makes the entry point visible to the test project (WebApplicationFactory).
public partial class Program { }
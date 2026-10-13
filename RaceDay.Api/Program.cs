using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Database (EF Core Code First -> SQL Server LocalDB) ----------
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RaceDayDb")));

// ---------- Server-side session ----------
// The session is kept in the server's memory. The browser only holds a session id cookie.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;   // JavaScript cannot read the cookie
    options.Cookie.IsEssential = true;
});

// ---------- Services ----------
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<EnrolmentService>();
builder.Services.AddScoped<ResultService>();

builder.Services.AddControllers();

// ---------- Swagger ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "RaceDay API for managing road running, walking and cycling events. " +
                      "Log in with one of the login endpoints first. After that, Swagger sends your session cookie automatically."
    });

    // Reads the /// comments so Swagger can show them.
    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Development only: create the database from the migrations and load the sample data.
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        db.Database.Migrate();
        await DbSeeder.SeedAsync(db);
    }

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseSession();   // turns on the server-side session for every request
app.MapControllers();

app.Run();
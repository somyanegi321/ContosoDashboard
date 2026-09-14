using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();
builder.Services.Configure<DocumentFeatureOptions>(builder.Configuration.GetSection(DocumentFeatureOptions.SectionName));

// Add authentication state provider for Blazor
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Configure Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Mock Authentication (Cookie-based for training purposes)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Add authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Employee", policy => policy.RequireRole("Employee", "TeamLead", "ProjectManager", "Administrator"));
    options.AddPolicy("TeamLead", policy => policy.RequireRole("TeamLead", "ProjectManager", "Administrator"));
    options.AddPolicy("ProjectManager", policy => policy.RequireRole("ProjectManager", "Administrator"));
    options.AddPolicy("Administrator", policy => policy.RequireRole("Administrator"));
});

// Register application services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IMalwareScanner, FailClosedMalwareScanner>();
builder.Services.AddScoped<DocumentAuthorizationService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();

// Add HttpContextAccessor for accessing user claims
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        context.Database.EnsureCreated(); // For development - use migrations in production

        // EnsureCreated does not update an existing database when new entities are added.
        // The training app can safely rebuild a stale local database so new document tables exist.
        if (app.Environment.IsDevelopment() && !HasDocumentSchema(context))
        {
            logger.LogWarning("The local database predates document management. Recreating the development database.");
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the database.");
    }
}

static bool HasDocumentSchema(ApplicationDbContext context)
{
    try
    {
        context.Database.ExecuteSqlRaw("SELECT 1 FROM \"Documents\" LIMIT 1");
        context.Database.ExecuteSqlRaw("SELECT 1 FROM \"DocumentShares\" LIMIT 1");
        context.Database.ExecuteSqlRaw("SELECT 1 FROM \"DocumentActivities\" LIMIT 1");
        return true;
    }
    catch (SqliteException)
    {
        return false;
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    // Use HSTS even in development for training purposes
    app.UseHsts();
}

// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
    // Content Security Policy for Blazor Server
    context.Response.Headers["Content-Security-Policy"] = 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "font-src 'self' https://cdn.jsdelivr.net; " +
        "img-src 'self' data: https:; " +
        "connect-src 'self' wss: ws:;";
    
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapControllers();
app.MapGet("/documents/{documentId:int}/download", async (int documentId, HttpContext httpContext, IDocumentService documents, CancellationToken cancellationToken) =>
{
    if (!int.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Unauthorized();
    var stream = await documents.OpenAuthorizedAsync(userId, documentId, false, cancellationToken);
    return stream is null ? Results.NotFound() : Results.File(stream, "application/octet-stream", enableRangeProcessing: true);
}).RequireAuthorization();
app.MapGet("/documents/{documentId:int}/preview", async (int documentId, HttpContext httpContext, IDocumentService documents, CancellationToken cancellationToken) =>
{
    if (!int.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Unauthorized();
    var stream = await documents.OpenAuthorizedAsync(userId, documentId, true, cancellationToken);
    return stream is null ? Results.NotFound() : Results.File(stream, "application/pdf");
}).RequireAuthorization();
app.MapFallbackToPage("/_Host");

app.Run();

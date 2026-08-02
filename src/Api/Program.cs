using Api;
using Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(builder.Configuration);

// Catches anything that escapes controller/middleware handling (e.g. the
// concurrent-first-sign-in race in AuthService, or any future unforeseen
// failure) so callers get a generic 500 ProblemDetails instead of a raw
// framework error page - the exception itself is logged, never returned.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Cloud Run terminates TLS at its edge and forwards plain HTTP internally,
// so the app needs to know the original request's port/scheme was https via
// forwarded headers, and Cloud Run's own edge IPs aren't a fixed,
// enumerable set - the container is never reachable except through Cloud
// Run's proxy, so trust the forwarded headers unconditionally here.
//
// Known, accepted tradeoff: this means anyone who can reach the process
// directly (a local `dotnet run`, docker-compose with no proxy in front,
// or any future non-Cloud-Run deployment) can set X-Forwarded-Proto: https
// and bypass the HTTPS-redirect check below. That's fine today - there's
// nothing sensitive reachable any other way in those environments - but if
// this app is ever deployed somewhere reachable directly (not exclusively
// through Cloud Run's proxy), this needs KnownProxies/KnownIPNetworks
// restricted to that proxy's real address(es) instead of cleared.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddHttpsRedirection(options => options.HttpsPort = 443);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        // Required because the frontend (Vercel) and API (Cloud Run) are
        // different origins.
        options.Cookie.SameSite = SameSiteMode.None;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;

        // This is a JSON API, not an MVC login page - return status codes
        // instead of redirecting to a nonexistent login/access-denied page.
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization(options =>
{
    // NFR-12: every endpoint requires an authenticated session by default;
    // individual actions opt out with [AllowAnonymous] (e.g. the Google
    // sign-in exchange itself).
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposed so WebApplicationFactory<Program> in Api.IntegrationTests can
// boot this app in-memory for integration tests.
public partial class Program;

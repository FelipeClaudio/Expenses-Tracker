var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// NFR-10 (HTTPS) is implemented with real coverage in the Auth slice
// (build-order step 3, SecurityHeadersTests) — the container only serves
// plain HTTP internally (Cloud Run terminates TLS upstream), so a redirect
// middleware here would be dead code with no cert to redirect to yet.

app.UseAuthorization();

app.MapControllers();

app.Run();

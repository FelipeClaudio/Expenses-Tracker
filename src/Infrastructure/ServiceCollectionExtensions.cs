using Core.Auth;
using Core.Expenses;
using Core.Settlements;
using Core.Topics;
using Core.Users;
using Infrastructure.Auth;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Wires persistence and auth infrastructure so the Api project doesn't
    /// need its own direct package references to EF Core/Npgsql.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IClock, SystemClock>();

        // Auth:FakeMode is only ever set for local Playwright E2E runs (see
        // client/playwright.config.ts) so smoke tests can sign in without a
        // real Google account. Defaults to false everywhere else, including
        // production.
        if (configuration.GetValue<bool>("Auth:FakeMode"))
        {
            services.AddSingleton<IGoogleTokenValidator, FakeGoogleTokenValidator>();
        }
        else
        {
            services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
        }

        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<ITopicRepository, TopicRepository>();
        services.AddScoped<ITopicMembershipRepository, TopicMembershipRepository>();
        services.AddScoped<ITopicService, TopicService>();

        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddSingleton<IExpenseSplitService, ExpenseSplitService>();
        services.AddScoped<IExpenseService, ExpenseService>();

        services.AddSingleton<ISettlementService, SettlementService>();
        services.AddScoped<ISettledTransferRepository, SettledTransferRepository>();
        services.AddScoped<IBalanceService, BalanceService>();
        services.AddScoped<ISettledTransferService, SettledTransferService>();

        services.Configure<GoogleAuthOptions>(configuration.GetSection("Google"));

        return services;
    }
}

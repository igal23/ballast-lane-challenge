using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using TaskBoard.Application.Abstractions;
using TaskBoard.Infrastructure.Persistence;
using TaskBoard.Infrastructure.Security;
using TaskBoard.Infrastructure.Auth;
using TaskBoard.Infrastructure.Time;

namespace TaskBoard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTaskBoardInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>()
            .Configure(options =>
                options.ConnectionString = configuration.GetConnectionString("TaskBoard") ?? string.Empty)
            .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "ConnectionStrings:TaskBoard is required.")
            .ValidateOnStart();
        services.AddSingleton(serviceProvider => NpgsqlDataSource.Create(
            serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value.ConnectionString));
        services.AddSingleton<DatabaseMigrator>();
        services.AddSingleton<DevelopmentSeeder>();
        services.AddScoped<IUserRepository, NpgsqlUserRepository>();
        services.AddScoped<ITaskRepository, NpgsqlTaskRepository>();
        services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "Jwt:Issuer is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Jwt:Audience is required.")
            .Validate(options => System.Text.Encoding.UTF8.GetByteCount(options.SigningKey) >= 32,
                "Jwt:SigningKey must contain at least 32 UTF-8 bytes.")
            .ValidateOnStart();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddSingleton<ICurrentDate, UtcCurrentDate>();
        services.AddHostedService<DatabaseBootstrapHostedService>();
        return services;
    }
}

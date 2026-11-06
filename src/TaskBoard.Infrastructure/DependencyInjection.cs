using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TaskBoard.Application.Abstractions;
using TaskBoard.Infrastructure.Persistence;
using TaskBoard.Infrastructure.Security;
using TaskBoard.Infrastructure.Auth;

namespace TaskBoard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTaskBoardInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(_ => NpgsqlDataSource.Create(
            configuration.GetConnectionString("TaskBoard")!));
        services.AddSingleton<DatabaseMigrator>();
        services.AddScoped<IUserRepository, NpgsqlUserRepository>();
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
        return services;
    }
}

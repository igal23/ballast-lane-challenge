using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TaskBoard.Application.Abstractions;
using TaskBoard.Infrastructure.Persistence;
using TaskBoard.Infrastructure.Security;

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
        return services;
    }
}

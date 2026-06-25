using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Dashboard_v2.Infrastructure.Data;

/// <summary>
/// Design-time factory used by dotnet ef migrations tool
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Build configuration from appsettings in the Web project
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../Web"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Dashboard_v2Db");
        if (connectionString is null)
        {
            // IMPORTANT: Set ConnectionStrings__Dashboard_v2Db as an environment variable,
            // or define it under "ConnectionStrings": { "Dashboard_v2Db": "..." } in appsettings.json.
            throw new InvalidOperationException(
                "No connection string found. Set ConnectionStrings__Dashboard_v2Db in environment or appsettings.json.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}

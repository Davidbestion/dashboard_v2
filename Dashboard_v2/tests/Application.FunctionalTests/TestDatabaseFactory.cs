namespace Dashboard_v2.Application.FunctionalTests;

public static class TestDatabaseFactory
{
    public static async Task<ITestDatabase> CreateAsync()
    {
        // Using local PostgreSQL instead of Testcontainers (requires Docker).
        // Connection string configured in appsettings.json.
        var database = new PostgreSQLTestDatabase();

        await database.InitialiseAsync();

        return database;
    }
}

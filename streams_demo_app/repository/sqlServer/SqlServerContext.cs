namespace StreamsDemoApp.repository.sqlServer;

using Microsoft.EntityFrameworkCore;

public class SqlServerContext : DbContext
{
    public DbSet<PersonRecord> Persons => Set<PersonRecord>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connection = Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION") ?? "defaults params";
        optionsBuilder.UseSqlServer(connection);
    }
}
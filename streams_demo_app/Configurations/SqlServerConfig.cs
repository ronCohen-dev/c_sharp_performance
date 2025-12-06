namespace StreamsDemoApp.Configurations
{
    public class SqlServerConfig
    {
        public string ConnectionString { get; set; } =
            Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION") ?? "defaults params";

        public string DatabaseName { get; set; } =
            Environment.GetEnvironmentVariable("SQLSERVER_DBNAME") ?? "defaults params";
    }
}
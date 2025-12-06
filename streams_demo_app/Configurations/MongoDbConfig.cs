namespace StreamsDemoApp.Configurations
{
    public class MongoDbConfig
    {
        public string ConnectionString { get; set; } =
            Environment.GetEnvironmentVariable("MONGODB_CONNECTION") ?? "defaults params";

        public string DatabaseName { get; set; } =
            Environment.GetEnvironmentVariable("MONGODB_DBNAME") ?? "defaults params";
    }
}
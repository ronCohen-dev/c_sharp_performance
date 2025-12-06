namespace StreamsDemoApp.repository.mongo;
using MongoDB.Driver;
public class MongoContext
{
    private readonly IMongoDatabase _database;
    public IMongoCollection<PersonRecord> Persons => _database.GetCollection<PersonRecord>("Persons");

    public MongoContext()
    {
        var mongoUri = Environment.GetEnvironmentVariable("MONGO_URI") ?? "defaults params";
        var client = new MongoClient(mongoUri);
        _database = client.GetDatabase( Environment.GetEnvironmentVariable("MONGODB_DBNAME"));

    }
}
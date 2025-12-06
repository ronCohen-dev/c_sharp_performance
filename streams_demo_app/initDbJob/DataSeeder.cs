using Bogus;
using MongoDB.Driver;
using StreamsDemoApp.repository.mongo;
using StreamsDemoApp.repository.sqlServer;

namespace StreamsDemoApp.initDbJob
{
    public class DataSeeder
    {
        public static async Task InitializeAsync()
        {
            const int total = 5_000_000;
            const int batchSize = 500_000;

            Console.WriteLine("checking database status :) ");

            var faker = new Faker<PersonRecord>()
                .RuleFor(p => p.Id, f => Guid.NewGuid().ToString())
                .RuleFor(p => p.FirstName, f => f.Name.FirstName())
                .RuleFor(p => p.LastName, f => f.Name.LastName())
                .RuleFor(p => p.Age, f => f.Random.Int(18, 70))
                .RuleFor(p => p.City, f => f.Address.City())
                .RuleFor(p => p.Country, f => f.Address.Country())
                .RuleFor(p => p.Email, (f, p) => $"{p.FirstName.ToLower()}.{p.LastName.ToLower()}@{f.Internet.DomainName()}")
                .RuleFor(p => p.JobTitle, f => f.Name.JobTitle())
                .RuleFor(p => p.Salary, f => f.Finance.Amount(5_000, 30_000))
                .RuleFor(p => p.CreatedAt, f => f.Date.Past(1));
            
            using (var sqlContext = new SqlServerContext())
            {
                await sqlContext.Database.EnsureCreatedAsync();

                var existingCount = sqlContext.Persons.Count();
                if (existingCount == 0)
                {
                    Console.WriteLine($"sql server empty -> {total:N0} records in batches of {batchSize:N0}");
                    
                    for (int i = 0; i < total; i += batchSize)
                    {
                        var batch = faker.Generate(batchSize);
                        await sqlContext.Persons.AddRangeAsync(batch);
                        await sqlContext.SaveChangesAsync();
                        sqlContext.ChangeTracker.Clear();

                        Console.WriteLine($"inserted {i + batch.Count:N0} / {total:N0} to sql server");
                    }
                }
                else
                {
                    Console.WriteLine($"sql server already has {existingCount:N0} records");
                }
            }

            var mongoContext = new MongoContext();
            var mongoCount = await mongoContext.Persons.CountDocumentsAsync(Builders<PersonRecord>.Filter.Empty);

            if (mongoCount == 0)
            {
                Console.WriteLine($"mongoDb empty -> {total:N0} records in batches of {batchSize:N0}");
                
                for (int i = 0; i < total; i += batchSize)
                {
                    var batch = faker.Generate(batchSize);
                    await mongoContext.Persons.InsertManyAsync(batch);
                    Console.WriteLine($"inserted {i + batch.Count:N0} / {total:N0} to mongoDb");
                }
            }
            else
            {
                Console.WriteLine($"mongoDb already has {mongoCount:N0} records");
            }

            Console.WriteLine("finish insert database ;)");
        }
    }
}

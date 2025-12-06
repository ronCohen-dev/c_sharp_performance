using StreamsDemoApp.performanceTestJob;
using StreamsDemoApp.Configurations;
using StreamsDemoApp.initDbJob;

var builder = WebApplication.CreateBuilder(args);

// to init the db with data
await DataSeeder.InitializeAsync();

builder.Services.AddSingleton(new SqlServerConfig());
builder.Services.AddSingleton(new MongoDbConfig());

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

Console.WriteLine("starting mongo pipeline");
var mongoPip = new PerformanceDeepComparisonMongo();
await mongoPip.RunAsync();
Console.WriteLine("\n ending mongo pipeline");

Console.WriteLine("\n ------------------------ \n");

Console.WriteLine("starting sql pipeline");
var sqlPip = new PerformanceDeepComparisonSql();
await sqlPip.RunAsync();
Console.WriteLine("\n ending sql pipeline");

app.Run();





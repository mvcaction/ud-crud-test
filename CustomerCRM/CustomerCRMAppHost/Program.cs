using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Simple PostgreSQL setup
var postgres = builder.AddPostgres("postgres");
var database = postgres.AddDatabase("customercrm");

// Add the API project with a custom endpoint name
var api = builder.AddProject("customercrm-api", "../API/API.csproj")
    .WithReference(database)
    .WithHttpEndpoint(port: 5000, name: "api-http"); // Different name

// Build and run the application
var app = builder.Build();

await app.RunAsync();

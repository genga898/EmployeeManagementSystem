using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Server>("server");
builder.AddProject<Projects.ClientApp>("client")
    .WithReference(api);


builder.Build().Run();
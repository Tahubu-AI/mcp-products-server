var builder = DistributedApplication.CreateBuilder(args);

var openAi = builder.AddConnectionString("openAiConnectionName");

var mcpServer = builder.AddProject<Projects.mcp_products_server>("mcp-products-server");

builder.AddProject<Projects.mcp_products_client>("mcp-products-client")
    .WithReference(mcpServer)
    .WithReference(openAi);

builder.Build().Run();

# MCP Products Server

A Model Context Protocol (MCP) server that provides product search and marketing summary generation capabilities for the Cosmic Works Bike Company. This project includes both the MCP server and a Blazor client for easy testing and interaction.

## Features

### MCP Server
- **Product Search Tools**: Full-text and hybrid search capabilities
- **Product Summary Tools**: Generate, retrieve, update, and delete marketing summaries
- **HTTP Transport**: JSON-RPC over HTTP for easy integration
- **Comprehensive Logging**: Track tool invocations, parameters, and performance metrics
- **Azure Cosmos DB Integration**: Vector search and document storage
- **Azure OpenAI Integration**: AI-powered content generation

### Blazor Client
- **Interactive UI**: Modern web interface for testing MCP tools
- **Markdown Rendering**: Rich display of AI-generated responses
- **Real-time Logging**: See which tools are being invoked
- **Responsive Design**: Works on desktop and mobile devices

## Architecture

```
┌─────────────────┐    HTTP/JSON-RPC    ┌─────────────────┐
│   Blazor Client │ ◄─────────────────► │   MCP Server    │
│                 │                     │                 │
│ - User Interface│                     │ - Product Tools │
│ - Markdown      │                     │ - Summary Tools │
│ - Logging       │                     │ - Cosmos DB     │
└─────────────────┘                     └─────────────────┘
                                              │
                                              ▼
                                       ┌─────────────────┐
                                       │  Azure Cosmos   │
                                       │      DB         │
                                       └─────────────────┘
```

## Prerequisites

- .NET 9.0 SDK
- Azure Cosmos DB account
- Azure OpenAI service (optional, for enhanced features)

## Setup

### 1. Clone the Repository
```bash
git clone <repository-url>
cd mcp-dotnet-2-complex
```

### 2. Configure Environment

Create `appsettings.Development.json` files in the following locations:

#### MCP Server (`mcp-products-server/appsettings.Development.json`)
```json
{
  "DetailedErrors": true,
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Chat": {
    "ProductMaxResults": "10",
    "MaxContextWindow": "3",
    "CacheSimilarityScore": "0.95"
  },
  "OpenAi": {
    "Endpoint": "https://your-openai-endpoint.openai.azure.com/",
    "CompletionDeploymentName": "gpt-4o",
    "MaxRagTokens": "1500",
    "MaxContextTokens": "500",
    "EmbeddingDeploymentName": "text-embedding-3-large"
  },
  "CosmosDb": {
    "CacheContainer": "cache",
    "Database": "cosmoscopilotdb",
    "ProductContainer": "products",
    "ChatContainer": "chat",
    "ProductDataSourceURI": "https://cosmosdbcosmicworks.blob.core.windows.net/cosmic-works-vectorized/product-text-3-large-1536-llm-gen-2.json",
    "Endpoint": "https://your-cosmos-endpoint.documents.azure.com:443/"
  }
}
```

#### App Host (`mcp-products-server.AppHost/appsettings.Development.json`)
```json
{
  "DetailedErrors": true,
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### 3. Build and Run

#### Option A: Run with .NET Aspire (Recommended)
```bash
cd mcp-products-server.AppHost
dotnet run
```

This will start both the MCP server and the Blazor client, with the Aspire dashboard providing monitoring and configuration.

#### Option B: Run Individual Components
```bash
# Terminal 1: Start MCP Server
cd mcp-products-server
dotnet run

# Terminal 2: Start Blazor Client
cd mcp-products-client
dotnet run
```

## Available MCP Tools

### Product Search Tools
- `full_text_search_products` - Keyword-based search through product descriptions
- `hybrid_search_products` - Vector + keyword search for semantic understanding
- `get_product_by_id` - Retrieve a specific product by ID

### Product Summary Tools
- `generate_product_summary` - Create marketing summaries with AI-generated content
- `get_product_summary` - Retrieve existing product summaries
- `update_product_summary` - Update marketing content while preserving automated fields
- `delete_product_summary` - Remove product summaries
- `list_product_summaries` - Get all summaries in the database

## Testing

### Using the Blazor Client
1. Navigate to the client URL (typically `http://localhost:5000`)
2. Enter prompts like:
   - "Find mountain bikes under $1000"
   - "Generate marketing copy for a road bike"
   - "What are the best city bikes for commuting?"

### Using HTTP Directly
```bash
# List available tools
curl -X POST http://localhost:5000 \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "tools/list",
    "params": {}
  }'

# Search for products
curl -X POST http://localhost:5000 \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/call",
    "params": {
      "name": "full_text_search_products",
      "arguments": {
        "searchText": "mountain bike",
        "maxResults": 5
      }
    }
  }'
```

## Logging

The application provides comprehensive logging:

### Server-side Logging
- Tool invocations with parameters
- Execution duration and results
- Error tracking and debugging information

### Client-side Logging
- Agent function calls
- Request/response tracking
- Performance metrics

Logs are available in:
- Console output (development)
- Aspire dashboard (when using .NET Aspire)
- Application Insights (if configured)

## Security

- Sensitive configuration files (`appsettings.Development.json`) are excluded from version control
- Azure Cosmos DB uses managed identity for authentication
- All user inputs are sanitized and validated

## Development

### Project Structure
```
├── mcp-products-server/           # MCP Server implementation
│   ├── Tools/                     # MCP tool implementations
│   ├── Services/                  # Business logic services
│   ├── Models/                    # Data models
│   └── Options/                   # Configuration options
├── mcp-products-client/           # Blazor client application
│   ├── Components/                # Blazor components
│   └── wwwroot/                   # Static assets
├── mcp-products-server.AppHost/   # .NET Aspire application host
└── mcp-products-server.ServiceDefaults/  # Shared service defaults
```

### Adding New Tools
1. Create a new class in the `Tools/` directory
2. Add the `[McpServerToolType]` attribute
3. Implement methods with `[McpServerTool]` attributes
4. Register the tool in `Program.cs`

### Customizing the UI
- Modify `Home.razor` for the main interface
- Update `app.css` for styling
- Add new components in the `Components/` directory

## Troubleshooting

### Common Issues

1. **Configuration Errors**
   - Ensure all required settings are in `appsettings.Development.json`
   - Check Azure service endpoints and credentials

2. **Build Errors**
   - Ensure .NET 9.0 SDK is installed
   - Run `dotnet restore` to restore packages

3. **Connection Issues**
   - Verify Azure Cosmos DB connection string
   - Check network connectivity to Azure services

### Getting Help
- Check the console logs for detailed error messages
- Review the Aspire dashboard for service health
- Ensure all prerequisites are properly configured

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details. 
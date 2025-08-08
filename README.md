# RoslynAsAServiceAPI

A RESTful web API that provides Roslyn (Microsoft's C# compiler platform) functionality as a service. This API allows you to analyze, query, and modify C# source code remotely using HTTP requests.

**Primary Use Case**: This project enables n8n workflows to access and manipulate your local Visual Studio project files, allowing you to automate code analysis, refactoring, and modification tasks through n8n's visual workflow interface.

## Features

- **Code Analysis**: Query syntax nodes, methods, and attributes in C# files
- **File Discovery**: Find C# files in specified directories
- **Code Modification**: Replace text ranges in source files
- **Security**: API key authentication for all endpoints
- **OpenAPI Integration**: Swagger documentation for easy testing and integration
- **n8n Integration**: RESTful endpoints designed for seamless integration with n8n workflows
- **Local File Access**: Direct access to your Visual Studio project files on the local machine

## Technologies Used

- **.NET 9.0**: Latest version of the .NET framework
- **ASP.NET Core**: Web API framework
- **Microsoft.CodeAnalysis.CSharp**: Roslyn compiler APIs for C# code analysis
- **Microsoft.CodeAnalysis.Workspaces.MSBuild**: MSBuild workspace support
- **OpenAPI/Swagger**: API documentation and testing interface

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022 or VS Code (optional)

### Installation

1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd RoslynAsAServiceAPI
   ```

2. Navigate to the project directory:
   ```bash
   cd src/RoslynAsAServiceAPI
   ```

3. Restore dependencies:
   ```bash
   dotnet restore
   ```

4. Configure your API key in `appsettings.json`:
   ```json
   {
     "ApiKey": "your-secret-api-key-here"
   }
   ```

### Running the Application

1. Build and run the application:
   ```bash
   dotnet run
   ```

2. The API will be available at:
   - HTTP: `http://localhost:5000`
   - HTTPS: `https://localhost:5001`

3. Access the Swagger UI (in development mode):
   - `https://localhost:5001/openapi`

## API Endpoints

All endpoints require an API key to be passed in the request headers:
```
X-API-Key: your-api-key
```

### Query Operations (`/api/query`)

#### POST `/api/query/syntax-nodes`
Query syntax nodes in a C# file with optional filtering.

**Request Body:**
```json
{
  "filePath": "path/to/your/file.cs",
  "withAttribute": "OptionalAttributeName",
  "methodName": "OptionalMethodName"
}
```

**Response:**
Returns a list of found nodes with their details, locations, and attributes.

#### POST `/api/query/find-csharp-files`
Find all C# files in a specified directory.

**Request Body:**
```json
{
  "path": "path/to/search/directory"
}
```

**Response:**
```json
{
  "files": ["file1.cs", "file2.cs", ...]
}
```

### Action Operations (`/api/actions`)

#### POST `/api/actions/replace-range`
Replace a specific range of lines in a C# file.

**Request Body:**
```json
{
  "filePath": "path/to/your/file.cs",
  "startLine": 10,
  "endLine": 15,
  "newText": "// New code content"
}
```

## Configuration

### API Key Security

The API uses a simple API key authentication mechanism. Configure your API key in:

- `appsettings.json` for development
- `appsettings.Production.json` for production
- Environment variables: `ApiKey`

### Example Configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ApiKey": "your-secure-api-key-here",
  "AllowedHosts": "*"
}
```

## Development

### Project Structure

```
src/
├── RoslynAsAServiceAPI/
│   ├── Groups/
│   │   └── RoslynGroup.cs          # API endpoint definitions
│   ├── Models/                     # Data models (if any)
│   ├── Properties/
│   │   └── launchSettings.json     # Launch configurations
│   ├── wwwroot/
│   │   └── index.html              # Static content
│   ├── ApiKeyEndpointFilter.cs     # Authentication filter
│   ├── Program.cs                  # Application entry point
│   ├── appsettings.json            # Configuration
│   └── RoslynAsAServiceAPI.csproj  # Project file
```

### Adding New Endpoints

1. Add your endpoint handler methods to `Groups/RoslynGroup.cs`
2. Register the endpoint in the `MapRoslynApi` method
3. Define any required DTOs in `ApiKeyEndpointFilter.cs` or create separate model files

### Building

```bash
# Debug build
dotnet build

# Release build
dotnet build --configuration Release
```

### Testing

The API includes a simple HTML page at the root URL to verify the service is running. For comprehensive testing, use the OpenAPI/Swagger interface or tools like Postman.

## n8n Integration

This API is specifically designed to work with n8n workflows, enabling you to automate Visual Studio project manipulation tasks. Here's how to integrate it with n8n:

### Setting up n8n Integration

1. **Start the API**: Ensure RoslynAsAServiceAPI is running locally
2. **Configure n8n HTTP Request nodes**: Use the following settings in your n8n workflow:

#### n8n HTTP Request Node Configuration

**Basic Settings:**
- **Method**: POST
- **URL**: `http://localhost:5000/api/query/syntax-nodes` (or other endpoints)
- **Authentication**: None (use headers instead)

**Headers:**
```json
{
  "Content-Type": "application/json",
  "X-API-Key": "your-api-key-here"
}
```

**Body (for syntax-nodes endpoint):**
```json
{
  "filePath": "C:\\path\\to\\your\\project\\file.cs",
  "withAttribute": null,
  "methodName": null
}
```

### Common n8n Workflow Scenarios

1. **Code Analysis Workflow**:
   - Trigger: Schedule or webhook
   - Action: Query syntax nodes in multiple files
   - Process: Analyze results and generate reports

2. **Automated Refactoring**:
   - Trigger: Git commit or file change
   - Action: Find specific code patterns
   - Process: Replace outdated patterns with new implementations

3. **Code Quality Monitoring**:
   - Trigger: Daily schedule
   - Action: Scan project files for specific attributes or patterns
   - Process: Send notifications if issues are found

### Example n8n Workflow Steps

1. **HTTP Request Node**: Call `/api/query/find-csharp-files` to get all C# files
2. **Function Node**: Process the file list
3. **HTTP Request Node**: For each file, call `/api/query/syntax-nodes`
4. **Function Node**: Analyze the syntax data
5. **Email/Slack Node**: Send results or notifications

### Best Practices for n8n Integration

- **Error Handling**: Use n8n's error handling to manage API failures
- **Batching**: Process files in batches to avoid overwhelming the API
- **Caching**: Store results in n8n's data storage to avoid repeated API calls
- **Security**: Keep your API key secure in n8n's credential system

## Security Considerations

- **API Key**: Always use a strong, unique API key in production
- **HTTPS**: Enable HTTPS in production environments
- **File Access**: The API can read and modify files on the server - ensure proper access controls
- **Input Validation**: Validate file paths to prevent directory traversal attacks

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

### Development Setup

1. Clone the repository
2. Open in Visual Studio 2022 or VS Code
3. Restore NuGet packages: `dotnet restore`
4. Set your API key in `appsettings.Development.json`
5. Run the project: `dotnet run`

### Submitting Changes

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## Support

If you have questions or need help:

- Open an issue on GitHub for bugs or feature requests
- Check the existing issues before creating a new one
- For n8n integration questions, refer to the n8n Integration section above

## Acknowledgments

- Built with [Roslyn](https://github.com/dotnet/roslyn) - Microsoft's C# compiler platform
- Designed for [n8n](https://n8n.io/) workflow automation
- Uses ASP.NET Core minimal APIs

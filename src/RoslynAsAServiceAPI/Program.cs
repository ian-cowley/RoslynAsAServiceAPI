using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using RoslynAsAServiceAPI.Groups;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http.Json;

namespace RoslynAsAServiceAPI
{
    public partial class Program
    {
        // Shared JSON serialization options
        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // Minimal DI container for CLI execution
        private static readonly Lazy<IServiceProvider> CliServices = new(() =>
        {
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddLogging();
            services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(_ => { }); // ensure JsonOptions available
            return services.BuildServiceProvider();
        });

        public static async Task<int> Main(string[] args)
        {
            // If command-line arguments are provided, run in command-line mode.
            if (args.Length > 0)
            {
                return await InvokeCommand(args);
            }

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Serve static and default files (for index.html)
            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();

            app.MapRoslynApi();
            app.MapFilesApi();

            app.Run();
            return 0;
        }

        private static async Task<int> InvokeCommand(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Usage: <command> <json_payload>");
                return 1;
            }

            var command = args[0];
            var json = args[1];
            IResult result;

            try
            {
                switch (command)
                {
                    // RoslynGroup Commands
                    case "query-syntax-nodes":
                        var querySyntaxNodes = JsonSerializer.Deserialize<SyntaxNodeQuery>(json, JsonSerializerOptions);
                        result = await RoslynGroup.QuerySyntaxNodes(querySyntaxNodes!);
                        break;
                    case "find-csharp-files":
                        var findCSharpFiles = JsonSerializer.Deserialize<FindCSharpFilesQuery>(json, JsonSerializerOptions);
                        result = await RoslynGroup.FindCSharpFiles(findCSharpFiles!);
                        break;
                    case "replace-range":
                        var replaceRange = JsonSerializer.Deserialize<ReplaceRangeCommand>(json, JsonSerializerOptions);
                        result = await RoslynGroup.ReplaceRange(replaceRange!);
                        break;

                    // FilesGroup Commands
                    case "find-files":
                        var findFiles = JsonSerializer.Deserialize<FindFilesQuery>(json, JsonSerializerOptions);
                        result = await FilesGroup.FindFiles(findFiles!);
                        break;
                    case "replace-text":
                        var replaceText = JsonSerializer.Deserialize<ReplaceTextCommand>(json, JsonSerializerOptions);
                        result = await FilesGroup.ReplaceText(replaceText!);
                        break;
                    case "insert-text":
                        var insertText = JsonSerializer.Deserialize<InsertTextCommand>(json, JsonSerializerOptions);
                        result = await FilesGroup.InsertText(insertText!);
                        break;
                    case "create-file":
                        var createFile = JsonSerializer.Deserialize<CreateFileCommand>(json, JsonSerializerOptions);
                        result = await FilesGroup.CreateFile(createFile!);
                        break;

                    default:
                        Console.Error.WriteLine($"Unknown command: {command}");
                        return 1;
                }
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"Error deserializing JSON payload: {ex.Message}");
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"An unexpected error occurred: {ex.Message}");
                return 1;
            }

            return await HandleResult(result);
        }

        private static async Task<int> HandleResult(IResult result)
        {
            var httpContext = new DefaultHttpContext
            {
                RequestServices = CliServices.Value
            };
            httpContext.Response.Body = new MemoryStream();

            await result.ExecuteAsync(httpContext);

            var response = httpContext.Response;
            response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(response.Body);
            var body = await reader.ReadToEndAsync();

            if (response.StatusCode is >= 200 and < 300)
            {
                if (!string.IsNullOrEmpty(body))
                {
                    Console.WriteLine(body);
                }
                return 0;
            }
            else
            {
                if (!string.IsNullOrEmpty(body))
                {
                    Console.Error.WriteLine(body);
                }
                return 1;
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace RoslynAsAServiceAPI.Groups
{
    public static class FilesGroup
    {
        public static void MapFilesApi(this IEndpointRouteBuilder app)
        {
            var apiGroup = app.MapGroup("/api/files")
                .AddEndpointFilter<ApiKeyEndpointFilter>();

            // Define endpoints here
            apiGroup.MapPost("/find", FindFiles).WithTags("File System");
            apiGroup.MapPost("/replace-text", ReplaceText).WithTags("File System");
            apiGroup.MapPost("/insert-text", InsertText).WithTags("File System");
            apiGroup.MapPost("/create-file", CreateFile).WithTags("File System");
        }

        private static async Task<IResult> CreateFile([FromBody] CreateFileCommand command)
        {
            try
            {
                await File.WriteAllTextAsync(command.FilePath, command.Content);
                return Results.Ok(new { message = "File created successfully." });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error creating file: {ex.Message}");
            }
        }

        private static async Task<IResult> InsertText([FromBody] InsertTextCommand command)
        {
            if (!File.Exists(command.FilePath))
            {
                return Results.NotFound(new { message = "File not found.", file = command.FilePath });
            }

            try
            {
                var lines = (await File.ReadAllLinesAsync(command.FilePath)).ToList();
                if (command.LineNumber < 1 || command.LineNumber > lines.Count + 1)
                {
                    return Results.BadRequest(new { message = "Line number is out of bounds." });
                }
                lines.Insert(command.LineNumber - 1, command.TextToInsert);
                await File.WriteAllLinesAsync(command.FilePath, lines);
                return Results.Ok(new { message = "Text inserted successfully." });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error inserting text: {ex.Message}");
            }
        }

        private static async Task<IResult> ReplaceText([FromBody] ReplaceTextCommand command)
        {
            if (!File.Exists(command.FilePath))
            {
                return Results.NotFound(new { message = "File not found.", file = command.FilePath });
            }

            try
            {
                var content = await File.ReadAllTextAsync(command.FilePath);
                content = content.Replace(command.OldText, command.NewText);
                await File.WriteAllTextAsync(command.FilePath, content);
                return Results.Ok(new { message = "File updated successfully." });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error replacing text: {ex.Message}");
            }
        }

        private static Task<IResult> FindFiles([FromBody] FindFilesQuery query)
        {
            if (!Directory.Exists(query.Path))
            {
                return Task.FromResult(Results.NotFound(new { message = "Directory not found.", path = query.Path }));
            }

            try
            {
                var searchPattern = $"*.{query.Extension.TrimStart('.')}";
                var files = Directory.GetFiles(query.Path, searchPattern, SearchOption.AllDirectories).ToList();
                return Task.FromResult(Results.Ok(new FindFilesResponse(files)));
            }
            catch (Exception ex)
            {
                return Task.FromResult(Results.Problem($"Error searching for files: {ex.Message}"));
            }
        }
    }

    // --- DTOs ---
    public record FindFilesQuery(string Path, string Extension);
    public record FindFilesResponse(List<string> Files);
    public record ReplaceTextCommand(string FilePath, string OldText, string NewText);
    public record InsertTextCommand(string FilePath, int LineNumber, string TextToInsert);
    public record CreateFileCommand(string FilePath, string Content);
}

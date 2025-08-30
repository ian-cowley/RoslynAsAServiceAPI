using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynAsAServiceAPI.Groups;

public static class RoslynGroup
{
    public static void MapRoslynApi(this IEndpointRouteBuilder app)
    {
        // Create a group for all Roslyn-related endpoints
        var apiGroup = app.MapGroup("/api")
            // Apply the API Key filter to the entire group
            .AddEndpointFilter<ApiKeyEndpointFilter>();

        // Sub-group for read-only query operations
        var queryGroup = apiGroup.MapGroup("/query").WithTags("Roslyn Queries");
        queryGroup.MapPost("/syntax-nodes", QuerySyntaxNodes);
        queryGroup.MapPost("/find-csharp-files", FindCSharpFiles);

        // Sub-group for write/modify actions
        var actionGroup = apiGroup.MapGroup("/actions").WithTags("Roslyn Actions");
        actionGroup.MapPost("/replace-range", ReplaceRange);
    }

    // --- Public Endpoint Handlers ---
    /// <summary>
    /// Queries syntax nodes in a C# file with optional filtering by attribute and method name.
    /// </summary>
    /// <param name="query">The query parameters including file path, optional attribute filter, and optional method name filter.</param>
    /// <returns>A list of found nodes with their details, locations, and attributes.</returns>
    public static async Task<IResult> QuerySyntaxNodes([FromBody] SyntaxNodeQuery query)
    {
        if (!File.Exists(query.FilePath))
        {
            return Results.NotFound(new { message = "File not found.", file = query.FilePath });
        }

        var foundNodes = new List<FoundNode>();
        string code = await File.ReadAllTextAsync(query.FilePath);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            bool hasAttribute = string.IsNullOrEmpty(query.WithAttribute) || method.AttributeLists
                .SelectMany(list => list.Attributes)
                .Any(attr => attr.Name.ToString() == query.WithAttribute);
            bool hasMethodName = string.IsNullOrEmpty(query.MethodName) || method.Identifier.Text == query.MethodName;

            if (hasAttribute && hasMethodName)
            {
                // Include all leading trivia (comments, whitespace) in the span
                var leadingTrivia = method.GetLeadingTrivia();
                int start = leadingTrivia.Count > 0 ? leadingTrivia.Min(t => t.SpanStart) : method.Span.Start;
                int end = method.Span.End;
                var fullSpan = new Microsoft.CodeAnalysis.Text.TextSpan(start, end - start);
                FileLinePositionSpan lineSpan = tree.GetLineSpan(fullSpan);

                // Extract FullText using the calculated span from the source code
                string fullText = code.Substring(fullSpan.Start, fullSpan.Length);

                var attributes = method.AttributeLists
                                    .SelectMany(l => l.Attributes)
                                    .Select(a => a.ToString())
                                    .ToList();

                foundNodes.Add(new FoundNode(
                    FilePath: query.FilePath,
                    NodeType: "Method",
                    Name: method.Identifier.Text,
                    FullText: fullText,
                    Location: new NodeLocation(
                        StartLine: lineSpan.StartLinePosition.Line + 1,
                        EndLine: lineSpan.EndLinePosition.Line + 1
                    ),
                    Attributes: attributes
                ));
            }
        }
        return Results.Ok(foundNodes);
    }

    /// <summary>
    /// Replaces a range of lines in a C# file with new text content.
    /// </summary>
    /// <param name="command">The command containing file path, line range, and replacement text.</param>
    /// <returns>Success result if the operation completes successfully.</returns>
    public static async Task<IResult> ReplaceRange([FromBody] ReplaceRangeCommand command)
    {
        if (!File.Exists(command.FilePath))
        {
            return Results.NotFound(new { message = "File not found.", file = command.FilePath });
        }

        string tempFilePath = command.FilePath + ".tmp";
        try
        {
            var allLines = (await File.ReadAllLinesAsync(command.FilePath)).ToList();
            int startIndex = command.StartLine - 1;
            int count = command.EndLine - startIndex;

            if (startIndex < 0 || count <= 0 || (startIndex + count) > allLines.Count)
            {
                return Results.BadRequest(new { message = "Line range is out of bounds for the file." });
            }

            allLines.RemoveRange(startIndex, count);
            var newLines = command.NewText.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
            allLines.InsertRange(startIndex, newLines);

            // Write to a temp file using FileStream and StreamWriter
            using (var stream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream))
            {
                foreach (var line in allLines)
                {
                    await writer.WriteLineAsync(line);
                }
            }

            // Retry copy/delete if file is locked
            int retries = 5;
            int delayMs = 100;
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    File.Copy(tempFilePath, command.FilePath, overwrite: true);
                    File.Delete(tempFilePath);
                    return Results.Ok(new { message = "File updated successfully." });
                }
                catch (IOException)
                {
                    if (i == retries - 1) throw;
                    await Task.Delay(delayMs);
                }
            }

            return Results.Problem("Unexpected error during file update.");
        }
        catch (IOException ex)
        {
            return Results.Problem($"File access error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }

    /// <summary>
    /// Recursively finds all C# files in a given directory.
    /// </summary>
    /// <param name="query">The query containing the root directory to search.</param>
    /// <returns>A list of full file paths.</returns>
    public static Task<IResult> FindCSharpFiles([FromBody] FindCSharpFilesQuery query)
    {
        if (!Directory.Exists(query.Path))
        {
            return Task.FromResult(Results.NotFound(new { message = "Directory not found.", path = query.Path }));
        }

        try
        {
            // Search for all .cs files in the directory and all subdirectories.
            var files = Directory.GetFiles(query.Path, "*.cs", SearchOption.AllDirectories).ToList();

            return Task.FromResult(Results.Ok(new FindCSharpFilesResponse(files)));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Task.FromResult(Results.Problem($"Access denied to directory: {ex.Message}", statusCode: 403));
        }
        catch (DirectoryNotFoundException ex)
        {
            return Task.FromResult(Results.NotFound(new { message = "Directory not found.", path = query.Path, error = ex.Message }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Results.Problem($"Error searching for C# files: {ex.Message}"));
        }
    }
}

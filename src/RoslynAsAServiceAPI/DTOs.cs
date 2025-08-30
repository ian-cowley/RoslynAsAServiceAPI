namespace RoslynAsAServiceAPI;

// --- DTOs / Models for the API ---
public record SyntaxNodeQuery(string FilePath, string? WithAttribute, string? MethodName);
public record ReplaceRangeCommand(string FilePath, int StartLine, int EndLine, string NewText);
public record FoundNode(string FilePath, string NodeType, string Name, string FullText, NodeLocation Location, List<string> Attributes);
public record NodeLocation(int StartLine, int EndLine);
public record FindCSharpFilesQuery(string Path);
public record FindCSharpFilesResponse(List<string> Files);

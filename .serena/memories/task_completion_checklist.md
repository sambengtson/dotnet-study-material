# Task Completion Checklist

When a task is completed, verify the following:

1. **Build succeeds**: Run `dotnet build` (or `dotnet build src/XX-TopicName` for the specific project)
2. **Project runs**: Run `dotnet run --project src/XX-TopicName` and verify console output is sensible
3. **Code style**: Ensure top-level statements are at the end of the file (CS8803), modern C# features are used, and `// INTERVIEW ANSWER:` comments are present for new concepts
4. **No unnecessary comments**: Only add comments when the implementation is necessarily complex
5. **Solution file**: If a new project was added, ensure it's registered in `dotnet-study-material.sln`

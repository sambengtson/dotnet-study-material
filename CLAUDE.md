# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A monorepo of 30 self-contained C# console applications for interview preparation. Each project lives in `src/XX-TopicName/` with a single `Program.cs` file demonstrating a concept with inline `// INTERVIEW ANSWER:` comments.

## Build & Run Commands

```bash
# Build entire solution
dotnet build

# Build one project
dotnet build src/01-Inheritance

# Run one project
dotnet run --project src/01-Inheritance
```

There are no tests — each project is its own runnable demonstration.

## Technical Details

- **.NET 10 Preview** (`net10.0`) — requires .NET 10 SDK
- **Nullable reference types** and **implicit usings** enabled in all projects
- Only external dependency: `Microsoft.Extensions.DependencyInjection` in project 14
- No `Directory.Build.props` or shared build configuration

## Code Structure Convention

Every `Program.cs` follows the same layout:
1. Header comment block with topic title and interview answer
2. Top-level statements that run the demo
3. Type definitions (classes, interfaces, records) demonstrating the concept at the **end** of the file

**Critical:** Top-level statements must appear **before** all type declarations (CS8803 constraint). When editing, keep executable statements above and namespace/type definitions below.

## C# Feature Usage

Projects use modern C# 12 / .NET 10 features: primary constructors, collection expressions `[...]`, record types, pattern matching, switch expressions, file-scoped namespaces, and raw string literals `"""..."""`. Follow these conventions when adding or modifying code.

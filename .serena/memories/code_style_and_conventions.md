# Code Style & Conventions

## File Structure (every Program.cs)
1. Header comment block with topic title and `// INTERVIEW ANSWER:` explanation
2. Type definitions (classes, interfaces, records, enums) demonstrating the concept
3. Top-level statements at the **end** of the file that run the demo

**Critical rule:** Top-level statements must appear **after** all type declarations (CS8803 constraint).

## Naming
- Standard C# conventions: PascalCase for types, methods, properties; camelCase for locals/parameters
- Projects named `XX-TopicName` (e.g. `01-Inheritance`, `10-StrategyPattern`)

## Comments
- `// INTERVIEW ANSWER:` inline comments explain concepts as interview-style answers
- Header block uses `// ===...===` separator lines
- Do not add unnecessary code comments — only add comments when an implementation is necessarily complex

## C# Features to Use
- Primary constructors
- Collection expressions `[...]`
- Record types
- Pattern matching & switch expressions
- File-scoped namespaces
- Raw string literals `"""..."""`
- Modern C# 12 / .NET 10 idioms

## csproj Template
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

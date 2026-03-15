# Project Overview: dotnet-study-material

## Purpose
A monorepo of 30 self-contained C# console applications for .NET interview preparation. Each project demonstrates a specific concept (OOP principles, design patterns, async, LINQ, etc.) with inline `// INTERVIEW ANSWER:` comments.

## Tech Stack
- **.NET 10 Preview** (`net10.0`)
- **C# 12** with modern features: primary constructors, collection expressions `[...]`, record types, pattern matching, switch expressions, file-scoped namespaces, raw string literals `"""..."""`.
- **Nullable reference types** and **implicit usings** enabled in all projects.
- Only external dependency: `Microsoft.Extensions.DependencyInjection` (v9.0.0) in project 14.
- No `Directory.Build.props` or shared build configuration.
- No test projects — each project is its own runnable demonstration.

## IDE
JetBrains Rider (macOS / Darwin)

## Project List (src/)
| # | Topic |
|---|-------|
| 01 | Inheritance |
| 02 | Polymorphism |
| 03 | Abstraction |
| 04 | Encapsulation |
| 05 | Interfaces |
| 06 | Generics |
| 07 | DelegatesAndEvents |
| 08 | LINQ |
| 09 | AsyncAwait |
| 10 | StrategyPattern |
| 11 | FactoryPattern |
| 12 | ObserverPattern |
| 13 | SingletonPattern |
| 14 | DependencyInjection |
| 15 | SOLIDPrinciples |
| 16 | CollectionsAndDataStructures |
| 17 | ExceptionHandling |
| 18 | RecordsAndValueTypes |
| 19 | PatternMatching |
| 20 | ConcurrencyAndChannels |
| 21 | BuilderPattern |
| 22 | PrototypePattern |
| 23 | AdapterPattern |
| 24 | DecoratorPattern |
| 25 | FacadePattern |
| 26 | CompositePattern |
| 27 | ProxyPattern |
| 28 | CommandPattern |
| 29 | StatePattern |
| 30 | TemplateMethodPattern |

# .NET Study Material

A collection of 20 self-contained C# console applications designed to help experienced engineers practice articulating fundamental programming concepts for interviews. Each project pairs runnable code examples with clear "interview answer" explanations.

**Target Framework:** .NET 10 Preview (`net10.0`)

## How to Run

Each project is independent. Navigate to any project directory and run:

```bash
cd src/01-Inheritance && dotnet run
```

Or run from the repo root:

```bash
dotnet run --project src/01-Inheritance
```

## Table of Contents

| # | Topic | Description |
|---|-------|-------------|
| 01 | [Inheritance](src/01-Inheritance/) | Base/derived classes, virtual/override, sealed, constructor chaining, type checking |
| 02 | [Polymorphism](src/02-Polymorphism/) | Runtime vs compile-time polymorphism, treating different types through a common base |
| 03 | [Abstraction](src/03-Abstraction/) | Abstract classes, hiding complexity behind a public API, depending on abstractions |
| 04 | [Encapsulation](src/04-Encapsulation/) | Access modifiers, property patterns, protecting invariants, controlled APIs |
| 05 | [Interfaces](src/05-Interfaces/) | Contracts, multiple implementation, default interface methods, IDisposable |
| 06 | [Generics](src/06-Generics/) | Generic classes/methods, constraints, covariance/contravariance, Result\<T\> |
| 07 | [Delegates and Events](src/07-DelegatesAndEvents/) | Action, Func, events, lambdas, multicast delegates, pipelines |
| 08 | [LINQ](src/08-LINQ/) | Method/query syntax, deferred execution, common operators, custom extensions |
| 09 | [Async/Await](src/09-AsyncAwait/) | Task, ValueTask, cancellation, ConfigureAwait, async streams |
| 10 | [Strategy Pattern](src/10-StrategyPattern/) | Family of algorithms, encapsulation, runtime swapping via interfaces |
| 11 | [Factory Pattern](src/11-FactoryPattern/) | Factory Method, Abstract Factory, decoupling creation from usage |
| 12 | [Observer Pattern](src/12-ObserverPattern/) | Manual implementation, events/delegates, pub/sub decoupling |
| 13 | [Singleton Pattern](src/13-SingletonPattern/) | Thread-safe implementations, Lazy\<T\>, anti-pattern discussion |
| 14 | [Dependency Injection](src/14-DependencyInjection/) | Constructor injection, MS DI container, service lifetimes |
| 15 | [SOLID Principles](src/15-SOLIDPrinciples/) | One example per principle with violation and fix |
| 16 | [Collections & Data Structures](src/16-CollectionsAndDataStructures/) | List, Dictionary, HashSet, Queue, Stack, PriorityQueue, Frozen collections, Big-O |
| 17 | [Exception Handling](src/17-ExceptionHandling/) | try/catch/finally/when, custom exceptions, filters, Result types |
| 18 | [Records and Value Types](src/18-RecordsAndValueTypes/) | record vs struct vs class, value/reference equality, Span\<T\> |
| 19 | [Pattern Matching](src/19-PatternMatching/) | Switch expressions, property/tuple/list patterns, exhaustiveness |
| 20 | [Concurrency and Channels](src/20-ConcurrencyAndChannels/) | Channel\<T\>, producer/consumer, SemaphoreSlim, thread safety |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (Preview)

## Notes

- Each `Program.cs` contains interview-ready explanations as comments
- All code uses modern C# features (top-level statements, primary constructors, etc.)
- Examples use real-world scenarios (payment processing, caching, APIs, etc.)
- Every project is fully self-contained in a single `Program.cs` file

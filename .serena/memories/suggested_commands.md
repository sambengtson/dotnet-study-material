# Suggested Commands

## Build
```bash
# Build entire solution
dotnet build

# Build a single project
dotnet build src/01-Inheritance
```

## Run
```bash
# Run a specific project
dotnet run --project src/01-Inheritance

# Run any project by replacing the folder name
dotnet run --project src/XX-TopicName
```

## No Tests
There are no test projects. Each project is verified by running it and checking its console output.

## Git
```bash
git status
git log --oneline -10
git diff
git add <file>
git commit -m "message"
```

## System (macOS / Darwin)
```bash
ls -la
find . -name "*.cs"
grep -r "pattern" src/
```

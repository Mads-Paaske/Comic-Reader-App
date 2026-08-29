#!/usr/bin/env pwsh
# Run the whole unit test suite. Pass extra args straight through to `dotnet test`,
# e.g.  ./test.ps1 --filter "FullyQualifiedName~BookDatabaseTests"
dotnet test "$PSScriptRoot/tests/ComicReaderApp.Tests/ComicReaderApp.Tests.csproj" @args

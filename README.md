# Echo

by [@LMarshallAfzal](https://github.com/lmarshallafzal)

<!-- TODO: Tech stack icons go here for technologies used -->

## Features

## Usage

To start the Echo websocket server
```bash
    cd Echo.App/

    dotnet build
    dotnet run
```

To test the source code do the following
```bash
    cd Echo.Tests

    dotnet test
```
To get coverage ()
```bash 
    dotnet test --collect:"XPlat Code Coverage"

    reportgenerator
    "-reports:Echo.Tests/TestResults/<coverage-id>/coverage.cobertura.xml" 
    "-targetdir:coveragereport" 
    -reporttypes:Html
```

## Docs

## Deployment
# Mineswepper Service

This is a microservice for the BucStop web application that provides a classic Mineswepper game.

## Development Setup

1. Ensure you have .NET 6.0 SDK or later installed.
2. Clone the repository.
3. Navigate to the Mineswepper directory.
4. Run `dotnet restore` to restore the packages.
5. Run `dotnet build` to build the project.
6. Run `dotnet run` to start the service locally.

## Docker Setup

To build and run with Docker:

```bash
docker build -t game-mineswepper .
docker run -p 8085:80 game-mineswepper
```

## Integration with BucStop

This service integrates with the BucStop web application via the API Gateway. The gateway collects game information from this service and makes it available to the main application. 
# GameInfoService

A microservice that dynamically serves game information by reading from the SubmissionGateway's data files. This service eliminates the need to manually edit HTML when adding new games.

## Overview

GameInfoService reads game metadata from `submissions.jsonl` and serves it via REST API endpoints. When new games are submitted through the SubmissionGateway, they automatically appear in the API without requiring code changes.

## Architecture

- **Data Source**: Reads from `Team-3-BucStop_SubmissionGateway/SubmissionGateway/Data/submissions.jsonl`
- **File Storage**: Accesses uploaded files from `Team-3-BucStop_SubmissionGateway/SubmissionGateway/Uploads`
- **API**: RESTful endpoints for retrieving game information
- **Auto-refresh**: Uses file system watchers to automatically detect new submissions

## API Endpoints

### GET /api/games
Returns a list of all games with their metadata.

**Response:**
```json
[
  {
    "id": 1,
    "title": "Test Game",
    "author": "Submitted User",
    "content": "/api/files/game.js",
    "description": "A test game",
    "dateAdded": "2025-11-20",
    "howTo": "Check the game files for instructions.",
    "thumbnail": "/api/files/thumbnail.png",
    "leaderBoardStack": []
  }
]
```

### GET /api/games/{id}
Returns details for a specific game by ID.

**Response:**
```json
{
  "id": 1,
  "title": "Test Game",
  "author": "Submitted User",
  "content": "/api/files/game.js",
  "description": "A test game",
  "dateAdded": "2025-11-20",
  "howTo": "Check the game files for instructions.",
  "thumbnail": "/api/files/thumbnail.png",
  "leaderBoardStack": []
}
```

### GET /api/files/{filename}
Serves uploaded files (images, code files) from the Uploads directory.

## Data Model

The service reads from the `Submission` model stored in `submissions.jsonl`:

```json
{
  "Id": "guid",
  "GameName": "string",
  "Description": "string",
  "ImagePath": "string",
  "CodeFilePath": "string",
  "CreatedEst": "datetime",
  "UpdatedEst": "datetime"
}
```

And converts it to the `GameInfo` model:

```csharp
{
  Id: int,
  Title: string,
  Author: string,
  Content: string,      // URL to game code file
  Description: string,
  DateAdded: string,
  HowTo: string,
  Thumbnail: string,    // URL to thumbnail image
  LeaderBoardStack: Stack<KeyValuePair<string, int>>
}
```

## Configuration

### appsettings.json (Local Development)
```json
{
  "Storage": {
    "DataPath": "../../Team-3-BucStop_SubmissionGateway/SubmissionGateway/Data",
    "UploadsPath": "../../Team-3-BucStop_SubmissionGateway/SubmissionGateway/Uploads"
  }
}
```

### appsettings.containersLocal.json / appsettings.containers.json (Docker)
```json
{
  "Storage": {
    "DataPath": "/app/shared-data/data",
    "UploadsPath": "/app/shared-data/uploads"
  }
}
```

## Running the Service

### Using Docker Compose (Recommended)

The service is automatically included in the main `docker-compose.yml`:

```bash
docker-compose up
```

The service will be available at: `http://localhost:8087`

### Local Development (Without Docker)

1. Navigate to the service directory:
   ```bash
   cd Team-3-BucStop_GameInfoService/GameInfoService
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Run the service:
   ```bash
   dotnet run
   ```

4. The service will be available at: `http://localhost:5000` (or the port specified in `launchSettings.json`)

### Prerequisites

- .NET 8.0 SDK (for local development)
- Docker and Docker Compose (for containerized deployment)
- Access to the SubmissionGateway's Data and Uploads directories

## Integration with Existing Services

### Updating the API Gateway

To integrate GameInfoService with the existing API Gateway, you can modify the `GatewayController` to also fetch games from GameInfoService:

```csharp
// In GatewayController.cs
var gameInfoServiceUrl = _config["Microservices:GameInfoService"] ?? "http://gameinfo-service";
var submittedGames = await FetchGamesFromGameInfoService(gameInfoServiceUrl);
_gameInfos.AddRange(submittedGames);
```

### Updating the Frontend

The frontend can call GameInfoService directly or through the API Gateway:

```javascript
// Fetch all games
fetch('http://localhost:8087/api/games')
  .then(response => response.json())
  .then(games => {
    // Display games
  });

// Fetch specific game
fetch('http://localhost:8087/api/games/1')
  .then(response => response.json())
  .then(game => {
    // Display game details
  });
```

## Dynamic Loading

The service automatically detects new submissions:

1. **File System Watcher**: Monitors `submissions.jsonl` for changes
2. **Auto-refresh**: When a new submission is added, the service refreshes its cache
3. **No Code Changes**: New games appear in the API without restarting the service

## Project Structure

```
Team-3-BucStop_GameInfoService/
├── GameInfoService/
│   ├── Controllers/
│   │   └── GamesController.cs      # API endpoints
│   ├── Program.cs                   # Service configuration
│   ├── GameInfoService.csproj      # Project file
│   ├── Dockerfile                   # Container definition
│   └── appsettings*.json           # Configuration files
└── README.md                        # This file
```

## Troubleshooting

### Service can't find data files
- Ensure the `Storage:DataPath` and `Storage:UploadsPath` are correctly configured
- In Docker, verify volumes are mounted correctly in `docker-compose.yml`
- Check file permissions on the Data and Uploads directories

### Games not appearing after submission
- Verify the file system watcher is working (check logs)
- Manually refresh by calling the refresh endpoint (if implemented) or restart the service
- Check that `submissions.jsonl` is being written to by SubmissionGateway

### Port conflicts
- Default port is 8087
- Change the port mapping in `docker-compose.yml` if needed
- For local development, update `launchSettings.json`

## Future Enhancements

- Add caching with configurable TTL
- Implement pagination for the games list endpoint
- Add filtering and sorting options
- Support for game metadata updates
- Health check endpoint
- Metrics and monitoring integration


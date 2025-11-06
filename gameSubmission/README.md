# BucStop WebApp - C# ASP.NET Core

A game submission platform built with C# ASP.NET Core, running in Docker with persistent storage.

## Features
- Submit games with a name
- Automatically creates a folder for each game in a Docker volume
- Optional file uploads (game files, assets, etc.)
- Persistent storage across container restarts

## Quick Start

```bash
docker-compose up -d --build
```

Access at: `http://localhost:8080`

## How It Works

1. **Home Page** - Welcome page with link to submit games
2. **Submit Game** - Enter game name, upload files (optional)
3. **Storage** - Creates `/app/games/{game-name}/` in Docker volume

## Commands

**Build and run:**
```bash
docker-compose up -d --build
```

**Stop:**
```bash
docker-compose down
```

**View logs:**
```bash
docker-compose logs -f
```

**View submitted games:**
```bash
docker exec -it bucstop-webapp ls -la /app/games
```

## Files Needed
- Program.cs
- GameSubmissionController.cs
- BucStopWebApp.csproj
- index.html
- gameSubmit.html
- Dockerfile
- docker-compose.yml


# how to run
docker-compose up -d

# Check they're running
docker ps

# View logs
docker logs bucstop-webapp

# container 
docker exec -it bucstop-webapp ls -la /app/games

docker volume inspect gamesubmission_game-data

cd /app/games
ls -la

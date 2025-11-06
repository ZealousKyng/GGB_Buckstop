# Submission Gateway

A lightweight microservice for handling game submission data. It validates and persists submissions to a file (no database). Designed for independent use or as a proxy-backed microservice in a larger system.

## Overview
- Accepts create, update, and list requests for game submissions.
- File-backed: persists data as JSON lines (`Data/submissions.jsonl`).
- Input validation to prevent bad/malicious data.
- Runs independently (dotnet/Docker) and supports API Gateway integration.

## Key Files
- `Program.cs`: Configures services, file store, rate limiting, and endpoint registration.
- `Controllers/SubmissionsController.cs`: Defines the HTTP API (POST/PUT/GET endpoints) and input validation.
- `appsettings.json`: Controls file path for persistence under `Storage:FilePath`.
- `Data/submissions.jsonl`: Flat file storage of all submissions, created and managed at runtime.
- `Dockerfile`: Container build for shipping the service.
- `SubmissionGateway.csproj`: Project and dependency information.

## Endpoints

**Base URL:** `http://localhost:8085` (default, configurable via `ASPNETCORE_URLS`)

All datetime fields below (CreatedEst, UpdatedEst) are in US Eastern Time.

### POST /api/submissions
Create a new submission.

**Request:**
```powershell
$body = @{ game = "Snake"; userId = "user123"; score = 1234 } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:8085/api/submissions" -Method Post -Body $body -ContentType "application/json"
```

**Request Body:**
```json
{
  "game": "string",
  "userId": "string",
  "score": 1234
}
```

**Response 201 (Created):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "game": "Snake",
  "userId": "user123",
  "score": 1234,
  "createdEst": "2024-06-01T18:15:04",
  "updatedEst": "2024-06-01T18:15:04"
}
```

**Response 400 (Bad Request) - Validation Error:**
```json
{
  "error": "Invalid game"
}
```

---

### PUT /api/submissions/{id}
Update score of existing submission.

**Request:**
```powershell
$body = @{ score = 1500 } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:8085/api/submissions/550e8400-e29b-41d4-a716-446655440000" -Method Put -Body $body -ContentType "application/json"
```

**Request Body:**
```json
{
  "score": 1500
}
```

**Response 200 (OK):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "game": "Snake",
  "userId": "user123",
  "score": 1500,
  "createdEst": "2024-06-01T18:15:04",
  "updatedEst": "2024-06-01T18:20:30"
}
```

**Response 404 (Not Found):**
```json
{
  "error": "Submission not found"
}
```

**Response 400 (Bad Request) - Validation Error:**
```json
{
  "error": "Invalid score"
}
```

---

### GET /api/submissions/{id}
Fetch a single submission by ID.

**Request:**
```powershell
Invoke-RestMethod -Uri "http://localhost:8085/api/submissions/550e8400-e29b-41d4-a716-446655440000" -Method Get
```

**Response 200 (OK):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "game": "Snake",
  "userId": "user123",
  "score": 1500,
  "createdEst": "2024-06-01T18:15:04",
  "updatedEst": "2024-06-01T18:20:30"
}
```

**Response 404 (Not Found):**
```json
{
  "error": "Submission not found"
}
```

---

### GET /api/submissions
List submissions with optional filtering.

**Query Parameters:**
- `game` (optional) - Filter by game name
- `userId` (optional) - Filter by user ID
- `take` (optional) - Max results to return (default: 50, max: 500)

**Request Examples:**
```powershell
# Get all submissions (default 50)
Invoke-RestMethod -Uri "http://localhost:8085/api/submissions" -Method Get

# Get submissions for a specific game
Invoke-RestMethod -Uri "http://localhost:8085/api/submissions?game=Snake" -Method Get

# Get submissions for a specific user
Invoke-RestMethod -Uri "http://localhost:8085/api/submissions?userId=user123" -Method Get

# Get top 10 submissions for a game
Invoke-RestMethod -Uri "http://localhost:8085/api/submissions?game=Snake&take=10" -Method Get

# Get submissions for a specific user and game
Invoke-RestMethod -Uri "http://localhost:8085/api/submissions?game=Snake&userId=user123&take=20" -Method Get
```

**Response 200 (OK):**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "game": "Snake",
    "userId": "user123",
    "score": 1500,
    "createdEst": "2024-06-01T18:15:04",
    "updatedEst": "2024-06-01T18:20:30"
  },
  {
    "id": "660e8400-e29b-41d4-a716-446655440001",
    "game": "Snake",
    "userId": "user456",
    "score": 1200,
    "createdEst": "2024-06-01T17:10:00",
    "updatedEst": "2024-06-01T17:10:00"
  }
]
```

Results are ordered by:
1. Score (descending)
2. UpdatedEst (descending)

**Response 400 (Bad Request) - Invalid take parameter:**
```json
{
  "error": "Invalid take value"
}
```

---

### Error Responses Summary

| Status Code | Description | Response Body |
|------------|-------------|---------------|
| 400 | Validation error (invalid input) | `{ "error": "Invalid game" }` or `{ "error": "Invalid userId" }` or `{ "error": "Invalid score" }` |
| 404 | Submission not found | `{ "error": "Submission not found" }` |
| 429 | Rate limit exceeded (20 requests per 10 seconds) | Standard rate limit response |

---

### Testing Endpoints

You can test the endpoints using:

1. **PowerShell Invoke-RestMethod** (examples above)
2. **Swagger UI** (when running): Navigate to `http://localhost:8085/swagger` for an interactive API documentation interface
3. **Postman** or any REST client

### Automated Test Scripts

Ready-to-use test scripts are provided to test all endpoints and verify JSON responses:

#### PowerShell Script (Windows)
```powershell
# Run all endpoint tests
.\test-endpoints.ps1

# Or specify a custom base URL
.\test-endpoints.ps1 -baseUrl "http://localhost:8085"
```

**Features:**
- Tests all 4 endpoints (POST, GET, PUT, GET with filters)
- Validates JSON responses
- Tests error cases (invalid IDs, validation errors)
- Color-coded output with pass/fail summary
- Automatically captures submission IDs for testing dependent endpoints

**Requirements:**
- PowerShell 5.1+ (Windows) or PowerShell Core

The script tests:
1. Create submission (POST)
2. List all submissions (GET)
3. Get submission by ID (GET)
4. Update submission (PUT)
5. Filter by game (GET with query)
6. Filter by userId (GET with query)
7. Filter with take limit (GET with query)
8. Error handling - Invalid ID (404)
9. Error handling - Invalid request (400)

### Verified Endpoints (Tested)

All endpoints have been tested and verified to return JSON responses:

 **POST /api/submissions** - Creates new submissions, returns 201 with JSON submission object  
 **GET /api/submissions** - Lists all submissions, returns 200 with JSON array  
 **GET /api/submissions/{id}** - Retrieves single submission, returns 200 with JSON object  
 **PUT /api/submissions/{id}** - Updates submission score, returns 200 with JSON object  
 **GET /api/submissions?game={name}** - Filters by game, returns 200 with JSON array

**Example Test Results:**
```json
// POST Response
{
  "id": "15262ddc-8b3a-4406-aed3-a430a1941ca6",
  "game": "Snake",
  "userId": "testuser",
  "score": 1234,
  "createdEst": "2025-11-05T21:44:45.3080275",
  "updatedEst": "2025-11-05T21:44:45.3080275"
}

// GET List Response
[
  {
    "id": "15262ddc-8b3a-4406-aed3-a430a1941ca6",
    "game": "Snake",
    "userId": "testuser",
    "score": 1500,
    "createdEst": "2025-11-05T21:44:45.3080275",
    "updatedEst": "2025-11-05T21:45:08.6899121"
  }
]
```

## Input Validation
- `game`: Required, max 64 chars
- `userId`: Required, max 128 chars
- `score`: 0 <= score <= 1,000,000,000
- Invalid input gives 400 error

## Persistence
- Implements `ISubmissionStore` and `FileSubmissionStore` in code
- Stores each entry as 1 JSON object/line in `Data/submissions.jsonl`
- Appends a line on create, rewrites the file for updates
- File loaded into memory at startup for fast reads

## Rate Limiting
- Fixed window: 20 submissions per 10 seconds per instance

## Running the Service
- Local dev:
  ```powershell
  cd Team-3-BucStop_SubmissionGateway/SubmissionGateway
  $env:ASPNETCORE_URLS="http://localhost:8085"
  dotnet run
  ```

## Quick Test - One-Liners

Test each endpoint with these one-liner commands. All commands return JSON responses.

**PowerShell (Windows):**

```powershell
# 1. POST - Create a new submission
$body = @{ game = "Snake"; userId = "testuser"; score = 1234 } | ConvertTo-Json; Invoke-RestMethod -Uri "http://localhost:8085/api/submissions" -Method Post -Body $body -ContentType "application/json" | ConvertTo-Json

# 2. GET - List all submissions
Invoke-RestMethod -Uri "http://localhost:8085/api/submissions" -Method Get | ConvertTo-Json

# 3. GET - Get submission by ID (replace {id} with actual submission ID from step 1)
Invoke-RestMethod -Uri "http://localhost:8085/api/submissions/{id}" -Method Get | ConvertTo-Json

# 4. PUT - Update submission score (replace {id} with actual submission ID)
$body = @{ score = 1500 } | ConvertTo-Json; Invoke-RestMethod -Uri "http://localhost:8085/api/submissions/{id}" -Method Put -Body $body -ContentType "application/json" | ConvertTo-Json

# 5. GET - Filter by game
Invoke-RestMethod -Uri "http://localhost:8085/api/submissions?game=Snake" -Method Get | ConvertTo-Json

# 6. GET - Filter by userId
Invoke-RestMethod -Uri "http://localhost:8085/api/submissions?userId=testuser" -Method Get | ConvertTo-Json


**Complete example with captured ID:**
```powershell
# Create submission and capture ID
$create = @{ game = "Snake"; userId = "testuser"; score = 1234 } | ConvertTo-Json; $created = Invoke-RestMethod -Uri "http://localhost:8085/api/submissions" -Method Post -Body $create -ContentType "application/json"; $id = $created.id; Write-Host "Created ID: $id"; $created | ConvertTo-Json

# Get by captured ID
Invoke-RestMethod -Uri "http://localhost:8085/api/submissions/$id" -Method Get | ConvertTo-Json

# Update by captured ID
$update = @{ score = 2000 } | ConvertTo-Json; Invoke-RestMethod -Uri "http://localhost:8085/api/submissions/$id" -Method Put -Body $update -ContentType "application/json" | ConvertTo-Json
```

## Integration (with API Gateway)
- Proxy requests by calling `/api/submissions` (POST/PUT/GET/query).
- Configure API Gateway to route those calls using `Microservices:SubmissionGateway` URL.


## Troubleshooting

### Service Issues
- If the service doesn't store data: check write permissions for the `Data/` directory.
- If you get port errors: use a different value for `ASPNETCORE_URLS`.
- If GET/POST fails: ensure service is actually running and listening ("Now listening on …" log).
- For HTTPS, you may need to trust the .NET dev certificate: (I had this issue on my own machine)
  ```powershell
  dotnet dev-certs https --trust
  ```

### PowerShell Script Execution Issues
- **"Execution policy" error**: PowerShell may block unsigned scripts. Use one of these solutions:
  ```powershell
  # Quick fix: Bypass for this script only
  powershell -ExecutionPolicy Bypass -File .\test-endpoints.ps1
  
  # Or set policy for current session only
  Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope Process
  .\test-endpoints.ps1
  ```
- **"Cannot find path" error**: Make sure you're in the correct directory:
  ```powershell
  # Check current directory
  Get-Location
  
  # Should be: ...\Team-3-BucStop_SubmissionGateway\SubmissionGateway
  # If not, navigate there:
  cd Team-3-BucStop_SubmissionGateway\SubmissionGateway
  ```
- **Script runs but no output**: Make sure the service is running first:
  ```powershell
  # Test if service is running
  Invoke-RestMethod -Uri "http://localhost:8085/api/submissions" -Method Get
  ```



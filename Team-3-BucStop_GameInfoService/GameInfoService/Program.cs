//
//	File name: Program.cs
//	  Author:	      Auto-generated
//	  Date Created:	2025-01-XX
//	Last revised:	2025-01-XX
//	  Description:	Program file for GameInfoService
//	
//
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Port is configured via ASPNETCORE_URLS environment variable (default: 8087 for GameInfoService)

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS to allow cross-origin requests
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register the game info store that reads from submissions.jsonl
builder.Services.AddSingleton<IGameInfoStore>(sp =>
{
    var env = sp.GetRequiredService<IHostEnvironment>();
    var config = sp.GetRequiredService<IConfiguration>();
    
    // Get paths from configuration or use defaults
    var dataDir = config["Storage:DataPath"];
    var uploadsDir = config["Storage:UploadsPath"];
    
    // If not configured, try to find relative paths (for local development)
    if (string.IsNullOrWhiteSpace(dataDir))
    {
        // Try the correct path structure first
        dataDir = Path.Combine(env.ContentRootPath, "../../Team-3-BucStop_SubmissionGateway/SubmissionGateway/Data");
        if (!Directory.Exists(dataDir))
        {
            // Try alternative path structure (if running from different location)
            var altPath = Path.Combine(env.ContentRootPath, "../../../Team-3-BucStop_SubmissionGateway/SubmissionGateway/Data");
            if (Directory.Exists(altPath))
            {
                dataDir = altPath;
            }
            else
            {
                // Last resort: use local directory
                dataDir = Path.Combine(env.ContentRootPath, "Data");
            }
        }
    }
    
    if (string.IsNullOrWhiteSpace(uploadsDir))
    {
        // Try the correct path structure first
        uploadsDir = Path.Combine(env.ContentRootPath, "../../Team-3-BucStop_SubmissionGateway/SubmissionGateway/Uploads");
        if (!Directory.Exists(uploadsDir))
        {
            // Try alternative path structure (if running from different location)
            var altPath = Path.Combine(env.ContentRootPath, "../../../Team-3-BucStop_SubmissionGateway/SubmissionGateway/Uploads");
            if (Directory.Exists(altPath))
            {
                uploadsDir = altPath;
            }
            else
            {
                // Last resort: use local directory
                uploadsDir = Path.Combine(env.ContentRootPath, "Uploads");
            }
        }
    }
    
    var filePath = Path.Combine(dataDir, "submissions.jsonl");
    
    // Ensure directories exist
    if (!string.IsNullOrWhiteSpace(dataDir))
        Directory.CreateDirectory(dataDir);
    if (!string.IsNullOrWhiteSpace(uploadsDir))
        Directory.CreateDirectory(uploadsDir);
    
    return new FileGameInfoStore(filePath, uploadsDir ?? string.Empty, env);
});

// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("gameinfo-fixed", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromSeconds(10);
        limiterOptions.PermitLimit = 100;
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();

app.UseRateLimiter();

app.MapControllers();

// Serve static files from uploads directory
var uploadsPath = app.Configuration["Storage:UploadsPath"];
if (!string.IsNullOrWhiteSpace(uploadsPath) && Directory.Exists(uploadsPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadsPath),
        RequestPath = "/api/files"
    });
}

app.Run();

// Represents game information returned by the API
public sealed class GameInfo
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DateAdded { get; set; } = string.Empty;
    public string HowTo { get; set; } = string.Empty;
    public string Thumbnail { get; set; } = string.Empty;
    public Stack<KeyValuePair<string, int>> LeaderBoardStack { get; set; } = new Stack<KeyValuePair<string, int>>();
}

// Represents a submission from the JSONL file
internal sealed class Submission
{
    public Guid Id { get; set; }
    public string GameName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string CodeFilePath { get; set; } = string.Empty;
    public DateTime CreatedEst { get; set; }
    public DateTime UpdatedEst { get; set; }
}

public interface IGameInfoStore
{
    IReadOnlyList<GameInfo> GetAll();
    GameInfo? GetById(int id);
    void Refresh(); // Refresh cache from file system
}

internal sealed class FileGameInfoStore : IGameInfoStore
{
    private readonly string _filePath;
    private readonly string _uploadsDirectory;
    private readonly IHostEnvironment _env;
    private readonly object _lock = new object();
    private List<GameInfo> _games = new List<GameInfo>();
    private DateTime _lastReadTime = DateTime.MinValue;
    private readonly FileSystemWatcher? _fileWatcher;

    public FileGameInfoStore(string filePath, string uploadsDirectory, IHostEnvironment env)
    {
        _filePath = filePath;
        _uploadsDirectory = uploadsDirectory;
        _env = env;
        
        // Initial load
        Refresh();
        
        // Set up file watcher to automatically refresh when files change
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (directory != null && Directory.Exists(directory))
            {
                _fileWatcher = new FileSystemWatcher(directory)
                {
                    Filter = "submissions.jsonl",
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
                };
                _fileWatcher.Changed += (sender, e) =>
                {
                    // Debounce: wait a bit before refreshing to avoid multiple rapid refreshes
                    Task.Delay(500).ContinueWith(_ => Refresh());
                };
                _fileWatcher.EnableRaisingEvents = true;
            }
        }
        catch
        {
            // File watcher setup failed, continue without it
        }
    }

    public IReadOnlyList<GameInfo> GetAll()
    {
        lock (_lock)
        {
            // Check if file has been modified
            if (File.Exists(_filePath))
            {
                var lastWriteTime = File.GetLastWriteTimeUtc(_filePath);
                if (lastWriteTime > _lastReadTime)
                {
                    Refresh();
                }
            }
            
            return _games.ToList();
        }
    }

    public GameInfo? GetById(int id)
    {
        lock (_lock)
        {
            return _games.FirstOrDefault(g => g.Id == id);
        }
    }

    public void Refresh()
    {
        lock (_lock)
        {
            var games = new List<GameInfo>();
            var submissions = new List<Submission>();

            if (File.Exists(_filePath))
            {
                try
                {
                    foreach (var line in File.ReadAllLines(_filePath))
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        try
                        {
                            var submission = System.Text.Json.JsonSerializer.Deserialize<Submission>(line);
                            if (submission != null)
                            {
                                submissions.Add(submission);
                            }
                        }
                        catch
                        {
                            // Skip invalid lines
                        }
                    }
                }
                catch
                {
                    // File read failed, use existing cache
                    return;
                }
            }

            // Convert submissions to GameInfo
            int idCounter = 1;
            foreach (var submission in submissions.OrderBy(s => s.CreatedEst))
            {
                var gameInfo = new GameInfo
                {
                    Id = idCounter++,
                    Title = submission.GameName,
                    Description = submission.Description,
                    Author = "Submitted User", // Could be extracted from submission if available
                    DateAdded = submission.CreatedEst.ToString("yyyy-MM-dd"),
                    HowTo = "Check the game files for instructions.",
                    Thumbnail = GetThumbnailUrl(submission.ImagePath),
                    Content = GetContentUrl(submission.CodeFilePath),
                    LeaderBoardStack = new Stack<KeyValuePair<string, int>>()
                };
                games.Add(gameInfo);
            }

            _games = games;
            _lastReadTime = DateTime.UtcNow;
        }
    }

    private string GetThumbnailUrl(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return "/images/default-thumbnail.png";

        // If it's already a full URL, return as-is
        if (imagePath.StartsWith("http://") || imagePath.StartsWith("https://"))
            return imagePath;

        // For relative paths, construct URL to serve from Uploads directory
        // In production, this would point to the SubmissionGateway service
        var fileName = Path.GetFileName(imagePath);
        return $"/api/files/{Uri.EscapeDataString(fileName)}";
    }

    private string GetContentUrl(string codeFilePath)
    {
        if (string.IsNullOrWhiteSpace(codeFilePath))
            return string.Empty;

        // If it's already a full URL, return as-is
        if (codeFilePath.StartsWith("http://") || codeFilePath.StartsWith("https://"))
            return codeFilePath;

        // For relative paths, construct URL to serve from Uploads directory
        var fileName = Path.GetFileName(codeFilePath);
        return $"/api/files/{Uri.EscapeDataString(fileName)}";
    }
}


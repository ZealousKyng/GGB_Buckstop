//
//	File name: Program.cs
//	  Author:	      Jericho McGowan
//	  Date Created:	2025-10-30
//	Last revised:	2025-10-30
//	  Description:	Program file for Submission Gateway
//	
//
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Port is configured via ASPNETCORE_URLS environment variable (default: 8085 for Submission Gateway)

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register a submission store for simple persistence
builder.Services.AddSingleton<ISubmissionStore>(sp =>
{
    var env = sp.GetRequiredService<IHostEnvironment>();
    var dataDir = Path.Combine(env.ContentRootPath, "Data");
    Directory.CreateDirectory(dataDir);
    var filePath = Path.Combine(dataDir, "submissions.jsonl");
    return new FileSubmissionStore(filePath);
});

// Adds a simple fixed-window rate limiter to protect the API from burst (used in most designs for AspNetCore)
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("submission-fixed", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromSeconds(10);
        limiterOptions.PermitLimit = 20;
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseRateLimiter();

app.MapControllers();

app.Run();

// Partial Program class for static helper methods
public partial class Program
{
    // Timezone for Eastern, UTC works, EST doesn't.
    public static DateTime CurrentEasternTime()
    {
        var est = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, est);
    }
}

// Represents a persisted game submission with score metadata
public sealed class Submission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Game { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int Score { get; set; }
    public DateTime CreatedEst { get; set; } = Program.CurrentEasternTime();
    public DateTime UpdatedEst { get; set; } = Program.CurrentEasternTime();
}

// Request DTO for creating a new submission
public sealed class CreateSubmissionRequest
{
    public string Game { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int Score { get; set; }
}

// Request DTO for updating an existing submission's score
public sealed class UpdateSubmissionRequest
{
    public int Score { get; set; }
}

public interface ISubmissionStore
{
    Submission Create(string game, string userId, int score);
    Submission? Get(Guid id);
    IReadOnlyList<Submission> List(string? game, string? userId, int take);
    Submission? Update(Guid id, int score);
}

public sealed class FileSubmissionStore : ISubmissionStore
{
    private readonly string _filePath;
    private readonly object _lock = new object();
    private readonly List<Submission> _submissions;

    public FileSubmissionStore(string filePath)
    {
        _filePath = filePath;
        _submissions = new List<Submission>();
        if (File.Exists(_filePath))
        {
            foreach (var line in File.ReadAllLines(_filePath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    var s = System.Text.Json.JsonSerializer.Deserialize<Submission>(line);
                    if (s != null) _submissions.Add(s);
                }
                catch { }
            }
        }
    }

    public Submission Create(string game, string userId, int score)
    {
        var easternNow = Program.CurrentEasternTime();
        var entity = new Submission
        {
            Id = Guid.NewGuid(),
            Game = game,
            UserId = userId,
            Score = score,
            CreatedEst = easternNow,
            UpdatedEst = easternNow
        };
        lock (_lock)
        {
            _submissions.Add(entity);
            AppendToFile(entity);
        }
        return entity;
    }

    public Submission? Get(Guid id)
    {
        lock (_lock)
        {
            return _submissions.FirstOrDefault(s => s.Id == id);
        }
    }

    public IReadOnlyList<Submission> List(string? game, string? userId, int take)
    {
        IEnumerable<Submission> q;
        lock (_lock)
        {
            q = _submissions.ToList();
        }
        if (!string.IsNullOrWhiteSpace(game)) q = q.Where(s => s.Game == game);
        if (!string.IsNullOrWhiteSpace(userId)) q = q.Where(s => s.UserId == userId);
        return q
            .OrderByDescending(s => s.Score)
            .ThenByDescending(s => s.UpdatedEst)
            .Take(Math.Clamp(take, 1, 500))
            .ToList();
    }

    public Submission? Update(Guid id, int score)
    {
        lock (_lock)
        {
            var entity = _submissions.FirstOrDefault(s => s.Id == id);
            if (entity == null) return null;
            entity.Score = score;
            entity.UpdatedEst = Program.CurrentEasternTime();
            RewriteFile();
            return entity;
        }
    }

    private void AppendToFile(Submission s)
    {
        var line = System.Text.Json.JsonSerializer.Serialize(s);
        File.AppendAllText(_filePath, line + Environment.NewLine);
    }

    private void RewriteFile()
    {
        var lines = _submissions.Select(s => System.Text.Json.JsonSerializer.Serialize(s));
        File.WriteAllLines(_filePath, lines);
    }
}


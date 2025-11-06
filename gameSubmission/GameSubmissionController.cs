using Microsoft.AspNetCore.Mvc;

namespace BucStopWebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameSubmissionController : ControllerBase
{
    private readonly string _gamesPath = "/app/games";

    public GameSubmissionController()
    {
        Directory.CreateDirectory(_gamesPath);
    }

    [HttpPost("submit")]
    public async Task<IActionResult> SubmitGame([FromForm] GameSubmissionRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.GameName))
            {
                return BadRequest(new { error = "Game name is required" });
            }

            var safeName = string.Join("_", request.GameName.Split(Path.GetInvalidFileNameChars()));
            var gameFolderPath = Path.Combine(_gamesPath, safeName);
            Directory.CreateDirectory(gameFolderPath);

            var uploadedFiles = new List<string>();

            if (request.Files != null && request.Files.Any())
            {
                foreach (var file in request.Files)
                {
                    if (file.Length > 0)
                    {
                        var fileName = Path.GetFileName(file.FileName);
                        var filePath = Path.Combine(gameFolderPath, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        uploadedFiles.Add(fileName);
                    }
                }
            }

            return Ok(new
            {
                message = $"Game submitted successfully: {request.GameName}",
                folder = safeName,
                files = uploadedFiles,
                path = gameFolderPath
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("list")]
    public IActionResult ListGames()
    {
        try
        {
            var games = new List<object>();
            if (Directory.Exists(_gamesPath))
            {
                var directories = Directory.GetDirectories(_gamesPath);
                foreach (var dir in directories)
                {
                    var dirInfo = new DirectoryInfo(dir);
                    var files = dirInfo.GetFiles();
                    games.Add(new
                    {
                        name = dirInfo.Name,
                        fileCount = files.Length,
                        files = files.Select(f => f.Name).ToList()
                    });
                }
            }
            return Ok(new { games });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class GameSubmissionRequest
{
    public string GameName { get; set; } = string.Empty;
    public List<IFormFile>? Files { get; set; }
}

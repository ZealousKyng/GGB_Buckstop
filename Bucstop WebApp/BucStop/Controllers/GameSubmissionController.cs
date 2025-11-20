using Microsoft.AspNetCore.Mvc;
using System.Net.Http;

namespace BucStopWebApp.Controllers;

public class GameSubmissionController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GameSubmissionController> _logger;

    public IActionResult Index()
    {
        return View();
    }

    public GameSubmissionController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<GameSubmissionController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("api/GameSubmission/submit")]
    public async Task<IActionResult> SubmitGame([FromForm] GameSubmissionRequest request)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.GameName))
            {
                return BadRequest(new { error = "Game name is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                return BadRequest(new { error = "Description is required" });
            }

            if (request.ImageFile == null || request.ImageFile.Length == 0)
            {
                return BadRequest(new { error = "Game image is required" });
            }

            if (request.CodeFile == null || request.CodeFile.Length == 0)
            {
                return BadRequest(new { error = "Game code file is required" });
            }

            // Get SubmissionGateway URL from configuration
            var submissionGatewayUrl = _configuration["SubmissionGateway"] ?? "http://submission-gateway";
            if (string.IsNullOrEmpty(submissionGatewayUrl))
            {
                _logger.LogError("SubmissionGateway URL not configured");
                return StatusCode(500, new { error = "SubmissionGateway URL not configured" });
            }

            // Create multipart form data to forward to SubmissionGateway
            using var httpClient = _httpClientFactory.CreateClient();
            using var multipartContent = new MultipartFormDataContent();
            
            multipartContent.Add(new StringContent(request.GameName.Trim()), "gameName");
            multipartContent.Add(new StringContent(request.Description.Trim()), "description");
            
            var imageStream = request.ImageFile.OpenReadStream();
            var imageContent = new StreamContent(imageStream);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(request.ImageFile.ContentType);
            multipartContent.Add(imageContent, "image", request.ImageFile.FileName);
            
            var codeStream = request.CodeFile.OpenReadStream();
            var codeContent = new StreamContent(codeStream);
            codeContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(request.CodeFile.ContentType);
            multipartContent.Add(codeContent, "code", request.CodeFile.FileName);

            // Forward request to SubmissionGateway
            var response = await httpClient.PostAsync($"{submissionGatewayUrl}/api/submissions", multipartContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var submissionData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseContent);
                _logger.LogInformation("Game submission successful: {GameName}, ID: {Id}", request.GameName, submissionData.GetProperty("id").GetString());
                
                return Ok(new
                {
                    id = submissionData.GetProperty("id").GetString(),
                    gameName = submissionData.GetProperty("gameName").GetString(),
                    message = $"Game submitted successfully: {request.GameName}"
                });
            }
            else
            {
                _logger.LogError("SubmissionGateway returned error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                var errorData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseContent);
                var errorMessage = errorData.TryGetProperty("error", out var error) ? error.GetString() : "Failed to submit game";
                return StatusCode((int)response.StatusCode, new { error = errorMessage });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting game: {Message}", ex.Message);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class GameSubmissionRequest
{
    public string GameName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    [FromForm(Name = "image")]
    public IFormFile? ImageFile { get; set; }
    
    [FromForm(Name = "code")]
    public IFormFile? CodeFile { get; set; }
}

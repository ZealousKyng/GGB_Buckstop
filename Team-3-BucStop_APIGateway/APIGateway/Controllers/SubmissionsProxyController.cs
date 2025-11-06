//
//	File name: SubmissionsProxyController.cs
//	  Author:	      Jericho McGowan
//	  Date Created:	2025-10-30
//	Last revised:	2025-10-30
//	  Description:	Submissions proxy controller for API Gateway
//	
//
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //forwards submission-related HTTP requests from the API Gateway to the Submission Gateway
    public class SubmissionsProxyController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        // Uses HttpClientFactory for HTTP calls and IConfiguration to resolve Submission Gateway base URLs from appsettings.
        public SubmissionsProxyController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpPost]
        // Forwards a create-submission request body to Submission Gateway (POST /api/submissions)
        public async Task<IActionResult> Post()
        {
            var client = _httpClientFactory.CreateClient();
            var baseUrl = _configuration["Microservices:SubmissionGateway"] ?? _configuration["PublicUrls:SubmissionGateway"];
            if (string.IsNullOrEmpty(baseUrl)) //if the base URL is not configured, return a 500 error (recommended error code from Gemini 2.0)
            {
                return StatusCode(500, new { error = "SubmissionGateway URL not configured" });
            }

            // Read raw body and forward with original content-type
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            var contentType = Request.ContentType ?? "application/json";
            using var content = new StringContent(body, System.Text.Encoding.UTF8, contentType);

            var upstream = await client.PostAsync($"{baseUrl}/api/submissions", content);
            var responseBody = await upstream.Content.ReadAsStringAsync();

            //Same code is used for all other tasks. Mirror upstream status code and content back to caller
            return new ContentResult
            {
                StatusCode = (int)upstream.StatusCode,
                ContentType = upstream.Content.Headers.ContentType?.ToString() ?? "application/json",
                Content = responseBody
            };
        }

        [HttpGet("{id:guid}")]
        // Retrieves a single submission by id via Submission Gateway (GET /api/submissions/{id})
        public async Task<IActionResult> GetById(Guid id)
        {
            var client = _httpClientFactory.CreateClient();
            var baseUrl = _configuration["Microservices:SubmissionGateway"] ?? _configuration["PublicUrls:SubmissionGateway"];
            if (string.IsNullOrEmpty(baseUrl)) //if the base URL is not configured, return a 500 error (recommended error code from Gemini 2.0)
            {
                return StatusCode(500, new { error = "SubmissionGateway URL not configured" });
            }
            var upstream = await client.GetAsync($"{baseUrl}/api/submissions/{id}");
            var responseBody = await upstream.Content.ReadAsStringAsync();
            //Same code as above
            return new ContentResult
            {
                StatusCode = (int)upstream.StatusCode,
                ContentType = upstream.Content.Headers.ContentType?.ToString() ?? "application/json",
                Content = responseBody
            };
        }

        [HttpGet]
        // Lists submissions and forwards the original query 
        public async Task<IActionResult> List()
        {
            var client = _httpClientFactory.CreateClient();
            var baseUrl = _configuration["Microservices:SubmissionGateway"] ?? _configuration["PublicUrls:SubmissionGateway"];
            if (string.IsNullOrEmpty(baseUrl))
            {
                return StatusCode(500, new { error = "SubmissionGateway URL not configured" });
            }
            var query = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
            var upstream = await client.GetAsync($"{baseUrl}/api/submissions{query}");
            var responseBody = await upstream.Content.ReadAsStringAsync();
            //Same code as above
            return new ContentResult
            {
                StatusCode = (int)upstream.StatusCode,
                ContentType = upstream.Content.Headers.ContentType?.ToString() ?? "application/json",
                Content = responseBody
            };
        }

        [HttpPut("{id:guid}")]
        // Forwards an update-submission request body by id (PUT /api/submissions/{id})
        public async Task<IActionResult> Put(Guid id)
        {
            var client = _httpClientFactory.CreateClient();
            var baseUrl = _configuration["Microservices:SubmissionGateway"] ?? _configuration["PublicUrls:SubmissionGateway"];
            if (string.IsNullOrEmpty(baseUrl))
            {
                return StatusCode(500, new { error = "SubmissionGateway URL not configured" });
            }

            // Read raw body and forward with original content-type
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            var contentType = Request.ContentType ?? "application/json";
            using var content = new StringContent(body, System.Text.Encoding.UTF8, contentType);

            var upstream = await client.PutAsync($"{baseUrl}/api/submissions/{id}", content);
            var responseBody = await upstream.Content.ReadAsStringAsync();
           //Same code as above
            return new ContentResult
            {
                StatusCode = (int)upstream.StatusCode,
                ContentType = upstream.Content.Headers.ContentType?.ToString() ?? "application/json",
                Content = responseBody
            };
        }
    }
}



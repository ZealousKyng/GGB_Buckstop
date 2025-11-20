//
//	File name: SubmissionsController.cs
//	  Author:	      Jericho McGowan
//	  Date Created:	2025-10-30
//	Last revised:	2025-10-30
//	  Description:	Submissions controller for Submission Gateway
//	
//
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace SubmissionGateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // Handles CRUD-style HTTP endpoints for game submissions
    public class SubmissionsController : ControllerBase
    {
        private readonly ISubmissionStore _store;
        private readonly string _uploadsDirectory;
        private readonly IHostEnvironment _env;

        // Injects the file-backed submission store used to persist submissions
        public SubmissionsController(ISubmissionStore store, IHostEnvironment env)
        {
            _store = store;
            _env = env;
            _uploadsDirectory = Path.Combine(env.ContentRootPath, "Uploads");
            Directory.CreateDirectory(_uploadsDirectory);
        }

        [HttpPost]
        [EnableRateLimiting("submission-fixed")]
        [DisableRequestSizeLimit]
        // Creates a new submission after validating the incoming request and handling file uploads
        public async Task<IActionResult> Create()
        {
            // Check if request is multipart/form-data
            if (!Request.HasFormContentType)
            {
                return BadRequest(new { error = "Request must be multipart/form-data" });
            }

            var form = await Request.ReadFormAsync();
            
            // Extract form fields
            var gameName = form["gameName"].ToString().Trim();
            var description = form["description"].ToString().Trim();
            var imageFile = form.Files["image"];
            var codeFile = form.Files["code"];

            // Basic request validation to prevent bad/malicious input
            if (string.IsNullOrWhiteSpace(gameName) || gameName.Length > 64)
            {
                return BadRequest(new { error = "Invalid game name. Must be 1-64 characters." });
            }

            if (string.IsNullOrWhiteSpace(description) || description.Length > 1000)
            {
                return BadRequest(new { error = "Invalid description. Must be 1-1000 characters." });
            }

            if (imageFile == null || imageFile.Length == 0)
            {
                return BadRequest(new { error = "Image file is required" });
            }

            if (codeFile == null || codeFile.Length == 0)
            {
                return BadRequest(new { error = "Code file is required" });
            }

            // Validate image file type
            var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var imageExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (!allowedImageExtensions.Contains(imageExtension))
            {
                return BadRequest(new { error = "Invalid image file type. Allowed: jpg, jpeg, png, gif, webp" });
            }

            // Validate file sizes (10MB for image, 50MB for code)
            const long maxImageSize = 10 * 1024 * 1024; // 10MB
            const long maxCodeSize = 50 * 1024 * 1024; // 50MB

            if (imageFile.Length > maxImageSize)
            {
                return BadRequest(new { error = "Image file size exceeds 10MB limit" });
            }

            if (codeFile.Length > maxCodeSize)
            {
                return BadRequest(new { error = "Code file size exceeds 50MB limit" });
            }

            try
            {
                // Generate unique file names
                var submissionId = Guid.NewGuid();
                var imageFileName = $"{submissionId}_{Path.GetFileName(imageFile.FileName)}";
                var codeFileName = $"{submissionId}_{Path.GetFileName(codeFile.FileName)}";

                // Save image file
                var imagePath = Path.Combine(_uploadsDirectory, imageFileName);
                using (var stream = new FileStream(imagePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Save code file
                var codePath = Path.Combine(_uploadsDirectory, codeFileName);
                using (var stream = new FileStream(codePath, FileMode.Create))
                {
                    await codeFile.CopyToAsync(stream);
                }

                // Store relative paths
                var relativeImagePath = $"Uploads/{imageFileName}";
                var relativeCodePath = $"Uploads/{codeFileName}";

                // Persist via the store
                var entity = _store.Create(gameName, description, relativeImagePath, relativeCodePath);
                return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error saving files: {ex.Message}" });
            }
        }

        [HttpPut("{id:guid}")]
        [EnableRateLimiting("submission-fixed")]
        // Updates an existing submission's game name and description
        public async Task<IActionResult> Update(Guid id)
        {
            if (!Request.HasFormContentType)
            {
                return BadRequest(new { error = "Request must be multipart/form-data or application/json" });
            }

            var form = await Request.ReadFormAsync();
            var gameName = form["gameName"].ToString().Trim();
            var description = form["description"].ToString().Trim();

            // Validate
            if (string.IsNullOrWhiteSpace(gameName) || gameName.Length > 64)
            {
                return BadRequest(new { error = "Invalid game name. Must be 1-64 characters." });
            }

            if (string.IsNullOrWhiteSpace(description) || description.Length > 1000)
            {
                return BadRequest(new { error = "Invalid description. Must be 1-1000 characters." });
            }

            // Lookup and update entity
            var entity = _store.Update(id, gameName, description);
            if (entity == null)
            {
                return NotFound(new { error = "Submission not found" });
            }
            return Ok(entity);
        }

        [HttpGet("{id:guid}")]
        // Retrieves a single submission by its unique identifier
        public async Task<IActionResult> GetById(Guid id)
        {
            var entity = _store.Get(id);
            if (entity == null)
            {
                return NotFound(new { error = "Submission not found" });
            }
            return Ok(entity);
        }

        [HttpGet]
        // Returns a filtered and limited list of submissions, ordered by creation date
        public async Task<IActionResult> List([FromQuery] string? gameName, [FromQuery] int? take = 50)
        {
            var items = _store.List(gameName, take ?? 50);
            return Ok(items);
        }
    }
}



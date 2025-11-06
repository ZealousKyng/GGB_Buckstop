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

        // Injects the file-backed submission store used to persist submissions
        public SubmissionsController(ISubmissionStore store)
        {
            _store = store;
        }

        [HttpPost]
        [EnableRateLimiting("submission-fixed")]
        // Creates a new submission after validating the incoming request
        public async Task<IActionResult> Create([FromBody] CreateSubmissionRequest request)
        {
            // Basic request validation to prevent bad/malicious input - AI Generated using Gemini 2.0
            if (request == null)
            {
                return BadRequest(new { error = "Request body is required" });
            }
            if (string.IsNullOrWhiteSpace(request.Game) || request.Game.Length > 64)
            {
                return BadRequest(new { error = "Invalid game" });
            }
            if (string.IsNullOrWhiteSpace(request.UserId) || request.UserId.Length > 128)
            {
                return BadRequest(new { error = "Invalid userId" });
            }
            if (request.Score < 0 || request.Score > 1_000_000_000)
            {
                return BadRequest(new { error = "Invalid score" });
            }

            // Persist via the store variable
            var entity = _store.Create(request.Game.Trim(), request.UserId.Trim(), request.Score);
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
        }

        [HttpPut("{id:guid}")]
        [EnableRateLimiting("submission-fixed")]
        // Updates the score for an existing submission identified by id
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSubmissionRequest request)
        {
            // Validate request body and score bounds
            if (request == null)
            {
                return BadRequest(new { error = "Request body is required" });
            }
            if (request.Score < 0 || request.Score > 1_000_000_000)
            {
                return BadRequest(new { error = "Invalid score" });
            }
            // Lookup and update entity
            var entity = _store.Update(id, request.Score);
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
        // Returns a filtered and limited list of submissions, ordered by score then last update
        public async Task<IActionResult> List([FromQuery] string? game, [FromQuery] string? userId, [FromQuery] int? take = 50)
        {
            var items = _store.List(game, userId, take ?? 50);
            return Ok(items);
        }
    }
}



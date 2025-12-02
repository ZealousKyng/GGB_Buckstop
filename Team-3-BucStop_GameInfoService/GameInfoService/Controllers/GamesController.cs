//
//	File name: GamesController.cs
//	  Author:	      Auto-generated
//	  Date Created:	2025-01-XX
//	Last revised:	2025-01-XX
//	  Description:	Games controller for GameInfoService
//	
//
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameInfoService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GamesController : ControllerBase
    {
        private readonly IGameInfoStore _store;
        private readonly ILogger<GamesController> _logger;

        public GamesController(IGameInfoStore store, ILogger<GamesController> logger)
        {
            _store = store;
            _logger = logger;
        }

        /// <summary>
        /// Returns a list of all games (metadata)
        /// </summary>
        [HttpGet]
        [EnableRateLimiting("gameinfo-fixed")]
        public IActionResult GetAll()
        {
            try
            {
                var games = _store.GetAll();
                _logger.LogInformation("Retrieved {Count} games", games.Count);
                return Ok(games);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving games list");
                return StatusCode(500, new { error = "Failed to retrieve games" });
            }
        }

        /// <summary>
        /// Returns details for a specific game by ID
        /// </summary>
        [HttpGet("{id:int}")]
        [EnableRateLimiting("gameinfo-fixed")]
        public IActionResult GetById(int id)
        {
            try
            {
                var game = _store.GetById(id);
                if (game == null)
                {
                    _logger.LogWarning("Game with ID {Id} not found", id);
                    return NotFound(new { error = $"Game with ID {id} not found" });
                }

                _logger.LogInformation("Retrieved game with ID {Id}", id);
                return Ok(game);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving game with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve game" });
            }
        }
    }
}


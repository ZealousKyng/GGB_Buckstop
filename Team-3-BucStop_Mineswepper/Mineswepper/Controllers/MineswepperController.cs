using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Mineswepper
{
    [ApiController]
    [Route("[controller]")]
    public class MineswepperController : ControllerBase
    {
        private readonly ILogger<MineswepperController> _logger;
        private readonly IConfiguration _config;
        private static string gameURL;
        private static string imgURL;

        public MineswepperController(ILogger<MineswepperController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
            gameURL = _config["MicroserviceUrls:Mineswepper"];
            imgURL = _config["MicroserviceUrls:Image"];
        }

        private static readonly List<GameInfo> TheInfo = new List<GameInfo>
        {
            new GameInfo {
                Id = 4,
                Title = "Mineswepper",
                Content = gameURL,
                Author = "Spring 2025 Semester",
                DateAdded = "",
                Description = "Mineswepper is a classic logic puzzle game where players must uncover cells while avoiding hidden mines. Use logic to identify the location of mines based on number clues in adjacent cells.",
                HowTo = "Left click to reveal a cell. Right click to place a flag on suspected mines.",
                Thumbnail = imgURL
            }
        };

        [HttpGet]
        public IEnumerable<GameInfo> Get()
        {
            // Confirm the Content and Thumbnail URLs are assigned if they were not available when initialized
            if (TheInfo[0].Content == null)
            {
                TheInfo[0].Content = gameURL;
            }

            if (TheInfo[0].Thumbnail == null)
            {
                TheInfo[0].Thumbnail = imgURL;
            }
            
            _logger.LogInformation("Mineswepper service: Returning game info");
            return TheInfo;
        }
    }
} 
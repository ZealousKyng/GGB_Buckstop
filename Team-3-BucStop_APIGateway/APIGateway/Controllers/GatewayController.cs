using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using Gateway;

namespace Gateway
{
    [ApiController]
    [Route("[controller]")]
    public class GatewayController : ControllerBase
    {
        private readonly ILogger<GatewayController> _logger;
        private readonly IConfiguration _config;
        private readonly List<GameInfo> _gameInfos;
        private readonly IHostEnvironment _host;

        public GatewayController(ILogger<GatewayController> logger, IConfiguration config, IHostEnvironment host)
        {
            _logger = logger;
            _config = config;
            _host = host;
            _gameInfos = new List<GameInfo>();
        }

        [HttpGet]
        public async Task<IEnumerable<GameInfo>> Get()
        {
            try
            {
                var gameKeys = new[] { "Snake", "Tetris", "Pong", "BucKart"};

                var fetchTasks = new List<Task>();
                foreach (var game in gameKeys)
                {
                    string internalUrl;

                    if (_host.IsDevelopment())
                    {
                        internalUrl = _config[$"PublicUrls:{game}"];
                        _logger.LogInformation($"Using Development URL for {game}: {internalUrl}");
                    }
                    else
                    {
                        internalUrl = _config[$"Microservices:{game}"];
                        _logger.LogInformation($"Using Container URL for {game}: {internalUrl}");
                    }

                    var publicUrl = _config[$"PublicUrls:{game}"];
                    var jsPath = $"/js/{game.ToLowerInvariant()}.js";

                    fetchTasks.Add(FetchGameInfo(internalUrl, $"/{game}", publicUrl + jsPath));
                }

                await Task.WhenAll(fetchTasks);
                return _gameInfos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while gathering game info");
                return GenerateFailureResponse();
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        private async Task FetchGameInfo(string internalUrl, string endpoint, string publicJsUrl)
        {
            try
            {
                using var client = new HttpClient { BaseAddress = new Uri(internalUrl) };
                var response = await client.GetAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    var gameInfoList = await response.Content.ReadFromJsonAsync<List<GameInfo>>();
                    if (gameInfoList != null)
                    {
                        foreach (var game in gameInfoList)
                        {
                            // Set the Content URL for the game
                            game.Content = publicJsUrl;

                            // Null check for required fields and set the Status accordingly
                            if (string.IsNullOrEmpty(game.Description) ||
                             string.IsNullOrEmpty(game.HowTo) ||
                              string.IsNullOrEmpty(game.Content))
                              {
                                game.Status = "under_construction"; // Mark the game as under construction if fields are missing
                              }
                              else
                              {
                                game.Status = "complete"; // Mark the game as complete if all required fields are present
                              }
                        }

                        lock (_gameInfos)
                        {
                            _gameInfos.AddRange(gameInfoList);
                        }
                    }
                }
                else
                {
                    _logger.LogError("Failed to get game info from {Url}{Endpoint} - Status: {Status}", internalUrl, endpoint, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in FetchGameInfo for {Url}{Endpoint}", internalUrl, endpoint);
            }
        }

        private IEnumerable<GameInfo> GenerateFailureResponse()
        {
            return new List<GameInfo>
            {
                new GameInfo
                {
                    Title = "Unavailable",
                    Author = "Unavailable",
                    Description = "Failed to retrieve data",
                    HowTo = "N/A",
                    LeaderBoardStack = new Stack<KeyValuePair<string, int>>()
                }
            };
        }
    }
}
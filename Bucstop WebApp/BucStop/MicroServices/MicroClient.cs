using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Linq;
using BucStop.Models;
using BucStop.Controllers;
using BucStop.Services;


namespace BucStop
{
    // DTO for GameInfoService responses (uses LeaderBoardStack instead of LeaderBoard)
    internal class GameInfoServiceResponse
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

    public class MicroClient
    {
        private readonly JsonSerializerOptions options = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly HttpClient client;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MicroClient> _logger;
        private readonly IConfiguration _configuration;
        private List<Game> gamesList;
        private Task<List<Game>> gamesTask;
        private readonly object _gamesLock = new object();

        public MicroClient(HttpClient client, IHttpClientFactory httpClientFactory, ILogger<MicroClient> logger, IConfiguration configuration)
        {
            this.client = client;
            this._httpClientFactory = httpClientFactory;
            this._logger = logger;
            this._configuration = configuration;

            //Start Asynchronous pull of Games
            gamesTask = GetGamesWithInfo();
        }

        /// <summary>
        /// Requests the Gateway for a List of Game Information 
        /// </summary>
        /// <returns></returns>
        public async Task<GameInfo[]> GetGamesAsync()
        {
            try
            {
                var responseMessage = await this.client.GetAsync("/Gateway");

                if (responseMessage != null)
                {
                    var stream = await responseMessage.Content.ReadAsStreamAsync();
                    return await JsonSerializer.DeserializeAsync<GameInfo[]>(stream, options);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("{Category}: API request failed: {ErrorMessage}", "APIRequests", ex.Message);
            }
            return new GameInfo[] { };
        }

        /// <summary>
        /// Fetches games from GameInfoService (submitted games)
        /// </summary>
        private async Task<GameInfo[]> GetGamesFromGameInfoServiceAsync()
        {
            try
            {
                var gameInfoServiceUrl = _configuration.GetValue<string>("GameInfoService");
                if (string.IsNullOrWhiteSpace(gameInfoServiceUrl))
                {
                    _logger.LogWarning("GameInfoService URL not configured, skipping submitted games.");
                    return new GameInfo[] { };
                }

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.BaseAddress = new Uri(gameInfoServiceUrl);
                var responseMessage = await httpClient.GetAsync("/api/games");

                if (responseMessage != null && responseMessage.IsSuccessStatusCode)
                {
                    var stream = await responseMessage.Content.ReadAsStreamAsync();
                    var serviceGames = await JsonSerializer.DeserializeAsync<GameInfoServiceResponse[]>(stream, options);
                    
                    if (serviceGames != null && serviceGames.Length > 0)
                    {
                        // Convert GameInfoServiceResponse to GameInfo
                        var games = serviceGames.Select(g => new GameInfo
                        {
                            Id = g.Id,
                            Title = g.Title,
                            Author = g.Author,
                            Content = g.Content,
                            Description = g.Description,
                            DateAdded = g.DateAdded,
                            HowTo = g.HowTo,
                            Thumbnail = g.Thumbnail,
                            LeaderBoard = g.LeaderBoardStack // Map LeaderBoardStack to LeaderBoard
                        }).ToArray();
                        
                        _logger.LogInformation("Successfully retrieved {Count} games from GameInfoService.", games.Length);
                        return games;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning("{Category}: Failed to fetch from GameInfoService: {ErrorMessage}", "APIRequests", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error fetching games from GameInfoService.");
            }
            return new GameInfo[] { };
        }

        // Converts the GameInfo objects gathered from GetGamesAsync() into Game objects to pass to controllers.
        public async Task<List<Game>> GetGamesWithInfo()
        {
            List<Game> games = new List<Game>();

            try
            {
                // Fetch games from API Gateway (existing games)
                GameInfo[] gatewayGames = await GetGamesAsync();
                
                // Fetch games from GameInfoService (submitted games)
                GameInfo[] submittedGames = await GetGamesFromGameInfoServiceAsync();

                if (gatewayGames.Length > 0 || submittedGames.Length > 0)
                {
                    _logger.LogInformation("Successfully retrieved {GatewayCount} games from Gateway and {SubmittedCount} games from GameInfoService. Total: {TotalCount}", 
                        gatewayGames.Length, submittedGames.Length, gatewayGames.Length + submittedGames.Length);
                }
                else
                {
                    _logger.LogWarning("No games retrieved from any source.");
                }

                // Get GameInfoService base URL for resolving relative paths
                var gameInfoServiceUrl = _configuration.GetValue<string>("GameInfoService") ?? "";

                // Find the maximum ID from gateway games to offset submitted games
                int maxGatewayId = gatewayGames.Length > 0 ? gatewayGames.Max(g => g?.Id ?? 0) : 0;
                int submittedGameOffset = maxGatewayId;

                // Process gateway games first
                foreach (GameInfo info in gatewayGames)
                {
                    if (info == null || !GameFeatureManager.IsEnabled(info.Title))
                        continue; // Skip disabled games

                    Game game = ConvertGameInfoToGame(info, gameInfoServiceUrl, false);
                    games.Add(game);
                }

                // Process submitted games with offset IDs
                foreach (GameInfo info in submittedGames)
                {
                    if (info == null || !GameFeatureManager.IsEnabled(info.Title))
                        continue; // Skip disabled games

                    Game game = ConvertGameInfoToGame(info, gameInfoServiceUrl, true);
                    // Offset ID to avoid conflicts
                    game.Id += submittedGameOffset;
                    games.Add(game);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving game information from API.");
            }

            return games;
        }

        /// <summary>
        /// Converts a GameInfo object to a Game object
        /// </summary>
        private Game ConvertGameInfoToGame(GameInfo info, string gameInfoServiceUrl, bool isSubmittedGame)
        {
            Game game = new Game();

            game.Id = info.Id;
            game.Title = info.Title;
            
            // Handle Content URL - if it's a relative path from GameInfoService, convert to full URL
            if (!string.IsNullOrWhiteSpace(info.Content))
            {
                if (info.Content.StartsWith("/api/files/") && !string.IsNullOrWhiteSpace(gameInfoServiceUrl))
                {
                    // Convert relative GameInfoService path to full URL
                    game.Content = $"{gameInfoServiceUrl.TrimEnd('/')}{info.Content}";
                }
                else
                {
                    game.Content = info.Content;
                }
            }
            
            // Handle Thumbnail URL - if it's a relative path from GameInfoService, convert to full URL
            if (!string.IsNullOrWhiteSpace(info.Thumbnail))
            {
                if (info.Thumbnail.StartsWith("/api/files/") && !string.IsNullOrWhiteSpace(gameInfoServiceUrl))
                {
                    // Convert relative GameInfoService path to full URL
                    game.Thumbnail = $"{gameInfoServiceUrl.TrimEnd('/')}{info.Thumbnail}";
                }
                else
                {
                    game.Thumbnail = info.Thumbnail;
                }
            }
            
            game.Author = info.Author ?? "Submitted User";
            game.HowTo = info.HowTo ?? "Check the game files for instructions.";
            game.DateAdded = info.DateAdded ?? "";
            game.Description = string.IsNullOrWhiteSpace(info.Description) 
                ? info.DateAdded ?? "" 
                : $"{info.Description} \n {info.DateAdded}";
            game.LeaderBoard = info.LeaderBoard ?? new Stack<KeyValuePair<string, int>>();

            _logger.LogInformation("Game ID {Id} Title: {Title}, Content URL: {Content}", game.Id, info.Title, game.Content);

            return game;
        }

        // Return the private gamesList object.
        // Refreshes the games list each time to ensure new submissions appear
        public List<Game> GetGamesList()
        {
            lock (_gamesLock)
            {
                // Refresh games list on each call to ensure new submissions appear
                gamesTask = GetGamesWithInfo();
                gamesList = gamesTask.Result;
                return this.gamesList;
            }
        }

        /*
         * Generic method to send data back to the microservice using the HttpClient Class 
         * baseUrl - This parameter is the base URL of the microservice. For example "http://microservice.url"
         * endpoint - This parameter represents the specific endpoint or route within your microservice API that you want to send the data to. Ex. 'POST /update/data' 
         * Made with ChatGPT
         */
        public async Task<bool> SendDataAsync<T>(string baseUrl, string endpoint, T data)
        {
            try
            {
                //Set the base address of the microservice 
                client.BaseAddress = new Uri(baseUrl);

                //Serialize the data 
                string jsonData = JsonSerializer.Serialize(data);

                //Convert serialized data to bytes 
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                //Send data using POST request
                var response = await client.PostAsync(endpoint, content);

                //Return status code (True if HTTP status code in range 200-299, false otherwise) 
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex) //Log error and return false if any exception occurs
            {
                _logger.LogError(ex.Message);
                return false; 
            }
        }
    }
}
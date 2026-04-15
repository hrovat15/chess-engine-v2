using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace chess_engine_v2
{
    internal class LichessListener
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions;

        public LichessListener(string token)
        {
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        // Inside MyChessBot.App/LichessClient.cs
        public async Task ListenForEvents()
        {
            // ... inside your stream reading loop ...
            while (true) // Auto-reconnect loop
            {
                try
                {
                    Console.WriteLine("Connecting to Lichess Event Stream...");
                    using var response = await _http.GetAsync("https://lichess.org/api/stream/event", HttpCompletionOption.ResponseHeadersRead);
                    Console.WriteLine(response);
                    using var stream = await response.Content.ReadAsStreamAsync();
                    using var reader = new StreamReader(stream);

                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (string.IsNullOrWhiteSpace(line)) continue; // Lichess sends empty lines as keep-alive

                        var evt = JsonSerializer.Deserialize<LichessEvent>(line, _jsonOptions);
                        if (evt?.Type == "gameStart" && evt.Game != null)
                        {
                            Console.WriteLine($"Game Started: {evt.Game.GameId}");
                            // FIRE AND FORGET: Start the game handler in a new task (thread)

                            var gameHandler = new GameStreamHandler(evt.Game.GameId, _http, evt.Game.color);
                            _ = Task.Run(() => gameHandler.RunAsync());
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Stream disconnected: {ex.Message}. Reconnecting in 5s...");
                    await Task.Delay(5000);
                }
            }
        }

        public class LichessEvent
        {
            public string Type { get; set; }
            public GameInfo Game { get; set; }

            public class GameInfo
            {
                public string GameId { get; set; }
                public string color { get; set; }
            }
        }
    }
}

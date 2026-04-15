using chessEngine.App;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace chess_engine_v2
{
    internal class GameStreamHandler
    {
        private readonly string _gameId;
        private readonly HttpClient _http;
        private readonly Search _engineSearch; // Instance of your engine logic
        private Board _board; // Current board state for this game
        private bool _myTurn; // Track if it's our turn
        private int counter = 0;

        public GameStreamHandler(string gameId, HttpClient client, string color)
        {
            _gameId = gameId;
            _http = client;
            _board = new Board(); // Each game gets its own board state
            _engineSearch = new Search(); // Each game gets its own search state
            _myTurn = color == "white"; // Determine if it's our turn based on color
        }

        public async Task RunAsync()
        {
            // 1. Connect to /api/bot/game/stream/{_gameId}
            using var response = await _http.GetAsync($"https://lichess.org/api/bot/game/stream/{_gameId}", HttpCompletionOption.ResponseHeadersRead);
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            if (_myTurn)
            {
                // If we start as white, we need to make the first move immediately
                await MakeMove();
            }
            else counter++;

            while (!reader.EndOfStream)
            {
                counter++;
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line) || line.Contains("id"))
                {
                    counter--;
                    continue;
                }
                if (!_myTurn && counter%2 == 0)
                {
                    var opponentMove = ParseOpponentMove(line);
                    _board.MakeMove(opponentMove);
                    Console.WriteLine(_board.WPieces);
                    await MakeMove();
                }
            }
        }

        Move ParseOpponentMove(string jsonLine)
        {
            var jsonDoc = JsonDocument.Parse(jsonLine);
            var moveStr = jsonDoc.RootElement.GetProperty("moves").GetString();
            int fromFile = moveStr[moveStr.Length - 4] - 'a';
            int fromRank = moveStr[moveStr.Length - 3] - '1';
            int toFile = moveStr[moveStr.Length - 2] - 'a';
            int toRank = moveStr[moveStr.Length - 1] - '1';
            Move move = new Move(from: fromRank * 8 + fromFile, to: toRank * 8 + toFile, 0);
            ushort flags = 0;
            if ((1UL << move.To & _board.AllPieces) != 0) flags = 4;
            return new Move(from: fromRank * 8 + fromFile, to: toRank * 8 + toFile, flags);
        }

        private async Task MakeMove()
        {
            var bestMove = _engineSearch.GetBestMove(_board, depth: 5); // Adjust depth as needed
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://lichess.org/api/bot/game/{_gameId}/move/{ToUCI(bestMove)}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Program.token);
            request.Content = new StringContent(""); // Lichess API requires a body, even if it's empty
            await _http.SendAsync(request);
            Console.WriteLine("made move " + ToUCI(bestMove));
            _board.MakeMove(bestMove);

            string ToUCI(Move move)
            {
                char fromFile = (char)('a' + (move.From % 8));
                char fromRank = (char)('1' + (move.From / 8));
                char toFile = (char)('a' + (move.To % 8));
                char toRank = (char)('1' + (move.To / 8));
                string promotion = move.IsPromotion ? "q" : "";
                return $"{fromFile}{fromRank}{toFile}{toRank}{promotion}";
            }

            _myTurn = false; // After making a move, it's no longer our turn until we receive the opponent's move
        }
    }
}

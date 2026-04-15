using chess_engine_v2;
using System;
using System.Collections.Generic;
using System.Text;

namespace chessEngine.App
{
    internal class Program
    {
        static public string token = File.ReadAllText("C:\\Users\\Luka\\Documents\\api tokens\\Lichess token.txt").Trim(); // Read token from file for security

        static void Main(string[] args)
        {
            LichessListener client = new LichessListener(token);
            client.ListenForEvents().Wait();
        }
    }
}

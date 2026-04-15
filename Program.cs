using chess_engine_v2;
using System;
using System.Collections.Generic;
using System.Text;

namespace chessEngine.App
{
    internal class Program
    {
        static void Main(string[] args)
        {
            LichessListener client = new LichessListener("lip_RGGGCfGeGmSe0I0pRYrF");
            client.ListenForEvents().Wait();
        }
    }
}

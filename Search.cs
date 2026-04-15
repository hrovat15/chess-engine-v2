using chess_engine_v2;
using System;
using System.Diagnostics;
using System.Numerics;

public class Search
{
    private const int Infinity = 30000;
    private const int maxDepth = 8; // You can adjust this based on performance needs
    private readonly Evaluation _eval = new Evaluation();
    private readonly MovesGenerator _gen = new MovesGenerator();

    public Move GetBestMove(Board board, int depth)
    {
        int bestScore = -Infinity;
        Move bestMove = default;
        Stopwatch stopwatch = new Stopwatch();

        var moves = _gen.GenerateMoves(board);
        for (int i = 0; i < moves.Length; i++)
        {
            var move = moves[i];
            if (move.Value == 0) break;

                board.MakeMove(move);
                int score = -AlphaBeta(board, depth - 1, -Infinity, Infinity);
                board.UnmakeMove(move);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
                if(stopwatch.ElapsedMilliseconds > 5000) // Time limit of 5 seconds for move calculation
                {
                    break;
                }
        }

        // Guard: if no legal moves found, don't send a garbage move
        if (bestMove.Value == 0)
        {
            Console.WriteLine("No legal moves found (checkmate or stalemate).");
        }

        return bestMove;
    }

    private int AlphaBeta(Board board, int depth, int alpha, int beta)
   {
       if (depth == 0)
           return _eval.Evaluate(board);

       var moves = _gen.GenerateMoves(board);

        // If no moves (first slot is sentinel), check for checkmate or stalemate
        if (moves.Length == 0 || moves[0].Value == 0)
        {
            return board.IsInCheck() ? -Infinity + (maxDepth - depth) : 0;
        }

       for (int i = 0; i < moves.Length; i++)
       {
            var move = moves[i];
            // sentinel: stop iterating when we hit a default/zero move
            if (move.Value == 0) break;

            board.MakeMove(move);
            int score = -AlphaBeta(board, depth - 1, -beta, -alpha);
            board.UnmakeMove(move);

            // Pruning: If the score is too good, the opponent won't let us reach this branch
            if (score >= beta)
                return beta; // Beta cutoff

            if (score > alpha)
                alpha = score; // Update our best guaranteed score
       }

       return alpha;
   }
}
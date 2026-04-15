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
       

       // Get all legal moves into generator's fixed array. It uses a sentinel Move (Value==0) to mark the end.
       var moves = _gen.GenerateMoves(board);
       // iterate with index to avoid foreach overhead and work with the sentinel
       for (int i = 0; i < moves.Length; i++)
       {
           var move = moves[i];
           if (move.Value == 0) break;
           board.MakeMove(move);
           // Search the next level (notice the negative sign and swapped alpha/beta)
           int score = -AlphaBeta(board, depth - 1, -Infinity, Infinity);
           board.UnmakeMove(move);

           if (score > bestScore)
           {
               bestScore = score;
               bestMove = move;
           }
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
           return board.IsInCheck() ? -Infinity + (maxDepth - depth) : 0;

       for (int i = 0; i < moves.Length; i++)
       {
            var move = moves[i];
            if (move.Value == 0) break;

            board.MakeMove(move);
            bool isWhiteJustMoved = board.sideToMove == 1;
            ulong kingBitboard = isWhiteJustMoved ? board.WKings : board.BKings;
            int kingSq = BitOperations.TrailingZeroCount(kingBitboard);

            // If the opponent can attack our king, the move was illegal.
            if (board.IsSquareAttacked(kingSq, board.sideToMove == 0))
            {
                board.UnmakeMove(move);
                continue; // Skip this move and don't count it as a legal move
            }

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
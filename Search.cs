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
    private Opening _opening = new Opening();

    Stopwatch stopwatch;

    // Move ordering structures
    private readonly int[,] killerMoves = new int[64, 2]; // Two killer moves per depth
    private readonly int[,] historyMoves = new int[64, 64]; // History heuristic: from-to squares

    // Piece values for MVV-LVA (Most Valuable Victim - Least Valuable Attacker)
    private static readonly int[] PieceValues = { 0, 100, 320, 330, 500, 900, 20000 }; // None, Pawn, Knight, Bishop, Rook, Queen, King

    bool openingPhase = true;

    public Move GetBestMove(Board board, string history)
    {
        Move bestMove = default;
        stopwatch = new Stopwatch();
        stopwatch.Start();

        int depthReached = 0;

        var openingMove = _opening.GetOpeningMove(history, board);
        if (openingMove.Value != 0 && openingPhase)
        {
            Console.WriteLine(openingMove);
            return openingMove;
        }
        openingPhase = false;

        // Iterative deepening: search depth 1, 2, 3, ... until time runs out
        for (int currentDepth = 1; currentDepth <= maxDepth; currentDepth++)
        {
            if (stopwatch.ElapsedMilliseconds > 5000)
                break;

            int bestScore = -Infinity;
            Move bestMoveThisDepth = default;

            var moves = _gen.GenerateMoves(board);

            bool depthCompleted = true;

            for (int i = 0; i < moves.Length; i++)
            {
                var move = moves[i];
                if (move.Value == 0) break;

                if (stopwatch.ElapsedMilliseconds > 5000)
                {
                    depthCompleted = false;
                    break;
                }

                board.MakeMove(move);
                int score = -AlphaBeta(board, currentDepth - 1, -Infinity, Infinity);
                board.UnmakeMove(move);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMoveThisDepth = move;
                }
            }

            // Only update best move if we completed this depth
            if (depthCompleted && bestMoveThisDepth.Value != 0)
            {
                bestMove = bestMoveThisDepth;
                depthReached = currentDepth;
                Console.WriteLine($"Depth {currentDepth} completed - Time: {stopwatch.ElapsedMilliseconds}ms");
            }
            else
            {
                Console.WriteLine($"Depth {currentDepth} incomplete - using depth {depthReached} result");
                break;
            }
        }

        Console.WriteLine($"Max depth reached: {depthReached} in {stopwatch.ElapsedMilliseconds}ms");

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

        // Order moves for better alpha-beta pruning
        OrderMoves(board, moves, depth, default);

        for (int i = 0; i < moves.Length; i++)
        {
            if(stopwatch.ElapsedMilliseconds > 5000) // Time limit of 5 seconds for move calculation
            {
                break;
            }
            var move = moves[i];
            // sentinel: stop iterating when we hit a default/zero move
            if (move.Value == 0) break;
    
            board.MakeMove(move);
            int score = -AlphaBeta(board, depth - 1, -beta, -alpha);
            board.UnmakeMove(move);
    
            // Pruning: If the score is too good, the opponent won't let us reach this branch
            if (score >= beta)
            {
                // Store killer move
                if (!move.IsCapture && depth < 64)
                {
                    // Shift existing killer moves
                    if (killerMoves[depth, 0] != move.Value)
                    {
                        killerMoves[depth, 1] = killerMoves[depth, 0];
                        killerMoves[depth, 0] = move.Value;
                    }
                }
    
                // Update history heuristic
                if (!move.IsCapture)
                {
                    historyMoves[move.From, move.To] += depth * depth; // Deeper moves get higher bonus
                }
    
                return beta; // Beta cutoff
            }
    
            if (score > alpha)
            {
                alpha = score; // Update our best guaranteed score
    
                // Update history for non-captures
                if (!move.IsCapture)
                {
                    historyMoves[move.From, move.To] += depth;
                }
            }
        }
    
        return alpha;
    }

    private void OrderMoves(Board board, Move[] moves, int depth, Move pvMove)
    {
        // Score each move for ordering
        int[] scores = new int[moves.Length];

        for (int i = 0; i < moves.Length; i++)
        {
            if (moves[i].Value == 0) break;

            scores[i] = ScoreMove(board, moves[i], depth);
        }

        // Simple selection sort (good enough for small move lists)
        for (int i = 0; i < moves.Length - 1; i++)
        {
            if (moves[i].Value == 0) break;

            int bestIdx = i;
            for (int j = i + 1; j < moves.Length; j++)
            {
                if (moves[j].Value == 0) break;

                if (scores[j] > scores[bestIdx])
                {
                    bestIdx = j;
                }
            }

            if (bestIdx != i)
            {
                // Swap moves and scores
                (moves[i], moves[bestIdx]) = (moves[bestIdx], moves[i]);
                (scores[i], scores[bestIdx]) = (scores[bestIdx], scores[i]);
            }
        }
    }

    private int ScoreMove(Board board, Move move, int depth)
    {
        int score = 0;

        // 1. Promotions are very good
        if (move.IsPromotion)
        {
            score += 10000;
        }

        // 2. MVV-LVA for captures (Most Valuable Victim - Least Valuable Attacker)
        if (move.IsCapture)
        {
            int attackerPiece = GetPieceTypeAt(board, move.From, board.sideToMove == 0);
            int victimPiece = GetPieceTypeAt(board, move.To, board.sideToMove == 1);

            // Prioritize capturing valuable pieces with less valuable pieces
            // Multiply by 10 to make captures generally more valuable than quiet moves
            score += (PieceValues[victimPiece] - PieceValues[attackerPiece] / 10) * 10;
        }

        // 3. Killer moves (non-captures that caused beta cutoffs at this depth)
        if (!move.IsCapture && depth < 64)
        {
            if (killerMoves[depth, 0] == move.Value)
            {
                score += 9000;
            }
            else if (killerMoves[depth, 1] == move.Value)
            {
                score += 8000;
            }
        }

        // 4. History heuristic (moves that historically worked well)
        if (!move.IsCapture)
        {
            score += historyMoves[move.From, move.To];
        }

        return score;
    }

    private int GetPieceTypeAt(Board board, int square, bool isWhite)
    {
        ulong mask = 1UL << square;

        if (isWhite)
        {
            if ((board.WPawns & mask) != 0) return 1;
            if ((board.WKnights & mask) != 0) return 2;
            if ((board.WBishops & mask) != 0) return 3;
            if ((board.WRooks & mask) != 0) return 4;
            if ((board.WQueens & mask) != 0) return 5;
            if ((board.WKings & mask) != 0) return 6;
        }
        else
        {
            if ((board.BPawns & mask) != 0) return 1;
            if ((board.BKnights & mask) != 0) return 2;
            if ((board.BBishops & mask) != 0) return 3;
            if ((board.BRooks & mask) != 0) return 4;
            if ((board.BQueens & mask) != 0) return 5;
            if ((board.BKings & mask) != 0) return 6;
        }

        return 0; // No piece found
    }

    private Move UCIToMove(Board board, string uci)
    {
        int fromFile = uci[0] - 'a';
        int fromRank = uci[1] - '1';
        int toFile = uci[2] - 'a';
        int toRank = uci[3] - '1';
        Move move = new Move(from: fromRank * 8 + fromFile, to: toRank * 8 + toFile, 0); 
        return move;
    }
}
using System;
using System.IO;

namespace chess_engine_v2
{
    /// <summary>
    /// Unity-friendly API for the chess engine
    /// </summary>
    public class ChessEngineAPI
    {
        // Move flag constants for Unity
        public const ushort FLAG_NORMAL = 0;
        public const ushort FLAG_KINGSIDE_CASTLE = 2;
        public const ushort FLAG_QUEENSIDE_CASTLE = 3;
        public const ushort FLAG_CAPTURE = 4;
        public const ushort FLAG_PROMOTION = 8;
        public const ushort FLAG_CAPTURE_PROMOTION = 12; // FLAG_CAPTURE | FLAG_PROMOTION
        // Note: En passant uses FLAG_CAPTURE (4) on the en passant target square

        private readonly Board _board;
        private readonly Search _search;
        private readonly MovesGenerator _movesGenerator;
        private readonly Opening _opening;
        private string _moveHistory = "";

        public ChessEngineAPI()
        {
            _board = new Board();
            _search = new Search();
            _movesGenerator = new MovesGenerator();
            _opening = new Opening();
        }

        /// <summary>
        /// Get the best move for the current position
        /// Returns UCI format like "e2e4"
        /// </summary>
        public string GetBestUciMove()
        {
            // Try opening book first
            Move openingMove = _opening.GetOpeningMove(_moveHistory, _board);
            if (openingMove.Value != 0)
            {
                return MoveToUCI(openingMove);
            }

            // Use search
            Move bestMove = _search.GetBestMove(_board, _moveHistory);
            return MoveToUCI(bestMove);
        }

        public Move GetBestMove()
        {
            // Try opening book first
            Move openingMove = _opening.GetOpeningMove(_moveHistory, _board);
            if (openingMove.Value != 0)
            {
                return openingMove;
            }
            // Use search
            return _search.GetBestMove(_board, _moveHistory);
        }

        /// <summary>
        /// Make a move in UCI format (e.g., "e2e4", "e7e8q" for promotion)
        /// Returns true if the move was legal
        /// </summary>
        public bool MakeMove(string uciMove)
        {
            if (string.IsNullOrEmpty(uciMove) || uciMove.Length < 4)
                return false;

            int fromFile = uciMove[0] - 'a';
            int fromRank = uciMove[1] - '1';
            int toFile = uciMove[2] - 'a';
            int toRank = uciMove[3] - '1';

            if (fromFile < 0 || fromFile > 7 || toFile < 0 || toFile > 7 ||
                fromRank < 0 || fromRank > 7 || toRank < 0 || toRank > 7)
                return false;

            int from = fromRank * 8 + fromFile;
            int to = toRank * 8 + toFile;

            // Check for promotion piece
            bool hasPromotion = uciMove.Length > 4;
            char? promotionPiece = hasPromotion ? char.ToLower(uciMove[4]) : null;

            // Find the legal move that matches this
            var moves = _movesGenerator.GenerateMoves(_board);
            for (int i = 0; i < moves.Length; i++)
            {
                if (moves[i].Value == 0) break;

                // Match from/to squares
                if (moves[i].From == from && moves[i].To == to)
                {
                    // If it's a promotion, also check the promotion piece matches
                    if (hasPromotion)
                    {
                        if (!moves[i].IsPromotion)
                            continue;
                    }

                    _board.MakeMove(moves[i]);

                    // Update move history
                    if (!string.IsNullOrEmpty(_moveHistory))
                        _moveHistory += " ";
                    _moveHistory += uciMove;

                    return true;
                }
            }

            return false;
        }

        public void MakeMoveWithMove(Move move)
        {
            _board.MakeMove(move);
            // Update move history
            string uciMove = MoveToUCI(move);
            if (!string.IsNullOrEmpty(_moveHistory))
                _moveHistory += " ";
            _moveHistory += uciMove;
        }

        public bool MakeMoveWithFlags(int from, int to, ushort flags)
        {
            if (from < 0 || from > 63 || to < 0 || to > 63)
                return false;

            Move move = new Move(from, to, flags);
            _board.MakeMove(move);

            // Update move history
            string uciMove = MoveToUCI(move);
            if (!string.IsNullOrEmpty(_moveHistory))
                _moveHistory += " ";
            _moveHistory += uciMove;

            return true;
        }

        public bool MakeMoveWithFlags(string uciMove, ushort flags)
        {
            if (string.IsNullOrEmpty(uciMove) || uciMove.Length < 4)
                return false;

            int fromFile = uciMove[0] - 'a';
            int fromRank = uciMove[1] - '1';
            int toFile = uciMove[2] - 'a';
            int toRank = uciMove[3] - '1';

            if (fromFile < 0 || fromFile > 7 || toFile < 0 || toFile > 7 ||
                fromRank < 0 || fromRank > 7 || toRank < 0 || toRank > 7)
                return false;

            int from = fromRank * 8 + fromFile;
            int to = toRank * 8 + toFile;

            return MakeMoveWithFlags(from, to, flags);
        }

        /// <summary>
        /// Reset the board to starting position
        /// </summary>
        public void ResetBoard()
        {
            // Create new board (starts at initial position)
            _board.WPawns = 0x000000000000FF00;
            _board.WRooks = 0x0000000000000081;
            _board.WKnights = 0x0000000000000042;
            _board.WBishops = 0x0000000000000024;
            _board.WQueens = 0x0000000000000008;
            _board.WKings = 0x0000000000000010;

            _board.BPawns = 0x00FF000000000000;
            _board.BRooks = 0x8100000000000000;
            _board.BKnights = 0x4200000000000000;
            _board.BBishops = 0x2400000000000000;
            _board.BQueens = 0x0800000000000000;
            _board.BKings = 0x1000000000000000;

            _board.sideToMove = 0;
            _board.WhiteCanCastleK = true;
            _board.WhiteCanCastleQ = true;
            _board.BlackCanCastleK = true;
            _board.BlackCanCastleQ = true;
            _board.enPassantSquare = -1;

            _moveHistory = "";
        }

        /// <summary>
        /// Get current evaluation score (positive = white winning, negative = black winning)
        /// </summary>
        public int GetEvaluation()
        {
            var eval = new Evaluation();
            return eval.Evaluate(_board);
        }

        /// <summary>
        /// Check if current position is checkmate
        /// </summary>
        public bool IsCheckmate()
        {
            if (!_board.IsInCheck())
                return false;

            var moves = _movesGenerator.GenerateMoves(_board);
            return moves.Length == 0 || moves[0].Value == 0;
        }

        /// <summary>
        /// Check if current side is in check
        /// </summary>
        public bool IsInCheck()
        {
            return _board.IsInCheck();
        }

        /// <summary>
        /// Get whose turn it is (0 = white, 1 = black)
        /// </summary>
        public int GetSideToMove()
        {
            return _board.sideToMove;
        }

        /// <summary>
        /// Get all legal moves in UCI format
        /// </summary>
        public string[] GetLegalMoves()
        {
            var moves = _movesGenerator.GenerateMoves(_board);
            var result = new System.Collections.Generic.List<string>();

            for (int i = 0; i < moves.Length; i++)
            {
                if (moves[i].Value == 0) break;
                result.Add(MoveToUCI(moves[i]));
            }

            return result.ToArray();
        }

        private string MoveToUCI(Move move)
        {
            int fromFile = move.From % 8;
            int fromRank = move.From / 8;
            int toFile = move.To % 8;
            int toRank = move.To / 8;

            string uci = $"{(char)('a' + fromFile)}{fromRank + 1}{(char)('a' + toFile)}{toRank + 1}";

            // Add promotion piece if applicable
            if (move.IsPromotion)
            {
                uci += "q"; // Default to queen promotion (most common)
            }

            return uci;
        }
    }
}

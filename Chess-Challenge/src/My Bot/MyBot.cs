using System;
using System.Numerics;
using System.Collections.Generic;
using ChessChallenge.API;
public class MyBot : IChessBot
{
    private struct Evaluation
    {
        public int Value;
        public int UpperBound;
        public int LowerBound;
        public uint Depth;

        public static Evaluation FixedValue(uint depth, int value)
        {
            Evaluation eval;
            eval.Depth = depth;
            eval.Value = value;
            eval.UpperBound = value;
            eval.LowerBound = value;
            return eval;
        }

        public Evaluation Negate()
        {
            Evaluation eval;
            eval.Depth = Depth;
            eval.Value = -Value;
            eval.UpperBound = -LowerBound;
            eval.LowerBound = -UpperBound;
            return eval;
        }
    }

    private readonly Dictionary<ulong, Evaluation> evaluationCache = new Dictionary<ulong, Evaluation>();

    public Move Think(Board board, Timer timer)
    {
        const uint maxDepth = 5;
        Move move = Move.NullMove;
        for (uint depth = 1; depth <= maxDepth; depth++)
            AlphaBeta(board, depth, 50000, -50000, out move);

        return move;
    }

    Evaluation AlphaBeta(Board board, uint depth, int upperBound, int lowerBound, out Move bestMove)
    {
        bestMove = Move.NullMove;
        Evaluation bestEval;
        bestEval.Value = -50000;
        bestEval.UpperBound = upperBound;
        bestEval.LowerBound = lowerBound;
        bestEval.Depth = depth;
        int localLowerBound = lowerBound;
        
        if (board.IsDraw())
        {
            bestEval.Value = 0;
            return bestEval;
        }

        Move[] legalMoves = board.GetLegalMoves();
        
        if (board.IsInCheck() && legalMoves.Length == 0)
        {
            bestEval.Value = -50000;
            return bestEval;
        }

        bestMove = legalMoves[0];

        if (depth == 0)
            return EvalBoard(board, legalMoves, depth, upperBound, lowerBound);

        foreach(Move move in legalMoves)
        {
            board.MakeMove(move);
            Evaluation eval;
            bool previousEvalFound = evaluationCache.TryGetValue(board.ZobristKey, out Evaluation cachedEval);
            if (previousEvalFound && depth <= cachedEval.Depth && cachedEval.Value >= lowerBound && cachedEval.Value < upperBound)
            {
                eval = cachedEval;
            }
            else
            {
                eval = AlphaBeta(board, depth-1, -localLowerBound, -upperBound, out _);
            }
            board.UndoMove(move);
            eval = eval.Negate();
            if (eval.Value > bestEval.Value )
                bestMove = move;
            bestEval.Value = Math.Max(bestEval.Value, eval.Value);
            bestEval.UpperBound = Math.Min(bestEval.UpperBound, eval.UpperBound);
            bestEval.LowerBound = Math.Max(bestEval.LowerBound, eval.LowerBound);
            if (bestEval.Value >= upperBound)
                break;
            if (bestEval.Value > localLowerBound)
                localLowerBound = bestEval.Value;
        }

        bestEval.Depth = depth;
        evaluationCache[board.ZobristKey] = bestEval;
        return bestEval;
    }

    Evaluation EvalBoard(Board board, Move[] legalMoves, uint depth, int upperBound, int lowerBound)
    {
        Evaluation eval;
        eval.Depth = depth;
        eval.LowerBound = lowerBound;
        eval.UpperBound = upperBound;

        int[] pieceValues = { 0, 100, 300, 300, 500, 900, 50000 };
        int playerFactor = board.IsWhiteToMove ? 1 : -1;
        int pieceFactor;
        int materialValue = 0;
        int activityValue = 0;
        foreach(PieceList list in board.GetAllPieceLists())
        {
            foreach(Piece piece in list)
            {
                pieceFactor = piece.IsWhite ? 1 : -1; 
                materialValue += playerFactor * pieceFactor * pieceValues[(int)piece.PieceType];  
            }
        }

        int captures = 0;
        foreach(Move move in legalMoves)
        {
            if (move.IsCapture)
                captures++;
        }

        activityValue = legalMoves.Length + captures * 10;
        eval.Value = materialValue + activityValue;
        return eval;
    }
}
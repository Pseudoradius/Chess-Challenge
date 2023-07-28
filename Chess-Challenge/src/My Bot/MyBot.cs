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

    private readonly Dictionary<string, Evaluation> evaluationCache = new Dictionary<string, Evaluation>();

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        return moves[0];
    }

    Evaluation AlphaBeta(Board board, uint depth, int upperBound, int lowerBound, uint maxDepth)
    {
        Evaluation bestEval;
        bestEval.Value = int.MinValue;
        bestEval.UpperBound = upperBound;
        bestEval.LowerBound = lowerBound;
        bestEval.Depth = depth;
        int localLowerBound = lowerBound;
        
        if (board.IsDraw())
        {
            bestEval.Value = 0;
            return bestEval;
        }
        
        if (board.IsInCheckmate())
        {
            bestEval.Value = int.MinValue;
            return bestEval;
        }

        if (depth == maxDepth)
            return Evaluation.FixedValue(depth, 0); // Eval Function here

        foreach(Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            Evaluation eval = AlphaBeta(board, depth+1, -localLowerBound, -upperBound, maxDepth).Negate();
            board.UndoMove(move);
            bestEval.Value = Math.Max(bestEval.Value, eval.Value);
            bestEval.UpperBound = Math.Min(bestEval.UpperBound, eval.UpperBound);
            bestEval.LowerBound = Math.Max(bestEval.LowerBound, eval.LowerBound);
            if (bestEval.Value >= upperBound)
                break;
            if (bestEval.Value > localLowerBound)
                localLowerBound = bestEval.Value;
        }
        return bestEval;
    }

}
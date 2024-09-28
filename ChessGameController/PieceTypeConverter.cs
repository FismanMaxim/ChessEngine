using System;
using ChessEngine.ChessEngine;
using ChessEngine.ChessGameModel;
using ChessEngine.ChessGameView;

namespace ChessEngine.ChessGameController;

public static class PieceTypeConverter
{
    /**
     * Transforms an integer returned by model's inner pieces indexing into a piece type
     */
    public static PieceType PieceTypeFromModelIndexing(int n)
    {
        if (n == PieceManager.None) return PieceType.NONE;
        
        if (PieceManager.IsWhite(n))
        {
            return PieceManager.GetType(n) switch
            {
                PieceManager.Rook => PieceType.WHITE_ROOK,
                PieceManager.Knight => PieceType.WHITE_KNIGHT,
                PieceManager.Bishop => PieceType.WHITE_BISHOP,
                PieceManager.Queen => PieceType.WHITE_QUEEN,
                PieceManager.King => PieceType.WHITE_KING,
                PieceManager.Pawn => PieceType.WHITE_PAWN,
                _ => throw new InvalidOperationException()
            };
        }
        else
        {
            return PieceManager.GetType(n) switch
            {
                PieceManager.Rook => PieceType.BLACK_ROOK,
                PieceManager.Knight => PieceType.BLACK_KNIGHT,
                PieceManager.Bishop => PieceType.BLACK_BISHOP,
                PieceManager.Queen => PieceType.BLACK_QUEEN,
                PieceManager.King => PieceType.BLACK_KING,
                PieceManager.Pawn => PieceType.BLACK_PAWN,
                _ => throw new InvalidOperationException()
            };
        }
    }
}
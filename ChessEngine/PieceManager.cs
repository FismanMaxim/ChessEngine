namespace ChessEngine.ChessEngine;

/// <summary>
/// Represents a piece as a 5-bits number, where
/// bits 1-2 stand for color (WHITE=01xxx, BLACK=10xxx) and 
/// bits 3-5 for piece type (PAWN=xx001, KNIGHT=xx010, BISHOP=xx011, ROOK=xx100, QUEEN=xx101, KING=xx110)
/// </summary>
public static class PieceManager
{
    // Byte or short type would be smaller, but bitwise operators are defined for int only
    // Pieces codes should not be changed unwisely: their codes are base for promotion codes in Move.MoveFlag and most of the methods below
    public const int None = 0;
    public const int Pawn = 0b001; // 1
    public const int Knight = 0b010; // 2
    public const int Bishop = 0b011; // 3
    public const int Rook = 0b100; // 4
    public const int Queen = 0b101; // 5
    public const int King = 0b110; // 6

    public const int WhiteMask = 0b01_000; // 01_xxx
    public const int BlackMask = 0b10_000; // 10_xxx


    public static int GetType(int piece)
        => piece & 0b111;
    public static int GetColorIndex(int piece)
        => ((piece >> 3) & 0b11) == 0b01 ? Board.WhiteIndex : Board.BlackIndex;
    public static bool IsWhite(int piece)
        => ((piece >> 3) & 0b11) == 0b01;

    public static bool IsSlidingPiece(int piece)
    {
        int type = GetType(piece);
        return type is >= 3 and <= 5;
    }

    /// <summary>
    /// Is BISHOP or QUEEN
    /// </summary>
    public static bool IsDiagonal (int piece)
    {
        int type = GetType(piece);
        return type == 3 || type == 5;
    }
    /// <summary>
    /// Is ROOK or QUEEN
    /// </summary>
    public static bool IsOrthogonal(int piece)
    {
        int type = GetType(piece);
        return type == 4 || type == 5;
    }

    public static bool IsOfColor(int piece, int colorIndex)
        => piece != None && GetColorIndex(piece) == colorIndex;
}
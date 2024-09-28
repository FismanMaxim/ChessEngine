using System.ComponentModel.DataAnnotations;

namespace ChessEngine.ChessEngine;

/// <summary>
/// Contains all the information abour the state of the board. Namely, exactly the information stored in a FEN-string.
/// </summary>
public struct BoardState
{
    [Required] public int[] Squares { get; set; }

    public int SideToMove { get; set; }

    public int EnPassantFile { get; set; }

    public int HundredPliesCounter { get; set; }

    public int PlyIndex { get; set; }
    [Required] public int CastleRights { get; set; }

    public BoardState()
    {
    }

    public BoardState(Board board) : this(new int[64], board.IsWhiteMove() ? Color.White : Color.Black,
        board.GetEnPassantFile(), board.GetFiftyMovesPlyCount(), board.GetTotalPlies(), board.GetCastlingRights())
    {
        for (int i = 0; i < 64; i++)
        {
            Squares[i] = board.GetPiece(i);
        }
    }

    /// <summary>
    /// Contains all the information abour the state of the board. Namely, exactly the information stored in a FEN-string.
    /// </summary>
    public BoardState(int[] squares,
        int sideToMove,
        int enPassantFile,
        int hundredPliesCounter,
        int plyIndex,
        int castleRights)
    {
        Squares = squares;
        SideToMove = sideToMove;
        EnPassantFile = enPassantFile;
        HundredPliesCounter = hundredPliesCounter;
        PlyIndex = plyIndex;
        CastleRights = castleRights;
    }
}
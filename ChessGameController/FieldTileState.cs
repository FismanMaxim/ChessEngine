using ChessEngine.ChessGameModel;
using ChessEngine.ChessGameView;

namespace ChessEngine.ChessGameController;

public struct FieldTileState
{
    public readonly PieceType Piece;
    public BoardTileSpecialEffect Effect;

    public FieldTileState(PieceType piece)
    {
        Piece = piece;
    }
}

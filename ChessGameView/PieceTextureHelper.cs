using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ChessEngine.ChessGameView;

public class PieceTextureHelper
{
    private static readonly Dictionary<PieceType, string> texturesNames;
    private readonly Dictionary<PieceType, Texture2D> textures;

    static PieceTextureHelper()
    {
        texturesNames = new Dictionary<PieceType, string>
        {
            [PieceType.NONE] = "",
            [PieceType.WHITE_ROOK] = "wR",
            [PieceType.WHITE_KNIGHT] = "wN",
            [PieceType.WHITE_BISHOP] = "wB",
            [PieceType.WHITE_QUEEN] = "wQ",
            [PieceType.WHITE_KING] = "wK",
            [PieceType.WHITE_PAWN] = "wP",
            [PieceType.BLACK_ROOK] = "bR",
            [PieceType.BLACK_KNIGHT] = "bN",
            [PieceType.BLACK_BISHOP] = "bB",
            [PieceType.BLACK_QUEEN] = "bQ",
            [PieceType.BLACK_KING] = "bK",
            [PieceType.BLACK_PAWN] = "bP"
        };
    }

    public PieceTextureHelper(Game game)
    {
        textures = new Dictionary<PieceType, Texture2D>();

        foreach (var piece in Enum.GetValues<PieceType>())
        {
            if (piece == PieceType.NONE) continue;
            textures[piece] = game.Content.Load<Texture2D>("piece/" + texturesNames[piece]);
        }

        textures[PieceType.NONE] = null;
    }

    public Texture2D getTexture(PieceType piece)
    {
        return textures[piece];
    }
}
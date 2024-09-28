using System.Collections.Generic;

namespace ChessEngine.ChessEngine;

/// <summary>
/// Forsyth–Edwards Notation (FEN) is a standard notation for describing a particular board position of a chess game.
/// 
/// A FEN record contains six fields, each separated by a space. The fields are as follows:
/// 1) Piece placement data: letters (upper or lower) for pieces; digits for empty squares; slashes as ranks divisors;
/// 2) Side to move: "w" or "b"
/// 3) Castling rights: "Q", "K", 'q', 'k' for castles by white and black, queenside and kingside; "-" for no castle
/// 4) En passant target square: square name that a pawn has jumped over
/// 5) Halfmove clock: number of halfmoves (plies) since the last capture or pawn move
/// 6) Move index (>=1): as written before move in notation
/// </summary>
public static class FenManager
{
    #region Constants
    public const string StartGameFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    private static readonly Dictionary<char, int> PieceSymbolToPiece = new Dictionary<char, int>()
    {
        { 'p', PieceManager.Pawn }, {'r', PieceManager.Rook}, {'n', PieceManager.Knight}, {'b', PieceManager.Bishop}, {'q', PieceManager.Queen}, {'k', PieceManager.King}
    };
    private static readonly Dictionary<int, char> PieceToPieceSymbol = new Dictionary<int, char>()
    {
        { PieceManager.Pawn, 'p' }, { PieceManager.Rook, 'r' }, { PieceManager.Knight, 'n' }, {PieceManager.Bishop, 'b'}, {PieceManager.Queen, 'q'}, {PieceManager.King, 'k'}
    };
    private const int WhitePawnEnPassantRank = 3;
    private const int BlackPawnEnPassantRank = 6;
    #endregion

    public static BoardState FenToBoard (string fen)
    {
        string[] fenParts = fen.Split(" ");

        // Position
        int[] squares = new int[64];
        int file = 0;
        int rank = 0;
        foreach (char c in fenParts[0])
        {
            // Piece

            if (char.IsLetter(c))
            {
                int pieceColor = char.IsUpper(c) ? PieceManager.WhiteMask : PieceManager.BlackMask;
                squares[rank * 8 + file] = PieceSymbolToPiece[char.ToLower(c)] | pieceColor;

                file += 1;
            }
            // A sequence of empty squares
            else if (char.IsDigit(c))
            {
                int n = c - '0';
                file += n;
            }
            else
            {
                rank += 1;
                file = 0;
            }
        }
        
        // Move turn
        int sideToMove = (fenParts[1] == "w" ? Color.White : Color.Black);

        // Castle
        int castleRights = 0b0000;
        if (fenParts[2] != "-")
        {
            if (fenParts[2].Contains('K'))
                castleRights |= Board.WhiteCastleKingsideMask;
            if (fenParts[2].Contains('Q'))
                castleRights |= Board.WhiteCastleQueensideMask;
            if (fenParts[2].Contains('k'))
                castleRights |= Board.BlackCastleKingsideMask;
            if (fenParts[2].Contains('q'))
                castleRights |= Board.BlackCastleQueensideMask;
        }

        // En passant capture
        int enPassantFile = Board.NoEnPassant;
        if (fenParts[3] != "-")
            enPassantFile = fenParts[3][0] - 'a';

        // 50 moves count
        int hundredPliesCounter = int.Parse(fenParts[4]);

        // Move number
        int plyIndex = 2 * (int.Parse(fenParts[5])-1) + (sideToMove == Board.WhiteIndex ? 0 : 1);

        return new BoardState(squares, sideToMove, enPassantFile, hundredPliesCounter, plyIndex, castleRights);
    }

    public static string BoardToFen (Board board)
    {
        string fen = "";

        // Piece placement
        for (int rank = 0; rank < 8; rank++)
        {
            int countEmpty = 0;
            for (int file = 0; file < 8; file++)
            {
                int piece = board.GetPiece(rank * 8 + file);
                int pieceType = PieceManager.GetType(piece);
                if (pieceType == PieceManager.None)
                    countEmpty += 1;
                else
                {
                    if (countEmpty > 0)
                    {
                        fen += countEmpty.ToString();
                        countEmpty = 0;
                    }

                    char c = PieceManager.IsWhite(piece) ? char.ToUpper(PieceToPieceSymbol[pieceType]) : PieceToPieceSymbol[pieceType];
                    fen += c;
                }
            }

            if (countEmpty > 0)
                fen += countEmpty.ToString();
            if (rank < 7)
                fen += '/';
        }
        fen += ' ';

        // Move turn
        bool whiteMove = board.IsWhiteMove();
        fen += (whiteMove ? 'w' : 'b') + " ";

        // Castling
        string castle = "";
        int castleRights = board.GetCastlingRights();
        if ((castleRights & Board.WhiteCastleKingsideMask) == 1)
            castle += 'K';
        if ((castleRights & Board.WhiteCastleQueensideMask) == 1)
            castle += 'Q';
        if ((castleRights & Board.BlackCastleKingsideMask) == 1)
            castle += 'k';
        if ((castleRights & Board.BlackCastleQueensideMask) == 1)
            castle += 'q';
        fen += (castle == string.Empty ? '-' : castle) + " ";

        // En passant
        int epFile = board.GetEnPassantFile();
        if (epFile == Board.NoEnPassant)
            fen += '-';
        else
            fen += BoardRepresentation.FileIndexToFileName(epFile).ToString() + (whiteMove ? BlackPawnEnPassantRank : WhitePawnEnPassantRank);
        fen += " ";

        // Halfmove clock
        
        fen += board.GetFiftyMovesPlyCount() + " ";

        // Fullmove number
        fen += board.GetTotalPlies() / 2 + 1;
         
        return fen;
    }
}

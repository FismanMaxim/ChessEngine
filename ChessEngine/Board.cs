using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessEngine.ChessEngine;

public class Board
{
    #region Indexes and masks

    public const int WhiteIndex = 0;
    public const int BlackIndex = 1;

    public const int NoEnPassant = 8;

    public const int WhiteCastleKingsideMask = 0b0001;
    public const int WhiteCastleQueensideMask = 0b0010;
    public const int BlackCastleKingsideMask = 0b0100;
    public const int BlackCastleQueensideMask = 0b1000;

    #endregion

    #region Variables that define position

    /// <summary>
    /// 64-elements array of 5 digit binary numbers (see details in Piece)
    /// </summary>
    private readonly int[] _squares;

    /// <summary>
    /// Stores WHITE_INDEX or BLACK_INDEX
    /// </summary>
    private int _sideToMove;

    /// <summary>
    /// Stores number of plies made in the game
    /// </summary>
    private int _plyCounter;

    /// <summary>
    /// Zobrish hash of this position
    /// </summary>
    private ulong _zobristHash;

    /// <summary>
    /// Current game state (see _gameStateHistory). This is always the top element is _gameStateHistory.
    /// </summary>
    private int _gameState;

    private readonly PieceList[] _rooks, _knights, _bishops, _queens, _pawns;

    /// <summary>
    /// First agument - color index, second argument - piece type
    /// </summary>
    private readonly ulong[,] _piecesBitboards;

    /// <summary>
    /// Stores references to all 5 PieceLists, so that one can be got without enumerating through piece types, but by piece index<br/>
    /// First 0-5 items - white pieces, then 2 empty items; then 8-13 items - black pieces;
    /// </summary>
    private readonly PieceList[] _allPieceLists;

    private readonly int[] _kings;

    /// <summary>
    /// Stores some information of a position that is irrevocably lost after a move is made so that it can be retrieved in UnmakeMove()<br/>
    /// Bits 0-3 -- castling rights as a 4-bit number for white or black in kingside or queenside (see masks in Board)
    /// Bits 4-7 -- en passant file as a number within [0; 8] that represents a file where en passant capture can be made, NO_EN_PASSANT (=8) if no en passant
    /// Bits 8-12 -- taken piece
    /// Bits 13-19 -- fifty moves ply counter (number, no more than 100)
    /// </summary>
    private readonly Stack<int> _gameStateHistory;

    /// <summary>
    /// Stores zobrist hashes of consequitively occuring positions. <br/>
    /// It is used for defining draw by triple repition.
    /// This is synchonized with Stack _gameStateHistory in order.
    /// </summary>
    private readonly Stack<ulong> _repetitionsPositionsHistory;

    #endregion

    #region Self initializing

    public Board(BoardState boardState)
    {
        #region Initialize data structures

        _squares = new int[64];
        _rooks = [new PieceList(10), new PieceList(10)];
        _knights = [new PieceList(10), new PieceList(10)];
        _bishops = [new PieceList(10), new PieceList(10)];
        _queens = [new PieceList(9), new PieceList(9)];
        _pawns = [new PieceList(16), new PieceList(16)];
        _kings = new int[2];
        _piecesBitboards = new ulong[2, 6];
        _repetitionsPositionsHistory = new Stack<ulong>();
        _gameStateHistory = new Stack<int>();

        // The elements are placed so that to correspond the bitstring for each piece
        _allPieceLists =
        [
            null,
            _pawns[WhiteIndex],
            _knights[WhiteIndex],
            _bishops[WhiteIndex],
            _rooks[WhiteIndex],
            _queens[WhiteIndex],
            null,
            null,
            null,
            _pawns[BlackIndex],
            _knights[BlackIndex],
            _bishops[BlackIndex],
            _rooks[BlackIndex],
            _queens[BlackIndex]
        ];

        #endregion

        // Copy squares
        for (int pos = 0; pos < 64; pos++)
        {
            var piece = boardState.Squares[pos];
            _squares[pos] = piece;

            if (piece != PieceManager.None)
            {
                int pieceType = PieceManager.GetType(piece);
                int pieceColorIndex = PieceManager.GetColorIndex(piece);

                if (pieceType != PieceManager.King)
                {
                    GetPieceList(pieceColorIndex, pieceType).AddPiece(pos);
                    _piecesBitboards[pieceColorIndex, pieceType] |= 1ul << pos;
                }
                else
                    _kings[pieceColorIndex] = pos;
            }
        }

        // _sideToMove
        _sideToMove = boardState.SideToMove;

        // _gameState
        _gameState = (boardState.CastleRights) | (boardState.EnPassantFile << 4) |
                     (boardState.HundredPliesCounter << 13);
        _gameStateHistory.Push(_gameState);
        _plyCounter = boardState.PlyIndex;

        // Zobrist
        _zobristHash = ZobristHashing.GetZobristHash(this);
        _repetitionsPositionsHistory.Push(_zobristHash);
    }

    public Board(string fen) : this(FenManager.FenToBoard(fen))
    {
    }

    /// <summary>
    /// Return a copy of the board
    /// </summary>
    public Board(Board board)
    {
        #region Initialize data structures

        _squares = new int[64];
        _rooks = [new PieceList(10), new PieceList(10)];
        _knights = [new PieceList(10), new PieceList(10)];
        _bishops = [new PieceList(10), new PieceList(10)];
        _queens = [new PieceList(9), new PieceList(9)];
        _pawns = [new PieceList(16), new PieceList(16)];
        _kings = new int[2];
        _piecesBitboards = new ulong[2, 6];
        _repetitionsPositionsHistory = new Stack<ulong>();
        _gameStateHistory = new Stack<int>();

        #endregion

        #region Copy data

        _sideToMove = board._sideToMove;
        _plyCounter = board._plyCounter;
        _zobristHash = board._zobristHash;
        _gameState = board._gameState;
        Array.Copy(board._squares, _squares, 64);

        for (int i = 0; i < 2; i++)
        {
            _rooks[i] = new PieceList(board._rooks[i]);
            _bishops[i] = new PieceList(board._bishops[i]);
            _knights[i] = new PieceList(board._knights[i]);
            _queens[i] = new PieceList(board._queens[i]);
            _pawns[i] = new PieceList(board._pawns[i]);
            _kings[i] = board._kings[i];
            _piecesBitboards[i, PieceManager.Pawn] = board._piecesBitboards[i, PieceManager.Pawn];
            _piecesBitboards[i, PieceManager.Knight] = board._piecesBitboards[i, PieceManager.Knight];
            _piecesBitboards[i, PieceManager.Bishop] = board._piecesBitboards[i, PieceManager.Bishop];
            _piecesBitboards[i, PieceManager.Rook] = board._piecesBitboards[i, PieceManager.Rook];
            _piecesBitboards[i, PieceManager.Queen] = board._piecesBitboards[i, PieceManager.Queen];
        }

        // The elements are placed so that to correspond the bitstring for each piece
        _allPieceLists =
        [
            null,
            _pawns[WhiteIndex],
            _knights[WhiteIndex],
            _bishops[WhiteIndex],
            _rooks[WhiteIndex],
            _queens[WhiteIndex],
            null,
            null,
            null,
            _pawns[BlackIndex],
            _knights[BlackIndex],
            _bishops[BlackIndex],
            _rooks[BlackIndex],
            _queens[BlackIndex]
        ];
        
        #endregion

        _gameStateHistory = new Stack<int>(board._gameStateHistory);
        _repetitionsPositionsHistory = new Stack<ulong>(board._repetitionsPositionsHistory);
    }

    #endregion

    private PieceList GetPieceList(int colorIndex, int pieceIndex)
    {
        return _allPieceLists[colorIndex * 8 + pieceIndex];
    }

    #region Make/Unmake moves

    public void MakeMove(Move move, bool addToHistory = true)
    {
        int from = move.StartSquare;
        int to = move.TargetSquare;
        int flag = move.Flag;

        int movedPiece = GetPiece(from);
        int movedPieceType = PieceManager.GetType(movedPiece);
        int capturedPiece = PieceManager.None;
        bool isPawnMove = (movedPieceType == PieceManager.Pawn);

        int oldCastleState = GetCastlingRights();
        int newCastleState = oldCastleState;
        int oldEnPassantFile = GetEnPassantFile();
        int newEnPassantFile = NoEnPassant; // If there's, it will be set later
        int newFiftyMovesCounter = GetFiftyMovesPlyCount();

        int ownColorIndex = (PieceManager.IsWhite(movedPiece) ? WhiteIndex : BlackIndex);
        int oppositeColorIndex = (PieceManager.IsWhite(movedPiece) ? BlackIndex : WhiteIndex);
        int ownPieceColorMask = (PieceManager.IsWhite(movedPiece) ? PieceManager.WhiteMask : PieceManager.BlackMask);
        int oppositePieceColor = (PieceManager.IsWhite(movedPiece) ? PieceManager.BlackMask : PieceManager.WhiteMask);

        if (oldEnPassantFile != NoEnPassant)
            _zobristHash ^= ZobristHashing.EnPassantFileHashes[oldEnPassantFile]; // XOR out old en passant file

        #region Update _squares

        // Remove captured piece (if a capture)
        int pieceTypeOnTargetSquare =
            PieceManager.GetType(
                GetPiece(to)); // also captured piece type, unless en passant where captured piece is not on target square
        if (flag == Move.MoveFlag.EnPassantCapture)
        {
            int enPassantSquare = GetEnPassantVictimSquare(!IsWhiteMove()); // Square a captured pawn had been
            capturedPiece = (PieceManager.Pawn | oppositePieceColor);

            _squares[enPassantSquare] = PieceManager.None; // Remove captured pawn
            GetPieceList(oppositeColorIndex, PieceManager.Pawn).RemovePiece(enPassantSquare);
            _piecesBitboards[oppositeColorIndex, PieceManager.Pawn] ^= 1ul << enPassantSquare;
            _zobristHash ^=
                ZobristHashing.PiecesOnSquaresHashes
                    [oppositeColorIndex, PieceManager.Pawn, enPassantSquare]; // XOR out captured pawn
        }
        else if (pieceTypeOnTargetSquare != PieceManager.None) // Common capture
        {
            capturedPiece = (pieceTypeOnTargetSquare | oppositePieceColor);
            _squares[to] = PieceManager.None; // Remove captured piece
            GetPieceList(oppositeColorIndex, pieceTypeOnTargetSquare).RemovePiece(to);
            _piecesBitboards[oppositeColorIndex, pieceTypeOnTargetSquare] ^= 1ul << to;
            _zobristHash ^=
                ZobristHashing.PiecesOnSquaresHashes
                    [oppositeColorIndex, pieceTypeOnTargetSquare, to]; // XOR out captured piece
        }

        // Remove moving piece from old square
        _squares[from] = PieceManager.None;
        _zobristHash ^=
            ZobristHashing.PiecesOnSquaresHashes[ownColorIndex, movedPieceType, from]; // XOR out from old square
        // Promote moving piece if required
        if (move.IsPromotion)
        {
            movedPieceType = move.PromotedPieceType;
            movedPiece = (movedPieceType | ownPieceColorMask);

            GetPieceList(ownColorIndex, PieceManager.Pawn).RemovePiece(from);
            GetPieceList(ownColorIndex, movedPieceType).AddPiece(to);
            _piecesBitboards[ownColorIndex, PieceManager.Pawn] ^= (1ul << from) | (1ul << to);
        }
        else if (flag == Move.MoveFlag.Castle)
        {
            bool kingside = (to == BoardRepresentation.g1 || to == BoardRepresentation.g8);

            // Start & Target squares of castling rook
            int castlingRookFromSquare = (kingside ? to + 1 : to - 2);
            int castlingRookToSquare = (kingside ? to - 1 : to + 1);

            _squares[castlingRookFromSquare] = PieceManager.None; // Remove rook from old square
            _squares[castlingRookToSquare] = (PieceManager.Rook | ownPieceColorMask); // Put rook into new square
            GetPieceList(ownColorIndex, PieceManager.Rook).MovePiece(castlingRookFromSquare, castlingRookToSquare);
            _piecesBitboards[ownColorIndex, PieceManager.Rook] ^=
                (1ul << castlingRookFromSquare) | (1ul << castlingRookToSquare);
            _kings[ownColorIndex] = to;
            _zobristHash ^=
                ZobristHashing.PiecesOnSquaresHashes
                    [ownColorIndex, PieceManager.Rook, castlingRookFromSquare]; // XOR out from old square
            _zobristHash ^=
                ZobristHashing.PiecesOnSquaresHashes
                    [ownColorIndex, PieceManager.Rook, castlingRookToSquare]; // XOR in into new square
        }
        else if (movedPieceType == PieceManager.King)
            _kings[ownColorIndex] = to;
        else
        {
            GetPieceList(ownColorIndex, movedPieceType).MovePiece(from, to);
            _piecesBitboards[ownColorIndex, movedPieceType] ^= (1ul << from) | (1ul << to);
        }

        // - Put moving piece to new place
        _squares[to] = movedPiece;
        _zobristHash ^=
            ZobristHashing.PiecesOnSquaresHashes[ownColorIndex, movedPieceType, to]; // XOR in into new square
        // Handle castle:

        #endregion

        #region Update _sideToMove

        _sideToMove = (_sideToMove == WhiteIndex ? BlackIndex : WhiteIndex);
        _zobristHash ^= ZobristHashing.BlackToMoveHash; // XOR over move turn

        #endregion

        #region Update _plyCounter

        _plyCounter += 1;

        #endregion

        #region Update _gameState

        // Update castle state if needed
        if (newCastleState != 0)
        {
            // King's move loses castle for this color
            if (from == BoardRepresentation.e1)
            {
                newCastleState &= ~WhiteCastleKingsideMask;
                newCastleState &= ~WhiteCastleQueensideMask;
            }

            if (from == BoardRepresentation.e8)
            {
                newCastleState &= ~BlackCastleKingsideMask;
                newCastleState &= ~BlackCastleQueensideMask;
            }

            // ROOK's move (or rook taken) loses castle for this color in this side
            if (from == BoardRepresentation.a1 || to == BoardRepresentation.a1)
                newCastleState &= ~WhiteCastleQueensideMask;
            else if (from == BoardRepresentation.h1 || to == BoardRepresentation.h1)
                newCastleState &= ~WhiteCastleKingsideMask;
            if (from == BoardRepresentation.a8 || to == BoardRepresentation.a8)
                newCastleState &= ~BlackCastleQueensideMask;
            else if (from == BoardRepresentation.h8 || to == BoardRepresentation.h8)
                newCastleState &= ~BlackCastleKingsideMask;
        }

        // Set en passant possible
        if (flag == Move.MoveFlag.PawnDoubleMove)
        {
            newEnPassantFile = BoardRepresentation.GetFileBySquare(to); // Square that a pawn has jumped over
            _zobristHash ^= ZobristHashing.EnPassantFileHashes[newEnPassantFile]; // XOR in new en passant file
        }

        // 50 moves counter increases is NOT a capture and NOT a pawn's move
        if (capturedPiece == PieceManager.None && isPawnMove == false)
        {
            newFiftyMovesCounter += 1;
        }
        else
        {
            newFiftyMovesCounter = 0;
        }

        _gameState = (newCastleState | newEnPassantFile << 4 | capturedPiece << 8 | newFiftyMovesCounter << 13);

        if (oldCastleState != newCastleState)
        {
            _zobristHash ^= ZobristHashing.CastlingRightsHashes[oldCastleState]; // XOR out old castle state
            _zobristHash ^= ZobristHashing.CastlingRightsHashes[newCastleState]; // XOR in new castle state
        }

        #endregion

        #region Update histories

        if (addToHistory)
        {
            _gameStateHistory.Push(_gameState);
            _repetitionsPositionsHistory.Push(_zobristHash);
        }

        #endregion
    }

    public void UnmakeMove(Move move, bool b = false)
    {
        int startSquare = move.StartSquare;
        int targetSquare = move.TargetSquare;
        int flag = move.Flag;

        int moveBeginPiece = GetPiece(targetSquare); // Piece that made move that we unmake
        int moveEndPiece = moveBeginPiece;
        int moveEndPieceType = PieceManager.GetType(moveEndPiece);
        int ownColorIndex = PieceManager.IsWhite(moveEndPiece) ? WhiteIndex : BlackIndex;
        int oppositeColorIndex = ownColorIndex == WhiteIndex ? BlackIndex : WhiteIndex;

        #region _squares

        int takenPiece = GetTakenPiece();
        if (flag == Move.MoveFlag.EnPassantCapture) // En passant capture
        {
            int takenPawnPos = ownColorIndex == WhiteIndex ? targetSquare + 8 : targetSquare - 8;
            _squares[takenPawnPos] = takenPiece;
            _squares[targetSquare] = PieceManager.None;
            GetPieceList(oppositeColorIndex, PieceManager.Pawn).AddPiece(takenPawnPos);
            _piecesBitboards[oppositeColorIndex, PieceManager.Pawn] ^= 1ul << takenPawnPos;
            GetPieceList(ownColorIndex, PieceManager.Pawn).MovePiece(targetSquare, startSquare);
            _piecesBitboards[ownColorIndex, PieceManager.Pawn] ^= (1ul << targetSquare) | (1ul << startSquare);
            _zobristHash ^= ZobristHashing.PiecesOnSquaresHashes[oppositeColorIndex, PieceManager.Pawn, takenPawnPos];
            _zobristHash ^= ZobristHashing.PiecesOnSquaresHashes[ownColorIndex, PieceManager.Pawn, targetSquare];
        }
        else if (takenPiece != PieceManager.None) // Common capture
        {
            int takenPieceType = PieceManager.GetType(takenPiece);

            _squares[targetSquare] = takenPiece;
            GetPieceList(oppositeColorIndex, takenPieceType).AddPiece(targetSquare);
            _piecesBitboards[oppositeColorIndex, takenPieceType] ^= 1ul << targetSquare;
            _zobristHash ^= ZobristHashing.PiecesOnSquaresHashes[ownColorIndex, moveEndPieceType, targetSquare];
            _zobristHash ^=
                ZobristHashing.PiecesOnSquaresHashes[oppositeColorIndex, PieceManager.GetType(takenPiece),
                    targetSquare];

            if (move.IsPromotion) // Capture AND promotion
            {
                moveBeginPiece = (moveBeginPiece & 0b11000) | (PieceManager.Pawn);
                GetPieceList(ownColorIndex, move.PromotedPieceType).RemovePiece(targetSquare);
                _piecesBitboards[ownColorIndex, move.PromotedPieceType] ^= 1ul << targetSquare;
                _pawns[ownColorIndex].AddPiece(startSquare);
            }
            else if (moveEndPieceType == PieceManager.King)
            {
                _kings[ownColorIndex] = startSquare;
            }
            else
            {
                GetPieceList(ownColorIndex, moveEndPieceType).MovePiece(targetSquare, startSquare);
                _piecesBitboards[ownColorIndex, moveEndPieceType] ^= (1ul << targetSquare) | (1ul << startSquare);
            }
        }
        else // Quiet move
        {
            if (flag == Move.MoveFlag.Castle)
            {
                bool kingside = (targetSquare == BoardRepresentation.g1 || targetSquare == BoardRepresentation.g8);

                int castleRookStartSquare = kingside ? targetSquare + 1 : targetSquare - 2;
                int castleRookTargetSquare = kingside ? targetSquare - 1 : targetSquare + 1;

                _squares[castleRookStartSquare] = _squares[castleRookTargetSquare];
                _squares[castleRookTargetSquare] = PieceManager.None;
                GetPieceList(ownColorIndex, PieceManager.Rook).MovePiece(castleRookTargetSquare, castleRookStartSquare);
                _piecesBitboards[ownColorIndex, PieceManager.Rook] ^=
                    (1ul << castleRookTargetSquare) | (1ul << castleRookStartSquare);
                _kings[ownColorIndex] = startSquare;
                _zobristHash ^=
                    ZobristHashing.PiecesOnSquaresHashes[ownColorIndex, PieceManager.Rook, castleRookStartSquare];
                _zobristHash ^=
                    ZobristHashing.PiecesOnSquaresHashes[ownColorIndex, PieceManager.Rook, castleRookTargetSquare];
            }
            else if (moveEndPieceType == PieceManager.King)
                _kings[ownColorIndex] = startSquare;
            else if (move.IsPromotion)
            {
                moveBeginPiece = (moveBeginPiece & 0b11000) | (PieceManager.Pawn);
                _pawns[ownColorIndex].AddPiece(startSquare);
                GetPieceList(ownColorIndex, move.PromotedPieceType).RemovePiece(targetSquare);
                _piecesBitboards[ownColorIndex, move.PromotedPieceType] ^= 1ul << targetSquare;
            }
            else
            {
                GetPieceList(ownColorIndex, moveEndPieceType).MovePiece(targetSquare, startSquare);
                _piecesBitboards[ownColorIndex, moveEndPieceType] ^= (1ul << targetSquare) | (1ul << startSquare);
            }

            _squares[targetSquare] = PieceManager.None;
            _zobristHash ^= ZobristHashing.PiecesOnSquaresHashes[ownColorIndex, moveEndPieceType, targetSquare];
        }


        _squares[startSquare] = moveBeginPiece;
        _zobristHash ^=
            ZobristHashing.PiecesOnSquaresHashes[ownColorIndex, PieceManager.GetType(moveBeginPiece), startSquare];

        #endregion

        #region _sideToMove

        _sideToMove = (_sideToMove == WhiteIndex ? BlackIndex : WhiteIndex);
        _zobristHash ^= ZobristHashing.BlackToMoveHash; // XOR over move turn

        #endregion

        #region _plyCounter

        _plyCounter -= 1;

        #endregion

        #region _gameState

        int newCastlingRights = GetCastlingRights();
        int newEnPassantFile = GetEnPassantFile();

        if (newEnPassantFile != NoEnPassant)
            _zobristHash ^= ZobristHashing.EnPassantFileHashes[newEnPassantFile];

        _gameStateHistory.Pop();
        _gameState = _gameStateHistory.Peek();

        int oldCastlingRights = GetCastlingRights();
        if (oldCastlingRights != newCastlingRights)
        {
            _zobristHash ^= ZobristHashing.CastlingRightsHashes[newCastlingRights];
            _zobristHash ^= ZobristHashing.CastlingRightsHashes[oldCastlingRights];
        }

        int oldEnPassantFile = GetEnPassantFile();
        if (oldEnPassantFile != NoEnPassant)
            _zobristHash ^= ZobristHashing.EnPassantFileHashes[oldEnPassantFile];

        #endregion

        #region _histories

        if (_repetitionsPositionsHistory.Count > 0)
            _repetitionsPositionsHistory.Pop();

        #endregion
    }

    #endregion

    #region Getting board state

    public bool IsWhiteMove()
        => _sideToMove == WhiteIndex;

    public int GetColourIndexToMove()
        => _sideToMove;

    public int GetTotalPlies()
        => _plyCounter;

    public ulong GetZobristHash()
        => _zobristHash;

    public int GetPiece(int pos)
        => _squares[pos];

    /// <summary>
    /// External change of _squares is not recommended, except for checking the en passant move legality
    /// </summary>
    public int SetPiece(int pos, int piece)
        => _squares[pos] = piece;

    public int GetGameState()
        => _gameState;

    public int GetEnPassantFile()
        => (_gameState >> 4) & 0b1111;

    private int GetEnPassantVictimSquare(bool whiteVictim)
        => (whiteVictim ? 32 : 24) + GetEnPassantFile();

    public int GetCastlingRights()
        => _gameState & 0b1111;

    public int GetFiftyMovesPlyCount()
        => (_gameState >> 13) & 0b1111111;

    private int GetTakenPiece()
        => (_gameState >> 8) & 0b11111;

    public PieceList GetRooks(int colorIndex)
        => _rooks[colorIndex];

    public PieceList GetKnights(int colorIndex)
        => _knights[colorIndex];

    public PieceList GetBishops(int colorIndex)
        => _bishops[colorIndex];

    public PieceList GetQueens(int colorIndex)
        => _queens[colorIndex];

    public PieceList GetPawns(int colorIndex)
        => _pawns[colorIndex];

    public int CountQueens => _queens[WhiteIndex].Count + _queens[BlackIndex].Count;
    public int CountRooks => _rooks[WhiteIndex].Count + _rooks[BlackIndex].Count;
    public int CountBishops => _bishops[WhiteIndex].Count + _bishops[BlackIndex].Count;
    public int CountKnights => _knights[WhiteIndex].Count + _knights[BlackIndex].Count;
    public int CountPawns => _pawns[WhiteIndex].Count + _pawns[BlackIndex].Count;

    public int GetKingPos(int colorIndex)
        => _kings[colorIndex];

    /// <summary>
    /// This returns true if current position has occured three or more times, which corresponds with the official FIDE threefold repetition rule
    /// </summary>
    public bool IsThreefoldRepetition()
        => _repetitionsPositionsHistory.Count(hash => hash == _zobristHash) >= 3;

    /// <summary>
    /// This returns true if current position has occured at least once before in the game, in other words detects "twofold repetition".
    /// This should only be used by AI, since in Search() of a position, before considering all moves, AI looks it up in Transposition Table. 
    /// Then, if current position is in the TT indeed, then the evaluation will be returned instantly.
    /// Then, in the root AI is likely to repeat the same move as in previous time this position occured, 
    /// not even knowing that if opponent also repeats his move, it might be a draw, while the position might have been won. 
    /// To avoid this, AI considers a game drawn if current position has occured at least once before, 
    /// in other words it does not provoke the second repetition if not necessary.
    /// Thus, since even "twofold" repetition does not happen (unless targeting to threefold repetition),
    /// the situation of AI unvoluntarily and unknowingly allowing its opponent to commit THREEfold repetition does not happen even more.
    /// TODO: Find a way to solve this because this sounds wrong as hell!
    /// </summary>
    public bool HasPositionOccuredBefore()
        => _repetitionsPositionsHistory.Count(hash => hash == _zobristHash) >= 2;

    public bool IsDrawByRule()
    {
        return IsThreefoldRepetition() || GetFiftyMovesPlyCount() >= 100;
    }

    public ulong GetPieceBitboard(int colorIndex, int pieceType)
        => _piecesBitboards[colorIndex, pieceType];

    #endregion
}
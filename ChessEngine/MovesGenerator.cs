using System;
using System.Collections.Generic;

namespace ChessEngine.ChessEngine;

public class MovesGenerator
{
    #region Directions

    private const int UP = 0;
    private const int RIGHT = 1;
    private const int DOWN = 2;
    private const int LEFT = 3;
    private const int UP_RIGHT = 4;
    private const int DOWN_RIGHT = 5;
    private const int DOWN_LEFT = 6;
    private const int UP_LEFT = 7;

    #endregion

    #region Precomputated data

    /// <summary>
    /// 4 orthogonal, then 4 diagonal, from top, clockwise:
    /// TOP, RIGHT, BOTTOM, LEFT, TOP-RIGHT, BOTTOM-RIGHT, BOTTOM-LEFT, TOP-LEFT
    /// </summary>
    private static readonly int[] _offsets = [-8, +1, +8, -1, -7, +9, +7, -9];

    private static readonly int[,] knightsCoordOffsets = { { 1, 2 }, { 2, 1 }, { 2, -1 }, { 1, -2 }, { -1, -2 }, { -2, -1 }, { -2, 1 }, { -1, 2 } };

    private static readonly int[,] kingCoordOffsets = { { 0, 1 }, { 1, 1 }, { 1, 0 }, { 1, -1 }, { 0, -1 }, { -1, -1 }, { -1, 0 }, { -1, 1 } };

    /// <summary>
    /// How many squares can a piece go from given square in given direction at maximum
    /// </summary>
    private static readonly int[,] _squaresToEnd = new int[64, 8];

    private static int[][] _knightTargets = null!;
    private static int[][] _kingTargets = null!;
    private static ulong[] knightAttacksBitmap = null!;
    private static ulong[] kingAttacksBitmap = null!;
    private static ulong[,] pawnAttacksBitmap = null!;
    private static readonly int[,] _pawnCaptureDirectionsIndices;

    /// <summary>
    /// In [i, j] stores what is the index of the direction (as used in _offsets) from i-th to j-th squares
    /// </summary>
    private static int[,] _directionsBetweenSquares = null!;

    #endregion

    private Board _board = null!;
    private List<Move> _moves = null!;

    private int _ownColorIndex;
    private int _opponentColorIndex;
    private int _castleRights;
    private bool _includeQuietMoves;

    /// <summary>
    /// i-th bit is 1 if i-th square is attacked by opponent's piece(-s)
    /// </summary>
    private ulong _opponentAttackMap;

    private ulong _opponentSlidingAttackMap, _opponentPawnsAttackMap, _opponentKnightAttackMap;
    private ulong _pinRaysMask;
    private ulong _checksMask;
    private bool _isCheck, _isDoubleCheck, _positionHasPins;
    private int _friendlyKingSquare;

    #region Precomputating data

    static MovesGenerator()
    {
        PrecomputateSquaresToEnd();
        PrecomputateKnightTargets();
        PrecomputateKingTargets();
        PrecomputateDirections();
        PrecomputateKnightAttackBitmap();
        PrecomputateKingAttackBitmap();
        PrecomputatePawnAttackBitmap();

        _pawnCaptureDirectionsIndices = new[,]
        {
            { UP_LEFT, UP_RIGHT }, // WHITE
            { DOWN_LEFT, DOWN_RIGHT } // BLACK
        };
    }

    private static void PrecomputateSquaresToEnd()
    {
        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                int square = rank * 8 + file;

                int up = rank;
                int right = 7 - file;
                int down = 7 - rank;
                int left = file;

                _squaresToEnd[square, UP] = up;
                _squaresToEnd[square, RIGHT] = right;
                _squaresToEnd[square, DOWN] = down;
                _squaresToEnd[square, LEFT] = left;
                _squaresToEnd[square, UP_RIGHT] = Math.Min(up, right);
                _squaresToEnd[square, DOWN_RIGHT] = Math.Min(down, right);
                _squaresToEnd[square, DOWN_LEFT] = Math.Min(down, left);
                _squaresToEnd[square, UP_LEFT] = Math.Min(up, left);
            }
        }
    }

    private static void PrecomputateKnightTargets()
    {
        _knightTargets = new int[64][];
        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                int pos = rank * 8 + file;

                List<int> targets = new List<int>();
                for (int offSetIndex = 0; offSetIndex < knightsCoordOffsets.GetLength(0); offSetIndex++)
                {
                    int fileOffset = knightsCoordOffsets[offSetIndex, 0];
                    int rankOffset = knightsCoordOffsets[offSetIndex, 1];

                    int targetRank = rank + rankOffset;
                    int targetFile = file + fileOffset;

                    if (targetRank is >= 0 and <= 7 && targetFile is >= 0 and <= 7)
                        targets.Add(targetRank * 8 + targetFile);
                }

                _knightTargets[pos] = new int[targets.Count];
                for (int i = 0; i < targets.Count; i++)
                    _knightTargets[pos][i] = targets[i];
            }
        }
    }

    private static void PrecomputateKingTargets()
    {
        _kingTargets = new int[64][];
        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                int pos = rank * 8 + file;

                List<int> targets = new List<int>();
                for (int offSetIndex = 0; offSetIndex < kingCoordOffsets.GetLength(0); offSetIndex++)
                {
                    int fileOffset = kingCoordOffsets[offSetIndex, 0];
                    int rankOffset = kingCoordOffsets[offSetIndex, 1];

                    int targetRank = rank + rankOffset;
                    int targetFile = file + fileOffset;

                    if (targetRank is >= 0 and <= 7 && targetFile is >= 0 and <= 7)
                        targets.Add(targetRank * 8 + targetFile);
                }

                _kingTargets[pos] = new int[targets.Count];
                for (int i = 0; i < targets.Count; i++)
                    _kingTargets[pos][i] = targets[i];
            }
        }
    }

    private static void PrecomputateDirections()
    {
        _directionsBetweenSquares = new int[64, 64];
        for (int pos = 0; pos < 64; pos++)
        {
            for (int dirIndex = 0; dirIndex < 8; dirIndex++)
            {
                int offset = _offsets[dirIndex];
                for (int steps = 1; steps <= _squaresToEnd[pos, dirIndex]; steps++)
                {
                    int targetSquare = pos + offset * steps;

                    _directionsBetweenSquares[pos, targetSquare] = offset;
                }
            }
        }
    }

    private static void PrecomputateKnightAttackBitmap()
    {
        knightAttacksBitmap = new ulong[64];
        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                int pos = rank * 8 + file;

                for (int knightOffsetIndex = 0;
                     knightOffsetIndex < knightsCoordOffsets.GetLength(0);
                     knightOffsetIndex++)
                {
                    int newrank = rank + knightsCoordOffsets[knightOffsetIndex, 0];
                    int newfile = file + knightsCoordOffsets[knightOffsetIndex, 1];

                    if (BoardRepresentation.SquareExists(newrank, newfile))
                    {
                        int targetSquare = newrank * 8 + newfile;
                        knightAttacksBitmap[pos] |= 1ul << targetSquare;
                    }
                }
            }
        }
    }

    private static void PrecomputateKingAttackBitmap()
    {
        kingAttacksBitmap = new ulong[64];

        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                int pos = rank * 8 + file;

                for (int kingOffsetIndex = 0; kingOffsetIndex < kingCoordOffsets.GetLength(0); kingOffsetIndex++)
                {
                    int newrank = rank + kingCoordOffsets[kingOffsetIndex, 0];
                    int newfile = file + kingCoordOffsets[kingOffsetIndex, 1];

                    if (BoardRepresentation.SquareExists(newrank, newfile))
                    {
                        int targetSquare = newrank * 8 + newfile;
                        kingAttacksBitmap[pos] |= 1ul << targetSquare;
                    }
                }
            }
        }
    }

    private static void PrecomputatePawnAttackBitmap()
    {
        pawnAttacksBitmap = new ulong[2, 64];

        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                int pos = rank * 8 + file;

                // White pawns
                int newrank = rank - 1;
                int newfile = file - 1;
                int newpos = newrank * 8 + newfile;
                if (BoardRepresentation.SquareExists(newrank, newfile))
                    pawnAttacksBitmap[Board.WhiteIndex, pos] |= 1ul << newpos;

                newfile = file + 1;
                newpos = newrank * 8 + newfile;
                if (BoardRepresentation.SquareExists(newrank, newfile))
                    pawnAttacksBitmap[Board.WhiteIndex, pos] |= 1ul << newpos;

                // Black pawns
                newrank = rank + 1;
                newfile = file - 1;
                newpos = newrank * 8 + newfile;
                if (BoardRepresentation.SquareExists(newrank, newfile))
                    pawnAttacksBitmap[Board.BlackIndex, pos] |= 1ul << newpos;

                newfile = file + 1;
                newpos = newrank * 8 + newfile;
                if (BoardRepresentation.SquareExists(newrank, newfile))
                    pawnAttacksBitmap[Board.BlackIndex, pos] |= 1ul << newpos;
            }
        }
    }

    #endregion


    public List<Move> GenerateMoves(Board board, out bool isCheck, bool includingQuietMoves = true)
    {
        Reset();
        _board = board;
        _ownColorIndex = _board.GetColourIndexToMove();
        _opponentColorIndex = (_ownColorIndex + 1) % 2;
        _castleRights = _board.GetCastlingRights();
        _includeQuietMoves = includingQuietMoves;

        _friendlyKingSquare = _board.GetKingPos(_ownColorIndex);

        GenerateAttackMap();
        isCheck = _isCheck;

        GenerateKingMoves();

        // Double check can only be avoided by king's moves
        if (_isDoubleCheck)
            return _moves;

        GenerateSlidingMoves();
        GenerateKnightMoves();
        GeneratePawnMoves();

        return _moves;
    }

    public List<Move> GenerateMoves(Board board, bool includingQuietMoves = true)
    {
        return GenerateMoves(board, out _, includingQuietMoves);
    }

    private void Reset()
    {
        _moves = new List<Move>();
        _isCheck = false;
        _isDoubleCheck = false;
        _positionHasPins = false;
        _checksMask = 0;
        _pinRaysMask = 0;
        _opponentAttackMap = 0;
        _opponentKnightAttackMap = 0;
        _opponentPawnsAttackMap = 0;
        _opponentSlidingAttackMap = 0;
    }

    #region Generating Pieces Moves

    private void GenerateSlidingMoves()
    {
        PieceList rooks = _board.GetRooks(_ownColorIndex);
        for (int i = 0; i < rooks.Count; i++)
            GenerateSlidingPieceMoves(rooks[i], 0, 4);

        PieceList bishops = _board.GetBishops(_ownColorIndex);
        for (int i = 0; i < bishops.Count; i++)
            GenerateSlidingPieceMoves(bishops[i], 4, 8);

        PieceList queens = _board.GetQueens(_ownColorIndex);
        for (int i = 0; i < queens.Count; i++)
            GenerateSlidingPieceMoves(queens[i], 0, 8);
    }

    private void GenerateSlidingPieceMoves(int startSquare, int startDir, int endDir)
    {
        bool isPinned = IsPinned(startSquare);

        // A pinned piece cannot defend from check
        if (_isCheck && isPinned)
            return;

        for (int dirIndex = startDir; dirIndex < endDir; dirIndex++)
        {
            int offset = _offsets[dirIndex];

            // Pinned piece can only move along the ray to and from the king
            if (isPinned && !IsMovingAlongRay(startSquare, _friendlyKingSquare, offset))
                continue;

            for (int steps = 1; steps <= _squaresToEnd[startSquare, dirIndex]; steps++)
            {
                int targetSquare = startSquare + offset * steps;
                int targetSquarePiece = _board.GetPiece(targetSquare);
                bool hasPiece = targetSquarePiece != PieceManager.None;

                // Stuck in friendly piece
                if (hasPiece && PieceManager.GetColorIndex(targetSquarePiece) == _ownColorIndex)
                    break;

                bool blocksCheck = IsInCheckMask(targetSquare);
                bool capture = PieceManager.IsOfColor(targetSquarePiece, _opponentColorIndex);
                if (!_isCheck ||
                    blocksCheck) // move is possible, if it is not check OR it is one but the move prevents it
                    if (capture || _includeQuietMoves)
                        _moves.Add(new Move(startSquare, targetSquare));

                // Cannot move beyond a piece or after a move blocks check
                if (hasPiece || blocksCheck)
                    break;
            }
        }
    }

    private void GenerateKnightMoves()
    {
        PieceList knights = _board.GetKnights(_ownColorIndex);

        for (int i = 0; i < knights.Count; i++)
        {
            int startSquare = knights[i];

            // Pinned knight cannot move
            if (IsPinned(startSquare))
                continue;

            for (int targetSquareIndex = 0; targetSquareIndex < _knightTargets[startSquare].Length; targetSquareIndex++)
            {
                int targetSquare = _knightTargets[startSquare][targetSquareIndex];
                int targetSquarePiece = _board.GetPiece(targetSquare);

                // Cannot jump onto own piece
                if (PieceManager.IsOfColor(targetSquarePiece, _ownColorIndex))
                    continue;
                // If no check or the move block check
                bool capture = PieceManager.IsOfColor(targetSquarePiece, _opponentColorIndex);
                if (_isCheck == false || IsInCheckMask(targetSquare))
                    if (capture || _includeQuietMoves)
                        _moves.Add(new Move(startSquare, targetSquare));
            }
        }
    }

    private void GenerateKingMoves()
    {
        // Common moves
        for (int i = 0; i < _kingTargets[_friendlyKingSquare].Length; i++)
        {
            int targetSquare = _kingTargets[_friendlyKingSquare][i];

            // Cannot go on attacked square
            if (((_opponentAttackMap >> targetSquare) & 0b1) == 1)
                continue;

            int pieceOnTargetSquare = _board.GetPiece(targetSquare);

            // Cannot go on square with friendly piece
            if (pieceOnTargetSquare != PieceManager.None &&
                PieceManager.GetColorIndex(pieceOnTargetSquare) == _ownColorIndex)
                continue;

            bool capture = PieceManager.IsOfColor(pieceOnTargetSquare, _opponentColorIndex);
            if (capture || _includeQuietMoves)
                _moves.Add(new Move(_friendlyKingSquare, targetSquare));
        }

        // Castling
        if (!_isCheck)
        {
            // White's castle 
            if (_ownColorIndex == Board.WhiteIndex)
            {
                // King on correct square
                if (_friendlyKingSquare == BoardRepresentation.e1)
                {
                    // Kingside
                    if (CheckWhiteKingsideCastle())
                        _moves.Add(new Move(_friendlyKingSquare, BoardRepresentation.g1, Move.MoveFlag.Castle));
                    // Queenside
                    if (CheckWhiteQueensideCastle())
                        _moves.Add(new Move(_friendlyKingSquare, BoardRepresentation.c1, Move.MoveFlag.Castle));
                }
            }
            // Black's castle
            else
            {
                // King on correct square
                if (_friendlyKingSquare == BoardRepresentation.e8)
                {
                    if (CheckBlackKingsideCastle())
                        _moves.Add(new Move(_friendlyKingSquare, BoardRepresentation.g8, Move.MoveFlag.Castle));
                    // Queenside
                    if (CheckBlackQueensideCastle())
                        _moves.Add(new Move(_friendlyKingSquare, BoardRepresentation.c8, Move.MoveFlag.Castle));
                }
            }
        }
    }

    private void GeneratePawnMoves()
    {
        PieceList pawns = _board.GetPawns(_ownColorIndex);
        int pawnOffset = (_ownColorIndex == Board.WhiteIndex ? -8 : 8);
        int initPawnRank = (_ownColorIndex == Board.WhiteIndex ? 6 : 1);
        int readyToPromoteRank = (_ownColorIndex == Board.WhiteIndex ? 1 : 6);

        int enPassantFile = _board.GetEnPassantFile();
        int enPassantOverjumpedSquare = -1;
        if (enPassantFile != Board.NoEnPassant)
            enPassantOverjumpedSquare = (_ownColorIndex == Board.WhiteIndex ? 16 : 40) + _board.GetEnPassantFile();

        for (int i = 0; i < pawns.Count; i++)
        {
            int startSquare = pawns[i];
            int rank = BoardRepresentation.GetRankBySquare(startSquare);
            int targetSquare = startSquare + pawnOffset;

            #region Quiet moves

            if (_includeQuietMoves)
            {
                if (_board.GetPiece(targetSquare) == PieceManager.None) // No piece in forward square
                {
                    if (!IsPinned(startSquare) ||
                        IsMovingAlongRay(_friendlyKingSquare, startSquare, pawnOffset)) // Not pinned
                    {
                        if (!_isCheck || IsInCheckMask(targetSquare)) // No check or blocks check
                        {
                            if (rank == readyToPromoteRank)
                                AddPromotingMoves(startSquare, targetSquare);
                            else
                                _moves.Add(new Move(startSquare, targetSquare));
                        }

                        // Check for double move
                        if (rank == initPawnRank)
                        {
                            targetSquare = targetSquare + pawnOffset;

                            if (_board.GetPiece(targetSquare) == PieceManager.None)
                                if (!_isCheck || IsInCheckMask(targetSquare))
                                    _moves.Add(new Move(startSquare, targetSquare, Move.MoveFlag.PawnDoubleMove));
                        }
                    }
                }
            }

            #endregion

            #region Captures

            for (int j = 0; j < 2; j++)
            {
                int dirIndex = _pawnCaptureDirectionsIndices[_ownColorIndex, j];

                // Edge of the board
                if (_squaresToEnd[startSquare, dirIndex] == 0)
                    continue;

                int offset = _offsets[dirIndex];
                targetSquare = startSquare + offset;
                int targetSquarePiece = _board.GetPiece(targetSquare);

                if (IsPinned(startSquare) && !IsMovingAlongRay(_friendlyKingSquare, startSquare, offset))
                    continue;

                // Common capture
                if (targetSquarePiece != PieceManager.None &&
                    PieceManager.GetColorIndex(targetSquarePiece) == _opponentColorIndex)
                {
                    if (_isCheck && !IsInCheckMask(targetSquare))
                        continue;

                    if (rank == readyToPromoteRank)
                        AddPromotingMoves(startSquare, targetSquare);
                    else
                        _moves.Add(new Move(startSquare, targetSquare));
                }

                // En passant capture
                if (targetSquare == enPassantOverjumpedSquare)
                {
                    int capturedPawnSquare = enPassantOverjumpedSquare + (-pawnOffset);
                    if (WillBeCheckAfterEnPassant(startSquare, targetSquare, capturedPawnSquare) == false)
                        _moves.Add(new Move(startSquare, targetSquare, Move.MoveFlag.EnPassantCapture));
                }
            }

            #endregion
        }


        void AddPromotingMoves(int startSquare, int targetSquare)
        {
            _moves.Add(new Move(startSquare, targetSquare, Move.MoveFlag.PromotionToQueen));
            _moves.Add(new Move(startSquare, targetSquare, Move.MoveFlag.PromotionToKnight));
            _moves.Add(new Move(startSquare, targetSquare, Move.MoveFlag.PromotionToBishop));
            _moves.Add(new Move(startSquare, targetSquare, Move.MoveFlag.PromotionToRook));
        }
    }

    #endregion

    #region Check En Passant Legality

    /// <summary>
    /// This method imitates that the en passant move as made and checks in the king is in check in the new position
    /// </summary>
    private bool WillBeCheckAfterEnPassant(int startSquare, int targetSquare, int capturedPawnSquare)
    {
        _board.SetPiece(targetSquare, _board.GetPiece(startSquare));
        _board.SetPiece(startSquare, PieceManager.None);
        _board.SetPiece(capturedPawnSquare, PieceManager.None);

        bool willBeCheck = WillSquareBeAttackedAfterEnPassantCapture(capturedPawnSquare);

        _board.SetPiece(startSquare, _board.GetPiece(targetSquare));
        _board.SetPiece(targetSquare, PieceManager.None);
        _board.SetPiece(capturedPawnSquare,
            (PieceManager.Pawn | (_opponentColorIndex == Board.WhiteIndex
                ? PieceManager.WhiteMask
                : PieceManager.BlackMask)));

        return willBeCheck;
    }

    /// <summary>
    /// After en passant a check may occur if and only if a horizontal check got discovered on the rank where pawns had been
    /// </summary>
    /// <param name="capturedPawnSquare"></param>
    private bool WillSquareBeAttackedAfterEnPassantCapture(int capturedPawnSquare)
    {
        int dirIndex = (capturedPawnSquare < _friendlyKingSquare) ? LEFT : RIGHT;
        for (int steps = 1; steps <= _squaresToEnd[_friendlyKingSquare, dirIndex]; steps++)
        {
            int squareIndex = _friendlyKingSquare + _offsets[dirIndex] * steps;
            int piece = _board.GetPiece(squareIndex);
            if (piece != PieceManager.None)
            {
                if (PieceManager.GetColorIndex(piece) == _ownColorIndex) // Own piece
                    break;
                // Enemy piece
                if (PieceManager.IsOrthogonal(piece))
                    return true;
                break;
            }
        }

        return false;
    }

    #endregion

    #region Generating attack maps

    private void GenerateAttackMap()
    {
        #region Sliding Pieces

        GenerateSlidingAttackMap();

        // Search in all directions from the king to find pins and checks (possibly, double checks)
        int startDir = 0, endDir = 8;
        if (_board.GetQueens(_opponentColorIndex).Count == 0)
        {
            if (_board.GetRooks(_opponentColorIndex).Count == 0)
                startDir = 4;
            if (_board.GetBishops(_opponentColorIndex).Count == 0)
                endDir = 4;
        }

        for (int dirIndex = startDir; dirIndex < endDir; dirIndex++)
        {
            bool isDiagonal = dirIndex > 3;

            int n = _squaresToEnd[_friendlyKingSquare, dirIndex];
            int offset = _offsets[dirIndex];
            ulong rayMask = 0;

            bool hasMetFriendlyPiece = false; // A friedly piece can block check (i.e. be pinned)

            for (int steps = 1; steps <= n; steps++)
            {
                int targetSquare = _friendlyKingSquare + offset * steps;
                rayMask |= 1ul << targetSquare;
                int targetSquarePiece = _board.GetPiece(targetSquare);

                if (targetSquarePiece != PieceManager.None)
                {
                    if (PieceManager.GetColorIndex(targetSquarePiece) == _ownColorIndex)
                    {
                        // The first friendly piece on the ray may block check (i.e. be pinned)
                        if (!hasMetFriendlyPiece)
                        {
                            hasMetFriendlyPiece = true;
                        }
                        else // The second friendly piece in a row means there's no check on the ray
                        {
                            break;
                        }
                    }
                    else // Enemy's piece
                    {
                        // If a piece can attack along this direction (diagonal or non-diagonal)
                        if (isDiagonal && PieceManager.IsDiagonal(targetSquarePiece) ||
                            !isDiagonal && PieceManager.IsOrthogonal(targetSquarePiece))
                        {
                            // If a friendly piece was met, then it is pinned
                            if (hasMetFriendlyPiece)
                            {
                                _positionHasPins = true;
                                _pinRaysMask |= rayMask;
                            }
                            // If no friendly piece covers check, the king is in check
                            else
                            {
                                _checksMask |= rayMask;
                                _isDoubleCheck = _isCheck;
                                _isCheck = true;
                            }
                        }

                        // Not interested in anything behind the first enemy piece
                        break;
                    }
                }

                // Not interested in any other pins if double check, because it can only be avoided by king's move
                if (_isDoubleCheck)
                    break;
            }
        }

        #endregion

        #region Knights

        PieceList knights = _board.GetKnights(_opponentColorIndex);
        _opponentKnightAttackMap = 0;
        bool isKnightCheck = false;

        for (int knightIndex = 0; knightIndex < knights.Count; knightIndex++)
        {
            int startSquare = knights[knightIndex];
            _opponentKnightAttackMap |= knightAttacksBitmap[startSquare];
            if (!isKnightCheck && IsSquareInBitmap(_friendlyKingSquare, _opponentKnightAttackMap))
            {
                isKnightCheck = true;
                _isDoubleCheck = _isCheck;
                _isCheck = true;
                _checksMask |= 1ul << startSquare;
            }
        }

        #endregion

        #region Pawns

        PieceList opponentPawns = _board.GetPawns(_opponentColorIndex);
        _opponentPawnsAttackMap = 0;
        bool isPawnCheck = false;
        for (int pawnIndex = 0; pawnIndex < opponentPawns.Count; pawnIndex++)
        {
            int startSquare = opponentPawns[pawnIndex];
            ulong pawnAttacks = pawnAttacksBitmap[_opponentColorIndex, startSquare];
            _opponentPawnsAttackMap |= pawnAttacks;
            if (!isPawnCheck && IsSquareInBitmap(_friendlyKingSquare, pawnAttacks))
            {
                isPawnCheck = true;
                _isDoubleCheck = _isCheck; // if already in check, then this is double check
                _isCheck = true;
                _checksMask |= 1ul << startSquare;
            }
        }

        int enemyKingSquare = _board.GetKingPos(_opponentColorIndex);

        #endregion

        //opponentAttackMapNoPawns = opponentSlidingAttackMap | opponentKnightAttacks | kingAttackBitboards[enemyKingSquare];
        //_opponentAttackMap = opponentAttackMapNoPawns | opponentPawnAttackMap;
        _opponentAttackMap = _opponentSlidingAttackMap | _opponentKnightAttackMap | _opponentPawnsAttackMap |
                             kingAttacksBitmap[enemyKingSquare];
    }

    private void GenerateSlidingAttackMap()
    {
        _opponentSlidingAttackMap = 0;

        PieceList enemyRooks = _board.GetRooks(_opponentColorIndex);
        for (int i = 0; i < enemyRooks.Count; i++)
            AddSlidingAttackMap(enemyRooks[i], 0, 4);

        PieceList enemyBishops = _board.GetBishops(_opponentColorIndex);
        for (int i = 0; i < enemyBishops.Count; i++)
            AddSlidingAttackMap(enemyBishops[i], 4, 8);

        PieceList enemyQueens = _board.GetQueens(_opponentColorIndex);
        for (int i = 0; i < enemyQueens.Count; i++)
            AddSlidingAttackMap(enemyQueens[i], 0, 8);
    }

    private void AddSlidingAttackMap(int startSquare, int startDir, int endDir)
    {
        for (int dirIndex = startDir; dirIndex < endDir; dirIndex++)
        {
            int offset = _offsets[dirIndex];
            for (int steps = 1; steps <= _squaresToEnd[startSquare, dirIndex]; steps++)
            {
                int targetSquare = startSquare + offset * steps;
                int targetSquarePiece = _board.GetPiece(targetSquare);

                _opponentSlidingAttackMap |= 1ul << targetSquare;

                if (targetSquarePiece != PieceManager.None && targetSquare != _friendlyKingSquare)
                    break;
            }
        }
    }

    #endregion

    #region Checks in bitmaps

    private bool IsSquareAttacked(int square)
        => ((_opponentAttackMap >> square) & 0b1) == 1;

    private bool IsSquareInBitmap(int square, ulong bitmap)
        => ((bitmap >> square) & 0b1) == 1;

    private bool IsMovingAlongRay(int startSquare, int targetSquare, int comparedRay)
        => _directionsBetweenSquares[startSquare, targetSquare] == comparedRay ||
           _directionsBetweenSquares[startSquare, targetSquare] == -comparedRay;

    private bool IsPinned(int square)
        => _positionHasPins && ((_pinRaysMask >> square) & 0b1) == 1;

    private bool IsInCheckMask(int square)
        => _isCheck && ((_checksMask >> square) & 0b1) == 1;

    #endregion

    #region Check Castles Legality

    private bool CheckWhiteKingsideCastle()
        => (_castleRights & Board.WhiteCastleKingsideMask) > 0 &&
           _board.GetPiece(BoardRepresentation.f1) == PieceManager.None &&
           _board.GetPiece(BoardRepresentation.g1) == PieceManager.None &&
           IsSquareAttacked(BoardRepresentation.f1) == false &&
           IsSquareAttacked(BoardRepresentation.g1) == false;

    private bool CheckWhiteQueensideCastle()
        => (_castleRights & Board.WhiteCastleQueensideMask) > 0 &&
           _board.GetPiece(BoardRepresentation.d1) == PieceManager.None &&
           _board.GetPiece(BoardRepresentation.c1) == PieceManager.None &&
           _board.GetPiece(BoardRepresentation.b1) == PieceManager.None &&
           IsSquareAttacked(BoardRepresentation.d1) == false && IsSquareAttacked(BoardRepresentation.c1) == false;

    private bool CheckBlackKingsideCastle()
        => (_castleRights & Board.BlackCastleKingsideMask) > 0 &&
           _board.GetPiece(BoardRepresentation.f8) == PieceManager.None &&
           _board.GetPiece(BoardRepresentation.g8) == PieceManager.None &&
           IsSquareAttacked(BoardRepresentation.f8) == false &&
           IsSquareAttacked(BoardRepresentation.g8) == false;

    private bool CheckBlackQueensideCastle()
        => (_castleRights & Board.BlackCastleQueensideMask) > 0 &&
           _board.GetPiece(BoardRepresentation.d8) == PieceManager.None &&
           _board.GetPiece(BoardRepresentation.c8) == PieceManager.None &&
           _board.GetPiece(BoardRepresentation.b8) == PieceManager.None &&
           IsSquareAttacked(BoardRepresentation.d8) == false && IsSquareAttacked(BoardRepresentation.c8) == false;

    #endregion
}
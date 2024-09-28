using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ChessEngine.ChessEngine;
using ChessEngine.ChessEngine.ChessEngineAlgorithm;
using ChessEngine.ChessGameController;
using ChessEngine.Utils;

namespace ChessEngine.ChessGameModel;

// (Stores a copy of a board, on which it makes moves and checks whether a move can be made
// Stores both players and answers whether an answer should be expected, or it is a live player's turn, etc)
public class ChessGameModel
{
    public event Action OnPositionUpdated;

    private Board _board;
    private readonly MovesGenerator _movesGenerator = new();

    private bool _anyTileSelected;
    private Vector2Int _selectedTile;

    private readonly IChessAI _whiteAi, _blackAi;

    public ChessGameModel(IChessAI whiteAi, IChessAI blackAi)
    {
        _whiteAi = whiteAi;
        _blackAi = blackAi;
    }

    public void SetPosition(string fen)
    {
        _board = new Board(FenManager.FenToBoard(fen));

        _whiteAi?.Init(_board);
        _blackAi?.Init(_board);
    }

    public FieldTileState[,] GetTiles()
    {
        FieldTileState[,] tiles = new FieldTileState[8, 8];

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                tiles[i, j] = new FieldTileState(
                    PieceTypeConverter.PieceTypeFromModelIndexing(_board.GetPiece(i * 8 + j)));
            }
        }

        if (_anyTileSelected)
        {
            List<Move> moves = _movesGenerator.GenerateMoves(_board, out bool isInCheck);

            tiles[_selectedTile.x, _selectedTile.y].Effect = BoardTileSpecialEffect.HIGHLIGHTED;

            int selectedTileIndex = _selectedTile.x * 8 + _selectedTile.y;
            var movesFromSelectedTile = moves.Where(move => move.StartSquare == selectedTileIndex);
            foreach (var move in movesFromSelectedTile)
            {
                int targetX = BoardRepresentation.GetRankBySquare(move.TargetSquare);
                int targetY = BoardRepresentation.GetFileBySquare(move.TargetSquare);
                if (_board.GetPiece(move.TargetSquare) == PieceManager.None)
                    tiles[targetX, targetY].Effect = BoardTileSpecialEffect.SPOTTED;
                else
                    tiles[targetX, targetY].Effect = BoardTileSpecialEffect.TARGETED;
            }

            if (isInCheck)
            {
                int kingPos = _board.GetKingPos(_board.GetColourIndexToMove());
                int kingPosX = BoardRepresentation.GetRankBySquare(kingPos);
                int kingPosY = BoardRepresentation.GetFileBySquare(kingPos);
                tiles[kingPosX, kingPosY].Effect = BoardTileSpecialEffect.CHECKED;
            }
        }

        return tiles;
    }

    private void MakeMove(Move move)
    {
        _board.MakeMove(move);

        int sideToMove = _board.GetColourIndexToMove();
        IChessAI curAi = (sideToMove == Board.WhiteIndex ? _whiteAi : _blackAi);

        curAi?.AcceptMove(move, reply =>
        {
            _board.MakeMove(reply);
            OnPositionUpdated?.Invoke();
        });
    }

    public bool HandleTileClicked(Vector2Int tile, out Move move)
    {
        move = Move.InvalidMove;

        int sideToMove = _board.GetColourIndexToMove();
        int targetTileIndex = tile.x * 8 + tile.y;
        int clickedPieceIndex = _board.GetPiece(targetTileIndex);
        bool clickedEmptyTile = clickedPieceIndex == PieceManager.None;
        bool isOwnPiece =
            PieceManager.IsOfColor(clickedPieceIndex, sideToMove); // returns rubbish if empty tile

        if (_anyTileSelected)
        {
            // Clicked own piece
            if (!clickedEmptyTile && isOwnPiece)
            {
                _selectedTile = tile;
            }
            // Empty tile or enemy move -> try make move
            else if (sideToMove == Board.WhiteIndex && _whiteAi == null ||
                     sideToMove == Board.BlackIndex && _blackAi == null)
            {
                int startTileIndex = _selectedTile.x * 8 + _selectedTile.y;
                var moves = _movesGenerator.GenerateMoves(_board, out _);
                var suitedMoves = moves.Where(move =>
                    move.StartSquare == startTileIndex && move.TargetSquare == targetTileIndex).ToList();
                switch (suitedMoves.Count)
                {
                    // Cannot make move here
                    case 0:
                        _anyTileSelected = false;
                        break;
                    // Exactly one move
                    case 1:
                        move = suitedMoves[0];
                        MakeMove(move);
                        return true;
                    // We hardcode that a promotion can only be into a queen from a real player
                    default:
                        move = suitedMoves.First(move => move.Flag == Move.MoveFlag.PromotionToQueen);
                        MakeMove(move);
                        return true;
                }
            }
        }
        else
        {
            // Empty tile or enemy piece
            if (clickedEmptyTile || !isOwnPiece) return false;

            _anyTileSelected = true;
            _selectedTile = tile;
        }

        return false;
    }
}
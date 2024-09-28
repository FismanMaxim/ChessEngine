using System;
using ChessEngine.ChessGameModel;

namespace ChessEngine.ChessEngine.ChessEngineAlgorithm;

public interface IChessAI
{
    public void Init(Board board /*, other game settings*/);
    public void AcceptMove(Move move, Action<Move> responseCallback);
}
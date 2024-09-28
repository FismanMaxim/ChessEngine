using System;
using System.Threading;
using ChessEngine.ChessGameModel;

namespace ChessEngine.ChessEngine.ChessEngineAlgorithm;

public class ChessAI : IChessAI
{
    private readonly Action<Action<Move>> _activeSearchThreadStart;
    private readonly MovesGenerator _movesGenerator;
    private Board _board;

    public ChessAI()
    {
        _movesGenerator = new MovesGenerator();
        
        _activeSearchThreadStart = responseCallback =>
        {
            Move bestMove = SearchBestMove();
            responseCallback(bestMove);
            _board?.MakeMove(bestMove);
        };
    }

    #region Interface Implementation
    public void Init(Board board)
    {
        _board = board;
    }

    public void AcceptMove(Move move, Action<Move> responseCallback)
    {
        _board.MakeMove(move);

        Thread activeSearchThread = new Thread(() => _activeSearchThreadStart(responseCallback));
        activeSearchThread.Start();
    }
    #endregion
    
    private Move SearchBestMove()
    {
        
    }
}
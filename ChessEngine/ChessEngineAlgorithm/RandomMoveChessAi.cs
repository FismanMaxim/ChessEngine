using System;
using System.Threading;
using ChessEngine.ChessGameModel;

namespace ChessEngine.ChessEngine.ChessEngineAlgorithm;

public class RandomMoveChessAi : IChessAI
{
    private readonly MovesGenerator _movesGenerator;
    private Board _board;
    private Thread _activeSearchThread;

    public RandomMoveChessAi()
    {
        _movesGenerator = new MovesGenerator();
    }

    public void Init(Board board)
    {
        _board = new Board(board);
    }

    public void AcceptMove(Move move, Action<Move> responseCallback)
    {
        Console.WriteLine("Make move " + move + " in ai");
        _board.MakeMove(move);
        
        _activeSearchThread = new Thread(() =>
        {
            Thread.Sleep(1000);
            var moves = _movesGenerator.GenerateMoves(_board, out _);

            Move response = moves[new Random().Next() % moves.Count];
            responseCallback(response);

            Console.WriteLine("Make move " + response + " in ai");
            _board.MakeMove(response);
        });
        
        _activeSearchThread.Start();
    }
}
using ChessEngine.ChessEngine.ChessEngineAlgorithm;
using ChessEngine.ChessGameController;
using ChessEngine.ChessGameModel;

IChessAI whiteAI = null;
IChessAI blackAi = new RandomMoveChessAi();

var controller = new ChessGameController(whiteAI, blackAi);
controller.StartGame();




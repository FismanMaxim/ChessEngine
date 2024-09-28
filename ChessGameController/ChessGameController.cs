using ChessEngine.ChessEngine;
using ChessEngine.ChessEngine.ChessEngineAlgorithm;
using ChessEngine.ChessGameModel;
using ChessEngine.ChessGameView;
using ChessEngine.Utils;

namespace ChessEngine.ChessGameController;

public class ChessGameController
{
    private readonly ChessGameView.ChessGameView _view;
    private readonly ChessGameModel.ChessGameModel _model;
    
    public ChessGameController(IChessAI whiteAI, IChessAI blackAI)
    {
        _view = new ChessGameView.ChessGameView();
        _model = new ChessGameModel.ChessGameModel(whiteAI ,blackAI);
        
        _view.OnViewInitialized += HandleViewInitialized;
        _view.OnTileClicked += HandleTileClicked;

        _model.OnPositionUpdated += UpdatePositionInView;
    }

    public void StartGame()
    {
        _model.SetPosition(FenManager.StartGameFen);

        _view.Run();
    }

    private void HandleViewInitialized()
    {
        UpdatePositionInView();
    }

    private void UpdatePositionInView()
    {
        FieldTileState[,] field = _model.GetTiles();
        _view.SetPosition(field);
    }

    private void HandleTileClicked(Vector2Int tile)
    {
        // If move was made
        _model.HandleTileClicked(tile, out Move move);
        UpdatePositionInView();
    }
}
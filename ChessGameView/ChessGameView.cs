using ChessEngine.ChessGameController;
using ChessEngine.ChessGameModel;
using ChessEngine.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;

namespace ChessEngine.ChessGameView;

public class ChessGameView : Game
{
    public event System.Action OnViewInitialized;
    public event System.Action<Vector2Int> OnTileClicked;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _circleTexture, _squareTexture;

    private PieceTextureHelper _pieceTextureHelper;
    private FieldTileState[,] _field = new FieldTileState[8, 8];

    private const float TILE_TEXTURE_SIZE = 0.125f;
    private const float CIRCLE_TEXTURE_SIZE = 0.08f;
    private const int TILE_SIZE = 75;
    private const float PIECES_SIZE = 1.6f;
    private static readonly Color TILE_WHITE = new(240, 217, 181, 255);
    private static readonly Color TILE_BLACK = new(181, 136, 99, 255);
    private static readonly Color TILE_HIGHLIGHTED_WHITE = new(130, 151, 105, 255);
    private static readonly Color TILE_HIGHLIGHTED_BLACK = new(100, 111, 64, 255);

    public ChessGameView()
    {
        _graphics = new GraphicsDeviceManager(this);

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    public void SetPosition(FieldTileState[,] field)
    {
        _field = field;
    }

    protected override void Initialize()
    {
        _pieceTextureHelper = new PieceTextureHelper(this);

        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.ApplyChanges();

        base.Initialize();

        OnViewInitialized?.Invoke();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _circleTexture = Content.Load<Texture2D>("circle");
        _squareTexture = Content.Load<Texture2D>("square");
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        if (Mouse.GetState().LeftButton == ButtonState.Pressed)
        {
            if (tryMouseCoordToTile(new Vector2(Mouse.GetState().X, Mouse.GetState().Y), out Vector2Int tile))
            {
                OnTileClicked?.Invoke(tile);
            }
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();

        DrawEmptyBoard();
        DrawSpecialEffects();
        DrawPieces();

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawEmptyBoard()
    {
        Vector2 topLeftCorner = GetBoardTopLeftCorner();

        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                Vector2 location = new Vector2(topLeftCorner.X + TILE_SIZE * c, topLeftCorner.Y + TILE_SIZE * r);
                bool isWhite = (r % 2 == c % 2);

                Color color = isWhite ? TILE_WHITE : TILE_BLACK;

                _spriteBatch
                    .Draw(_squareTexture, location, null, color, 0,
                        Vector2.One, TILE_SIZE * TILE_TEXTURE_SIZE, SpriteEffects.None, 0);
            }
        }
    }

    private void DrawSpecialEffects()
    {
        Vector2 topLeftCorner = GetBoardTopLeftCorner();

        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                Vector2 location = new Vector2(topLeftCorner.X + TILE_SIZE * c, topLeftCorner.Y + TILE_SIZE * r);
                bool isWhite = (r % 2 == c % 2);
                Color color = (isWhite ? TILE_HIGHLIGHTED_WHITE : TILE_HIGHLIGHTED_BLACK);

                if (_field[r, c].Effect == BoardTileSpecialEffect.HIGHLIGHTED)
                {
                    _spriteBatch
                        .Draw(_squareTexture, location, null, color, 0,
                            Vector2.One, TILE_SIZE * TILE_TEXTURE_SIZE, SpriteEffects.None, 0);
                }
                else if (_field[r, c].Effect == BoardTileSpecialEffect.SPOTTED)
                {
                    location += Vector2.One * (TILE_SIZE / 2.0f) -
                                new Vector2(_circleTexture.Bounds.Size.X, _circleTexture.Bounds.Size.Y) *
                                (CIRCLE_TEXTURE_SIZE / 2.0f) - new Vector2(9, 9);
                    _spriteBatch
                        .Draw(_circleTexture, location, null, color, 0,
                            Vector2.One, CIRCLE_TEXTURE_SIZE, SpriteEffects.None, 0);
                }
                else if (_field[r, c].Effect == BoardTileSpecialEffect.TARGETED ||
                         _field[r, c].Effect == BoardTileSpecialEffect.CHECKED)
                {
                    _spriteBatch
                        .Draw(_squareTexture, location, null, Color.Red, 0,
                            Vector2.One, TILE_SIZE * TILE_TEXTURE_SIZE, SpriteEffects.None, 0);
                }
            }
        }
    }

    private void DrawPieces()
    {
        Vector2 topLeftCorner = GetBoardTopLeftCorner();

        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                if (_field[r, c].Piece != PieceType.NONE)
                {
                    Texture2D texture = _pieceTextureHelper.getTexture(_field[r, c].Piece);

                    Vector2 tileShift = new Vector2(c, r) * TILE_SIZE + Vector2.One * TILE_SIZE / 2;
                    Vector2 pieceShift = -new Vector2(texture.Bounds.Size.X * PIECES_SIZE / 2.0f,
                        texture.Bounds.Size.Y * PIECES_SIZE / 2.0f);
                    // I don't know why this shift is necessary, but without it pieces seem uncentered
                    Vector2 addShift = new Vector2(-8, -7);

                    Vector2 location = topLeftCorner + tileShift + pieceShift + addShift;

                    _spriteBatch
                        .Draw(texture, location, null, Color.White, 0,
                            Vector2.One, PIECES_SIZE, SpriteEffects.None, 0);
                }
            }
        }
    }

    private Vector2 GetBoardTopLeftCorner()
    {
        Vector2 screenCenter = new Vector2(Window.ClientBounds.Width / 2.0f, Window.ClientBounds.Height / 2.0f);
        return screenCenter - new Vector2(TILE_SIZE, TILE_SIZE) * 4;
    }

    private bool tryMouseCoordToTile(Vector2 coord, out Vector2Int tile)
    {
        Vector2 topLeftCorner = GetBoardTopLeftCorner();
        Vector2 bottomRightCorner = topLeftCorner + Vector2.One * 8 * TILE_SIZE;

        if (coord.X < topLeftCorner.X || coord.Y < topLeftCorner.Y
                                      || coord.X > bottomRightCorner.X || coord.Y > bottomRightCorner.Y)
        {
            tile = Vector2Int.Zero;
            return false;
        }

        tile = new Vector2Int(
            (int)((coord.Y - topLeftCorner.Y) / TILE_SIZE),
            (int)((coord.X - topLeftCorner.X) / TILE_SIZE));
        return true;
    }
}
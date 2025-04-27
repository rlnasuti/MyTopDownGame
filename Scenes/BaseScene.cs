using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

namespace MyTopDownGame.Scenes;

/// <summary>
/// Common functionality shared by all scenes.
/// </summary>
public abstract class BaseScene
{
    // ---------- engine plumbing ----------
    protected readonly Game1 _game;
    protected SpriteBatch SpriteBatch => _game.SpriteBatch;
    protected GraphicsDevice GraphicsDevice => _game.GraphicsDevice;
    protected ContentManager Content => _game.Content;
    protected Texture2D Pixel => _game.Pixel;      // 1×1 white – supplied by Game1

    // ---------- debug overlay ----------
    private bool _debugOverlayEnabled = true;
    private KeyboardState _prevKeyboardState;
    protected bool DebugOverlayEnabled => _debugOverlayEnabled;

    // ---------- minimap infrastructure ----------
    protected const int MinimapTileSize = 3;
    protected const int MinimapWindowTiles = 30;
    protected const int MinimapViewRadius = MinimapWindowTiles / 2;
    protected const int MinimapWidth = MinimapWindowTiles * MinimapTileSize;   // 90 px
    protected const int MinimapHeight = MinimapWindowTiles * MinimapTileSize;   // 90 px

    protected readonly RenderTarget2D _renderTarget;       // main world (640×360)
    protected readonly RenderTarget2D _minimapRenderTarget; // 90×90 minimap

    protected BaseScene(Game1 game)
    {
        _game = game;

        // Pre-create the scene’s render-targets.
        _renderTarget = new RenderTarget2D(GraphicsDevice, 640, 360);
        _minimapRenderTarget = new RenderTarget2D(GraphicsDevice, MinimapWidth, MinimapHeight);
    }

    // ---------- helper: toggle overlay with the tilde key ----------
    protected void HandleDebugToggle()
    {
        KeyboardState state = Keyboard.GetState();

        if (state.IsKeyDown(Keys.OemTilde) && !_prevKeyboardState.IsKeyDown(Keys.OemTilde))
            _debugOverlayEnabled = !_debugOverlayEnabled;

        _prevKeyboardState = state;
    }

    // ---------- helper: quick rectangle outline ----------
    protected void DrawBoundingBox(SpriteBatch sb, Rectangle rect, Color color)
    {
        // top
        sb.Draw(Pixel, new Rectangle(rect.Left, rect.Top, rect.Width, 1), color);
        // bottom
        sb.Draw(Pixel, new Rectangle(rect.Left, rect.Bottom - 1, rect.Width, 1), color);
        // left
        sb.Draw(Pixel, new Rectangle(rect.Left, rect.Top, 1, rect.Height), color);
        // right
        sb.Draw(Pixel, new Rectangle(rect.Right - 1, rect.Top, 1, rect.Height), color);
    }

    // ---------- contract for concrete scenes ----------
    public abstract void LoadContent();
    public abstract void Update(GameTime gameTime);
    public abstract void Draw(GameTime gameTime);
}
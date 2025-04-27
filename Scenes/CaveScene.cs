using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MyTopDownGame.Scenes;

public class CaveScene : BaseScene
{
    private readonly OverworldScene _overworld;
    private SpriteBatch _spriteBatch => _game.SpriteBatch;
    private SpriteFont _font;

    private KeyboardState _prevKeyboardState;

    public CaveScene(Game1 game, OverworldScene overworld) : base(game)
    {
        // Pass in the overworld scene to switch back to
        _overworld = overworld;
    }


    override public void LoadContent()
    {
        _font = _game.Content.Load<SpriteFont>("Fonts/comic_sans");
        // Capture current keyboard state so held keys don't instantly trigger exit
        _prevKeyboardState = Keyboard.GetState();
    }

    override public void Update(GameTime gameTime)
    {
        KeyboardState state = Keyboard.GetState();

        HandleDebugToggle();

        // Press any key (new press) to exit cave
        if (state.GetPressedKeys().Length > 0 && !_prevKeyboardState.IsKeyDown(state.GetPressedKeys()[0]))
        {
            // Switch back to overworld
            _game.SwitchScene(_overworld);
        }

        _prevKeyboardState = state;
    }

    override public void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin();
        string msg = "You are now inside the cave!";
        Vector2 size = _font.MeasureString(msg);
        Vector2 pos = new Vector2(
            (GraphicsDevice.Viewport.Width - size.X) / 2,
            (GraphicsDevice.Viewport.Height - size.Y) / 2);
        _spriteBatch.DrawString(_font, msg, pos, Color.White);
        _spriteBatch.End();
    }
}
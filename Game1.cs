using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using MyTopDownGame.Scenes;

namespace MyTopDownGame;

public class Tile
{
    public Texture2D Texture { get; init; }
    public Rectangle SourceRect { get; init; }
    public bool IsWalkable { get; init; }

    public Tile(Texture2D texture, Rectangle sourceRect, bool isWalkable)
    {
        Texture = texture;
        SourceRect = sourceRect;
        IsWalkable = isWalkable;
    }
}

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _pixel;

    private IScene _currentScene;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        // Load the initial scene (OverworldScene)
        _currentScene = new OverworldScene(this);
        _currentScene.LoadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        _currentScene?.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _currentScene?.Draw(gameTime);
        base.Draw(gameTime);
    }

    /// <summary>
    /// Switches the current scene to the provided newScene.
    /// </summary>
    public void SwitchScene(IScene newScene)
    {
        _currentScene = newScene;
        _currentScene?.LoadContent();
    }

    /// <summary>
    /// Allows scenes to access the pixel texture for debug/border drawing.
    /// </summary>
    public Texture2D Pixel => _pixel;

    /// <summary>
    /// Allows scenes to access the SpriteBatch.
    /// </summary>
    public SpriteBatch SpriteBatch => _spriteBatch;
}

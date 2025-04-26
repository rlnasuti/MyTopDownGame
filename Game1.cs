using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace MyTopDownGame;

public class Tile
{
    public Texture2D Texture { get; set; }
    public Rectangle SourceRect { get; set; }
    public bool IsWalkable { get; set; }

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

    private Texture2D _heroTexture;
    private Vector2 _heroPosition;
    private int _frame = 0;
    private float _timer = 0f;
    private float _animationSpeed = 0.2f;
    private const int _FrameWidth = 32;
    private const int _FrameHeight = 40;
    private enum Direction
    {
        Down = 0,
        Right = 1,
        Up = 2,
        Left = 3
    }
    private Direction _currentDirection = Direction.Down;
    private RenderTarget2D _renderTarget;
    private RenderTarget2D _minimapRenderTarget;

    private const int _TileSize = 32;

    private Tile[,] _tiles;

    private Texture2D _waterTexture;
    private Texture2D _grassTexture;
    private Texture2D _speedFruitTexture;
    private Texture2D _caveTopTexture;    // back layer (drawn behind hero)
    private Texture2D _caveBottomTexture; // front layer (drawn in front of hero)
    private Vector2 _cavePosition;
    private Rectangle _caveRect;
    List<AlienSpeedFruit> _fruits = new List<AlienSpeedFruit>();
    private static readonly Rectangle WaterTileSourceRect = new Rectangle(40, 50, 950, 900);
    private static readonly Rectangle GrassTileSourceRect = new Rectangle(0, 0, 1024, 1024);

    private Vector2 _cameraPosition;

    private readonly Random _random = new Random();

    private Texture2D _pixel;

    private const int MinimapWindowTiles = 30;       // 30×30 tile window
    private const int MinimapViewRadius = MinimapWindowTiles / 2; // 15 tiles each direction
    private const int MinimapTileSize = 3;                     // pixel size per map‑tile in minimap
    private const int MinimapWidth  = MinimapWindowTiles * MinimapTileSize;   // 90 px
    private const int MinimapHeight = MinimapWindowTiles * MinimapTileSize;   // 90 px

    private float _currentMoveSpeed;
    private const float NormalSpeed = 100f;
    private const float BuffedSpeed = 180f;
    private float _speedBuffTimer = 0;

    // Cave render dimensions (texture is scaled to this on screen)
    private const int CaveDrawWidth  = 256;
    private const int CaveDrawHeight = 256;
    // Vertical offset where the bottom slice starts (measured in source image pixels)
    private const int CaveSplitOffsetY = 442;
    // Scale factor from source texture to on‑screen size
    private const float CaveScale = 0.25f;  // original PNG is 1024 px wide → 256 px on screen

    // Cave collision helpers
    private Rectangle _caveCollisionRect;   // lower half of cave that is solid
    private Rectangle _caveEntranceRect;    // walk‑in entrance
    private float _entranceMsgTimer = 0f;   // seconds to display message

    private SpriteFont _font;

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
        _heroTexture = Content.Load<Texture2D>("Sprites/hero");
        _speedFruitTexture = Content.Load<Texture2D>("Sprites/speed_fruit");
        _waterTexture = Content.Load<Texture2D>("Tiles/water_highres");
        _grassTexture = Content.Load<Texture2D>("Tiles/grass_highres");
        _caveTopTexture    = Content.Load<Texture2D>("Objects/cave_top");
        _caveBottomTexture = Content.Load<Texture2D>("Objects/cave_bottom");
        _renderTarget = new RenderTarget2D(GraphicsDevice, 640, 360);
        _minimapRenderTarget = new RenderTarget2D(GraphicsDevice, MinimapWidth, MinimapHeight);
        _font = Content.Load<SpriteFont>("Fonts/comic_sans");

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _tiles = new Tile[100, 100];

        int centerX = 50;
        int centerY = 50;
        int islandRadius = 40;
        int lakeRadius = 8;

        for (int y = 0; y < 100; y++)
        {
            for (int x = 0; x < 100; x++)
            {
                double distanceToCenter = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));
                bool isLand = distanceToCenter < islandRadius && distanceToCenter > lakeRadius;
                bool isWalkable = isLand;

                _tiles[y, x] = isLand
                    ? new Tile(_grassTexture, GrassTileSourceRect, isWalkable)
                    : new Tile(_waterTexture, WaterTileSourceRect, false);
            }
        }

        // Position cave near the northern coastline
        _cavePosition = new Vector2(50 * _TileSize, 15 * _TileSize);

        // Rectangle representing the on‑screen footprint (after scaling)
        _caveRect = new Rectangle((int)_cavePosition.X,
                                  (int)_cavePosition.Y,
                                  CaveDrawWidth,
                                  CaveDrawHeight);

        /* ----- Collision setup -----
           We treat the lower‑front 2 tiles (64 px) as solid, except for
           a 96×48‑pixel doorway centred at the bottom of that zone.
           Everything above remains pass‑through so the hero can walk “behind”.
        */

        int entranceWidth  = 36;
        int entranceHeight = 48;

        // Entrance sits one tile (32 px) higher than before
        _caveEntranceRect = new Rectangle(
            _caveRect.X + (_caveRect.Width - entranceWidth) / 2,
            _caveRect.Bottom - entranceHeight - (2 *_TileSize),  // shift up 1 tile
            entranceWidth,
            entranceHeight);

        // Solid collision: 2‑tile (64 px) high strip above the new entrance Y
        _caveCollisionRect = new Rectangle(
            _caveRect.X + (entranceWidth / 2),
            _caveRect.Bottom - (4 * _TileSize) - _TileSize,   // top of solid zone
            _caveRect.Width - entranceWidth,         // width of solid zone
            3 * _TileSize);                       // 64 px tall

        // Spawn hero north of the lake, centered in X,
        // and halfway between lake top and cave entrance in Y.
        int spawnTileX = 50; // map center
        int spawnTileY = 32; // halfway between lake top (~42) and cave entrance (~23)
        _heroPosition = new Vector2(spawnTileX * _TileSize, spawnTileY * _TileSize);

        for (int i = 0; i < 10; i++)
        {
            int x = _random.Next(0, 100);
            int y = _random.Next(0, 100);
            if (_tiles[y, x].IsWalkable)
            {
                _fruits.Add(new AlienSpeedFruit(new Vector2(x * _TileSize, y * _TileSize)));
            }
        }

        _currentMoveSpeed = NormalSpeed;
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        KeyboardState state = Keyboard.GetState();
        Vector2 movement = Vector2.Zero;

        if (state.IsKeyDown(Keys.Down))
        {
            movement.Y += 1;
            _currentDirection = Direction.Down;
        }
        if (state.IsKeyDown(Keys.Up))
        {
            movement.Y -= 1;
            _currentDirection = Direction.Up;
        }
        if (state.IsKeyDown(Keys.Left))
        {
            movement.X -= 1;
            _currentDirection = Direction.Left;
        }
        if (state.IsKeyDown(Keys.Right))
        {
            movement.X += 1;
            _currentDirection = Direction.Right;
        }

        if (movement != Vector2.Zero)
        {
            _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_timer > _animationSpeed)
            {
                _frame = (_frame + 1) % 2; // since each direction only has 2 frames
                _timer = 0f;
            }

            Vector2 proposedPosition = _heroPosition + movement * _currentMoveSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            // Convert to tile coordinates based on center of the hero
            int tileX = (int)(proposedPosition.X + _FrameWidth / 2) / _TileSize;
            int tileY = (int)(proposedPosition.Y + _FrameHeight / 2) / _TileSize;

            bool tileOk = _tiles[tileY, tileX].IsWalkable;

            // Use a narrow rectangle representing the hero's feet (bottom 8 px)
            Rectangle feetRect = new Rectangle(
                (int)proposedPosition.X + 4,            // slight inset for better feel
                (int)proposedPosition.Y + _FrameHeight - 8,
                _FrameWidth - 16,
                8);                                     // 8‑pixel tall strip

            bool hitsEntrance = feetRect.Intersects(_caveEntranceRect);
            bool hitsCave     = feetRect.Intersects(_caveCollisionRect) && !hitsEntrance;

            if (tileOk && !hitsCave)
            {
                _heroPosition = proposedPosition;   // move accepted
            }

            if (hitsEntrance && hitsCave)
            {
                // Cave entrance detected
                _entranceMsgTimer = 2f;            // seconds
            }
            else if (hitsCave)
            {
                // Cave collision detected
                _heroPosition = new Vector2(_heroPosition.X, _heroPosition.Y);
                _frame = 0; // idle
            }
            else if (hitsEntrance)
            {
                // Cave entrance detected
                _heroPosition = proposedPosition; // move accepted
                _entranceMsgTimer = .5f;  // flash message for 2 seconds (stub for scene load)
            }
        }
        else
        {
            _frame = 0; // idle
        }

        Rectangle heroRect = new Rectangle((int)_heroPosition.X, (int)_heroPosition.Y, _FrameWidth, _FrameHeight);
        for (int i = _fruits.Count - 1; i >= 0; i--)
        {
            if (_fruits[i].CheckPickup(heroRect))
            {
                _speedBuffTimer = 5f;            // seconds
                _fruits.RemoveAt(i);
            }
        }
        if (_speedBuffTimer > 0)
        {
            _speedBuffTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            _currentMoveSpeed = BuffedSpeed;
        }
        else
        {
            _currentMoveSpeed = NormalSpeed;
        }

        if (_entranceMsgTimer > 0)
            _entranceMsgTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

        _cameraPosition = _heroPosition - new Vector2(320, 180); // center on hero in 640x360 space

        int mapWidth = _tiles.GetLength(1) * _TileSize;
        int mapHeight = _tiles.GetLength(0) * _TileSize;

        _cameraPosition.X = MathHelper.Clamp(_cameraPosition.X, 0, mapWidth - 640);
        _cameraPosition.Y = MathHelper.Clamp(_cameraPosition.Y, 0, mapHeight - 360);

        base.Update(gameTime);
    }

// Renders the entire world with a supplied view matrix (camera or minimap).
private void DrawWorld(SpriteBatch sb, Matrix transform, bool isMinimap)
{
    sb.Begin(transformMatrix: transform, samplerState: SamplerState.PointClamp);

    // --- Tiles ---
    for (int y = 0; y < _tiles.GetLength(0); y++)
    {
        for (int x = 0; x < _tiles.GetLength(1); x++)
        {
            sb.Draw(
                _tiles[y, x].Texture,
                new Rectangle(x * _TileSize, y * _TileSize, _TileSize, _TileSize),
                _tiles[y, x].SourceRect,
                Color.White);
        }
    }

    // --- Fruits ---
    if (!isMinimap)   // change to 'true' later if you want them visible in minimap
    {
        foreach (var fruit in _fruits)
            fruit.Draw(sb, _speedFruitTexture);
    }

    int destTopHeight    = (int)(CaveSplitOffsetY * CaveScale);
    int destBottomHeight = (int)((_caveBottomTexture.Height) * CaveScale);

    // --- Cave BACK (top slice) ---
    sb.Draw(
        _caveBottomTexture,
        destinationRectangle: new Rectangle(
            (int)_cavePosition.X,
            (int)_cavePosition.Y + destTopHeight,
            CaveDrawWidth,
            CaveDrawHeight - destTopHeight),
        sourceRectangle: null,
        color: Color.White);

    // --- Hero sprite ---
    Rectangle heroSourceRect = new Rectangle(_frame * _FrameWidth, (int)_currentDirection * _FrameHeight, _FrameWidth, _FrameHeight);
    SpriteEffects effects = SpriteEffects.None;
    if (_currentDirection == Direction.Left)
    {
        heroSourceRect = new Rectangle(_frame * _FrameWidth, (int)Direction.Right * _FrameHeight, _FrameWidth, _FrameHeight);
        effects = SpriteEffects.FlipHorizontally;
    }
    sb.Draw(_heroTexture, _heroPosition, heroSourceRect, Color.White, 0f, Vector2.Zero, 1f, effects, 0f);

    // --- Cave FRONT (bottom slice) ---
    sb.Draw(
        _caveTopTexture,
        destinationRectangle: new Rectangle(
            (int)_cavePosition.X,
            (int)_cavePosition.Y,
            CaveDrawWidth,
            destTopHeight),
        sourceRectangle: null,
        color: Color.White);

    if (!isMinimap)
    {
        // Visualize entrance (green) and collision (red, semi‑transparent)
        sb.Draw(_pixel, _caveCollisionRect, Color.Red * 0.25f);
        sb.Draw(_pixel, _caveEntranceRect, Color.Lime * 0.4f);
    }

    sb.End();
}

    protected override void Draw(GameTime gameTime)
    {
        int screenWidth = GraphicsDevice.Viewport.Width;
        Vector2 _minimapPosition = new Vector2(screenWidth - MinimapWidth - 10, 10);

        Matrix cameraTransform = Matrix.CreateTranslation(new Vector3(-_cameraPosition, 0));
        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(Color.CornflowerBlue);
        DrawWorld(_spriteBatch, cameraTransform, false);
        GraphicsDevice.SetRenderTarget(null);

        GraphicsDevice.SetRenderTarget(_minimapRenderTarget);
        GraphicsDevice.Clear(Color.Transparent);

        // Compute the top‑left world coordinate shown on the minimap
        int heroTileX = (int)(_heroPosition.X + _FrameWidth / 2) / _TileSize;
        int heroTileY = (int)(_heroPosition.Y + _FrameHeight / 2) / _TileSize;
        int startTileX = Math.Clamp(heroTileX - MinimapViewRadius, 0, _tiles.GetLength(1) - MinimapWindowTiles);
        int startTileY = Math.Clamp(heroTileY - MinimapViewRadius, 0, _tiles.GetLength(0) - MinimapWindowTiles);

        Vector2 minimapOrigin = new Vector2(startTileX * _TileSize, startTileY * _TileSize);
        float minimapScale = (float)MinimapTileSize / _TileSize;      // 3 / 32 ≈ 0.09375

        Matrix miniCam = Matrix.CreateTranslation(new Vector3(-minimapOrigin, 0)) *
                         Matrix.CreateScale(minimapScale);

        DrawWorld(_spriteBatch, miniCam, true);

        // Overlay a red pixel for the hero marker
        int heroMarkerX = (heroTileX - startTileX) * MinimapTileSize;
        int heroMarkerY = (heroTileY - startTileY) * MinimapTileSize;
        _spriteBatch.Begin();
        _spriteBatch.Draw(_pixel, new Rectangle(heroMarkerX, heroMarkerY, MinimapTileSize, MinimapTileSize), Color.Red);
        _spriteBatch.End();

        GraphicsDevice.SetRenderTarget(null);

        // Draw the render target scaled to fit the window
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(
            _renderTarget,
            destinationRectangle: GraphicsDevice.Viewport.Bounds,
            color: Color.White
        );

        Rectangle minimapScreenRect = new Rectangle((int)_minimapPosition.X, (int)_minimapPosition.Y, MinimapWidth, MinimapHeight);
        _spriteBatch.Draw(_minimapRenderTarget, minimapScreenRect, Color.White);

        // Draw white border around minimap
        int borderThickness = 1;
        Rectangle borderRect = minimapScreenRect;
        // Top
        _spriteBatch.Draw(_pixel, new Rectangle(borderRect.Left, borderRect.Top, borderRect.Width, borderThickness), Color.White);
        // Bottom
        _spriteBatch.Draw(_pixel, new Rectangle(borderRect.Left, borderRect.Bottom - borderThickness, borderRect.Width, borderThickness), Color.White);
        // Left
        _spriteBatch.Draw(_pixel, new Rectangle(borderRect.Left, borderRect.Top, borderThickness, borderRect.Height), Color.White);
        // Right
        _spriteBatch.Draw(_pixel, new Rectangle(borderRect.Right - borderThickness, borderRect.Top, borderThickness, borderRect.Height), Color.White);

        // Flash cave‑entrance message
        if (_entranceMsgTimer > 0)
        {
            string msg = "Cave entrance detected";
            Vector2 size = _font.MeasureString(msg);
            Vector2 pos  = new Vector2((GraphicsDevice.Viewport.Width - size.X) / 2, 20);
            _spriteBatch.DrawString(_font, msg, pos, Color.Yellow);
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}

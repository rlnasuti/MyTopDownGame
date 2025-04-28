using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;

namespace MyTopDownGame.Scenes;

public class OverworldScene : BaseScene
{
    private SpriteBatch _spriteBatch => _game.SpriteBatch;
    private Texture2D _heroTexture;
    private Texture2D _waterTexture;
    private Texture2D _grassTexture;
    private Texture2D _speedFruitTexture;
    private Texture2D _caveTopTexture;
    private Texture2D _caveBottomTexture;
    private Tile[,] _tiles;
    private List<AlienSpeedFruit> _fruits = new List<AlienSpeedFruit>();
    private Vector2 _heroPosition;
    private Vector2 _cameraPosition;
    private Vector2 _cavePosition;
    private Rectangle _caveRect;
    private Rectangle _caveEntranceRect;
    private Rectangle _caveCollisionRect;
    private const int _TileSize = 32;
    private const int _FrameWidth = 32;
    private const int _FrameHeight = 40;
    private const int FeetHeight = 8;
    private const int FeetMarginX = 8;
    private static readonly Rectangle WaterTileSourceRect = new Rectangle(40, 50, 950, 900);
    private static readonly Rectangle GrassTileSourceRect = new Rectangle(0, 0, 1024, 1024);
    private int _frame = 0;
    private float _timer = 0f;
    private float _animationSpeed = 0.2f;
    private float _currentMoveSpeed;
    private float _speedBuffTimer = 0f;
    private enum Direction { Down, Right, Up, Left }
    private Direction _currentDirection = Direction.Down;
    private KeyboardState _prevKeyboardState;
    private readonly Random _random = new Random(42);
    private const int CaveDrawWidth = 256;
    private const int CaveDrawHeight = 256;
    private const int CaveSplitOffsetY = 442;
    private const float CaveScale = 0.25f;

    public OverworldScene(Game1 game) : base(game)
    {
        // Scene-specific render targets
    }

    override public void LoadContent()
    {
        _heroTexture = Content.Load<Texture2D>("Sprites/hero");
        _speedFruitTexture = Content.Load<Texture2D>("Sprites/speed_fruit");
        _waterTexture = Content.Load<Texture2D>("Tiles/water_highres");
        _grassTexture = Content.Load<Texture2D>("Tiles/grass_highres");
        _caveTopTexture = Content.Load<Texture2D>("Objects/cave_top");
        _caveBottomTexture = Content.Load<Texture2D>("Objects/cave_bottom");

        InitializeMap();
    }

    private void InitializeMap()
    {
        _tiles = new Tile[100, 100];

        int centerX = 50;
        int centerY = 50;
        int islandRadius = 40;
        int lakeRadius = 8;

        for (int y = 0; y < 100; y++)
        {
            for (int x = 0; x < 100; x++)
            {
                double distance = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));
                bool isLand = distance < islandRadius && distance > lakeRadius;
                bool isWalkable = isLand;

                _tiles[y, x] = isLand
                    ? new Tile(_grassTexture, GrassTileSourceRect, isWalkable)
                    : new Tile(_waterTexture, WaterTileSourceRect, false);
            }
        }

        _cavePosition = new Vector2(50 * _TileSize, 15 * _TileSize);

        _caveRect = new Rectangle(
            (int)_cavePosition.X,
            (int)_cavePosition.Y,
            CaveDrawWidth,
            CaveDrawHeight);

        int entranceWidth = 36;
        int entranceHeight = 48;

        _caveEntranceRect = new Rectangle(
            _caveRect.X + (_caveRect.Width - entranceWidth) / 2,
            _caveRect.Bottom - entranceHeight - (2 * _TileSize),
            entranceWidth,
            entranceHeight);

        _caveCollisionRect = new Rectangle(
            _caveRect.X + (entranceWidth / 2),
            _caveRect.Bottom - (4 * _TileSize) - _TileSize,
            _caveRect.Width - entranceWidth,
            3 * _TileSize);

        int spawnTileX = 50;
        int spawnTileY = 32;
        _heroPosition = new Vector2(spawnTileX * _TileSize, spawnTileY * _TileSize);

        // --- Spawn 10 fruits but never on the hero's starting tile ---
        int fruitsToSpawn = 10;
        while (fruitsToSpawn > 0)
        {
            int x = _random.Next(0, 100);
            int y = _random.Next(0, 100);

            bool sameAsHeroStart = (x == spawnTileX && y == spawnTileY);

            if (_tiles[y, x].IsWalkable && !sameAsHeroStart)
            {
                _fruits.Add(new AlienSpeedFruit(new Vector2(x * _TileSize, y * _TileSize)));
                fruitsToSpawn--;
            }
        }

        _currentMoveSpeed = 100f; // NormalSpeed
    }

    override public void Update(GameTime gameTime)
    {
        KeyboardState state = Keyboard.GetState();

        HandleDebugToggle();

        Vector2 movement = GetMovementVector(state, out Direction intendedDir);
        _currentDirection = intendedDir;

        if (movement != Vector2.Zero)
        {
            _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_timer > _animationSpeed)
            {
                _frame = (_frame + 1) % 2;
                _timer = 0f;
            }

            Vector2 proposedPosition = _heroPosition + movement * _currentMoveSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
            Rectangle feetRect = new Rectangle(
                (int)proposedPosition.X + FeetMarginX,
                (int)proposedPosition.Y + _FrameHeight - FeetHeight,
                _FrameWidth - (FeetMarginX * 2),
                FeetHeight);

            // --- all 4 corners, just like original Game1 ---
            int left = (int)proposedPosition.X / _TileSize;
            int right = (int)(proposedPosition.X + _FrameWidth - 1) / _TileSize;
            int top = (int)proposedPosition.Y / _TileSize;
            int bottom = (int)(proposedPosition.Y + _FrameHeight - 1) / _TileSize;

            bool tileOk =
                _tiles[top, left].IsWalkable &&
                _tiles[top, right].IsWalkable &&
                _tiles[bottom, left].IsWalkable &&
                _tiles[bottom, right].IsWalkable;

            bool hitsEntrance = feetRect.Intersects(_caveEntranceRect);
            bool hitsCave = feetRect.Intersects(_caveCollisionRect) && !hitsEntrance;

            if (tileOk && !hitsCave)
                _heroPosition = proposedPosition;

            if (_caveEntranceRect.Contains(feetRect))
            {
                _game.SwitchScene(new CaveScene(_game, this));
                return;
            }
        }
        else
        {
            _frame = 0;
        }

        Rectangle heroRect = new Rectangle((int)_heroPosition.X, (int)_heroPosition.Y, _FrameWidth, _FrameHeight);
        for (int i = _fruits.Count - 1; i >= 0; i--)
        {
            if (_fruits[i].CheckPickup(heroRect))
            {
                _speedBuffTimer = 5f;
                _fruits.RemoveAt(i);
            }
        }
        if (_speedBuffTimer > 0)
        {
            _speedBuffTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            _currentMoveSpeed = 180f;
        }
        else
        {
            _currentMoveSpeed = 100f;
        }

        _cameraPosition = _heroPosition - new Vector2(320, 180);

        int mapWidth = _tiles.GetLength(1) * _TileSize;
        int mapHeight = _tiles.GetLength(0) * _TileSize;

        _cameraPosition.X = MathHelper.Clamp(_cameraPosition.X, 0, mapWidth - 640);
        _cameraPosition.Y = MathHelper.Clamp(_cameraPosition.Y, 0, mapHeight - 360);

        _prevKeyboardState = state;
    }

    override public void Draw(GameTime gameTime)
    {
        // ----- World / minimap draw copied from old Game1 -----
        int screenWidth = GraphicsDevice.Viewport.Width;
        Vector2 minimapPosition = new Vector2(screenWidth - MinimapWidth - 10, 10);

        Matrix cameraTransform = Matrix.CreateTranslation(new Vector3(-_cameraPosition, 0));

        // --- Main world into _renderTarget ---
        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(Color.CornflowerBlue);
        DrawWorld(_spriteBatch, cameraTransform, false);
        GraphicsDevice.SetRenderTarget(null);

        // --- Minimap into _minimapRenderTarget ---
        GraphicsDevice.SetRenderTarget(_minimapRenderTarget);
        GraphicsDevice.Clear(Color.Transparent);

        int heroTileX = (int)(_heroPosition.X + _FrameWidth / 2) / _TileSize;
        int heroTileY = (int)(_heroPosition.Y + _FrameHeight / 2) / _TileSize;
        int startTileX = Math.Clamp(heroTileX - MinimapViewRadius, 0, _tiles.GetLength(1) - MinimapWindowTiles);
        int startTileY = Math.Clamp(heroTileY - MinimapViewRadius, 0, _tiles.GetLength(0) - MinimapWindowTiles);

        Vector2 minimapOrigin = new Vector2(startTileX * _TileSize, startTileY * _TileSize);
        float minimapScale = (float)MinimapTileSize / _TileSize;

        Matrix miniCam = Matrix.CreateTranslation(new Vector3(-minimapOrigin, 0))
                         * Matrix.CreateScale(minimapScale);

        DrawWorld(_spriteBatch, miniCam, true);

        // red hero marker
        int heroMarkerX = (heroTileX - startTileX) * MinimapTileSize;
        int heroMarkerY = (heroTileY - startTileY) * MinimapTileSize;
        _spriteBatch.Begin();
        _spriteBatch.Draw(_game.Pixel, new Rectangle(heroMarkerX, heroMarkerY, MinimapTileSize, MinimapTileSize), Color.Red);
        _spriteBatch.End();

        GraphicsDevice.SetRenderTarget(null);

        // ----- Present to screen -----
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(_renderTarget,
                          destinationRectangle: GraphicsDevice.Viewport.Bounds,
                          color: Color.White);

        Rectangle minimapScreenRect = new Rectangle((int)minimapPosition.X, (int)minimapPosition.Y,
                                                    MinimapWidth, MinimapHeight);
        _spriteBatch.Draw(_minimapRenderTarget, minimapScreenRect, Color.White);

        // white border around minimap
        int border = 1;
        _spriteBatch.Draw(_game.Pixel, new Rectangle(minimapScreenRect.Left, minimapScreenRect.Top, minimapScreenRect.Width, border), Color.White);
        _spriteBatch.Draw(_game.Pixel, new Rectangle(minimapScreenRect.Left, minimapScreenRect.Bottom - border, minimapScreenRect.Width, border), Color.White);
        _spriteBatch.Draw(_game.Pixel, new Rectangle(minimapScreenRect.Left, minimapScreenRect.Top, border, minimapScreenRect.Height), Color.White);
        _spriteBatch.Draw(_game.Pixel, new Rectangle(minimapScreenRect.Right - border, minimapScreenRect.Top, border, minimapScreenRect.Height), Color.White);
        _spriteBatch.End();
    }

    private void DrawWorld(SpriteBatch sb, Matrix transform, bool isMinimap)
    {
        sb.Begin(transformMatrix: transform, samplerState: SamplerState.PointClamp);

        // Tiles
        for (int y = 0; y < _tiles.GetLength(0); y++)
        {
            for (int x = 0; x < _tiles.GetLength(1); x++)
            {
                sb.Draw(_tiles[y, x].Texture,
                        new Rectangle
                        (
                            x * _TileSize,
                            y * _TileSize,
                            _TileSize,
                            _TileSize),
                            _tiles[y, x].SourceRect,
                            Color.White
                        );

                if (DebugOverlayEnabled && !isMinimap)
                    DrawBoundingBox(sb, new Rectangle(x * _TileSize, y * _TileSize, _TileSize, _TileSize), Color.Yellow * 0.3f);
            }
        }

        // Fruits (not on minimap)
        if (!isMinimap)
            foreach (var fruit in _fruits)
                fruit.Draw(sb, _speedFruitTexture);

        int destTopHeight = (int)(CaveSplitOffsetY * CaveScale);

        // Cave front (bottom slice)
        sb.Draw(_caveBottomTexture,
                new Rectangle((int)_cavePosition.X,
                              (int)_cavePosition.Y + destTopHeight,
                              CaveDrawWidth,
                              CaveDrawHeight - destTopHeight),
                null, Color.White);

        // Hero
        Rectangle heroSource = new Rectangle(_frame * _FrameWidth, (int)_currentDirection * _FrameHeight,
                                             _FrameWidth, _FrameHeight);
        SpriteEffects fx = SpriteEffects.None;
        if (_currentDirection == Direction.Left)
        {
            heroSource = new Rectangle(_frame * _FrameWidth, (int)Direction.Right * _FrameHeight, _FrameWidth, _FrameHeight);
            fx = SpriteEffects.FlipHorizontally;
        }
        sb.Draw(_heroTexture, _heroPosition, heroSource, Color.White, 0f, Vector2.Zero, 1f, fx, 0f);

        // Cave back (top slice)
        sb.Draw(_caveTopTexture,
                new Rectangle((int)_cavePosition.X,
                              (int)_cavePosition.Y,
                              CaveDrawWidth,
                              destTopHeight),
                null, Color.White);

        // Debug overlay
        if (DebugOverlayEnabled && !isMinimap)
        {
            DrawBoundingBox(sb, new Rectangle((int)_heroPosition.X, (int)_heroPosition.Y, _FrameWidth, _FrameHeight), Color.Yellow);

            Rectangle feet = new Rectangle((int)_heroPosition.X + FeetMarginX,
                                           (int)_heroPosition.Y + _FrameHeight - FeetHeight,
                                           _FrameWidth - (FeetMarginX * 2),
                                           FeetHeight);
            DrawBoundingBox(sb, feet, Color.Red);

            foreach (var fruit in _fruits)
                DrawBoundingBox(sb, new Rectangle((int)fruit.Position.X, (int)fruit.Position.Y, 32, 32), Color.Yellow);

            DrawBoundingBox(sb, _caveCollisionRect, Color.Yellow);
            DrawBoundingBox(sb, _caveEntranceRect, Color.Yellow);
        }

        sb.End();
    }

    private Vector2 GetMovementVector(KeyboardState state, out Direction newDir)
    {
        Vector2 movement = Vector2.Zero;
        newDir = _currentDirection;

        if (state.IsKeyDown(Keys.Down)) { movement.Y += 1; newDir = Direction.Down; }
        if (state.IsKeyDown(Keys.Up)) { movement.Y -= 1; newDir = Direction.Up; }
        if (state.IsKeyDown(Keys.Left)) { movement.X -= 1; newDir = Direction.Left; }
        if (state.IsKeyDown(Keys.Right)) { movement.X += 1; newDir = Direction.Right; }

        return movement;
    }
}
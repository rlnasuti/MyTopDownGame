using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class AlienSpeedFruit
{
    public Vector2 Position;

    public AlienSpeedFruit(Vector2 position)
    {
        Position = position;
    }

    public bool CheckPickup(Rectangle heroRect)
    {
        Rectangle fruitRect = new Rectangle((int)Position.X, (int)Position.Y, 32, 32);
        return heroRect.Intersects(fruitRect);
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D texture)
    {
        int drawSize = 24; // 3/4 of 32
        Vector2 drawPosition = new Vector2(
            Position.X + (32 - drawSize) / 2,
            Position.Y + (32 - drawSize) / 2);
        Rectangle destRect = new Rectangle((int)drawPosition.X, (int)drawPosition.Y, drawSize, drawSize);
        spriteBatch.Draw(texture, destRect, Color.White);
    }
}

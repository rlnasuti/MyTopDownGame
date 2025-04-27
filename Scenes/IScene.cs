using Microsoft.Xna.Framework;

namespace MyTopDownGame.Scenes;

public interface IScene
{
    void LoadContent();
    void Update(GameTime gameTime);
    void Draw(GameTime gameTime);
}
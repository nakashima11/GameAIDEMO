using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameAIDemo.AI
{
    public abstract class BaseAI
    {
        protected Game game;
        protected SpriteBatch spriteBatch;
        
        public BaseAI(Game game)
        {
            this.game = game;
            this.spriteBatch = game.Services.GetService<SpriteBatch>();
        }
        
        public abstract void Update(GameTime gameTime);
        
        public abstract void Draw(GameTime gameTime);
    }
} 
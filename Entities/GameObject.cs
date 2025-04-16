using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameAIDemo.Entities
{
    public abstract class GameObject
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Rotation { get; set; }
        public Color Color { get; set; }
        public float Radius { get; set; }
        public bool IsActive { get; set; } = true;

        public GameObject(Vector2 position, float radius, Color color)
        {
            Position = position;
            Radius = radius;
            Color = color;
            Velocity = Vector2.Zero;
            Rotation = 0f;
        }

        public virtual void Update(GameTime gameTime)
        {
            // 基本的な更新ロジック
            Position += Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        public virtual void Draw(SpriteBatch spriteBatch, Texture2D texture)
        {
            if (!IsActive) return;

            spriteBatch.Draw(
                texture,
                Position,
                null,
                Color,
                Rotation,
                new Vector2(texture.Width / 2, texture.Height / 2),
                Radius * 2 / texture.Width,
                SpriteEffects.None,
                0f
            );
        }

        public bool Intersects(GameObject other)
        {
            return Vector2.Distance(Position, other.Position) < (Radius + other.Radius);
        }
    }
} 
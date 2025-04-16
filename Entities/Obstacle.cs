using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameAIDemo.Entities
{
    public class Obstacle
    {
        public Rectangle Bounds { get; private set; }
        public Color Color { get; private set; }
        public float Rotation { get; private set; }

        public Obstacle(Rectangle bounds, Color color)
        {
            Bounds = bounds;
            Color = color;
            Rotation = 0f;
        }

        public Obstacle(Rectangle bounds, Color color, float rotation)
        {
            Bounds = bounds;
            Color = color;
            Rotation = rotation;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D texture)
        {
            if (Rotation != 0f)
            {
                Vector2 origin = new Vector2(0, Bounds.Height / 2);
                spriteBatch.Draw(
                    texture, 
                    new Rectangle(Bounds.X, Bounds.Y + Bounds.Height / 2, Bounds.Width, Bounds.Height),
                    null,
                    Color,
                    Rotation,
                    origin,
                    SpriteEffects.None,
                    0f
                );
            }
            else
            {
                spriteBatch.Draw(texture, Bounds, Color);
            }
        }

        public bool Intersects(GameObject entity)
        {
            if (Rotation != 0f)
            {
                Vector2 center = new Vector2(
                    Bounds.X + Bounds.Width / 2,
                    Bounds.Y + Bounds.Height / 2
                );
                float radius = MathHelper.Max(Bounds.Width, Bounds.Height) / 2;
                
                return Vector2.Distance(entity.Position, center) < (entity.Radius + radius);
            }
            else
            {
                Vector2 closestPoint = new Vector2(
                    MathHelper.Clamp(entity.Position.X, Bounds.Left, Bounds.Right),
                    MathHelper.Clamp(entity.Position.Y, Bounds.Top, Bounds.Bottom)
                );

                return Vector2.Distance(entity.Position, closestPoint) < entity.Radius;
            }
        }
    }
} 
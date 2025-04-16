using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameAIDemo.Entities;

namespace GameAIDemo.Utilities
{
    public class GridMap
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int CellSize { get; private set; }
        public bool[,] Cells { get; private set; }
        
        private List<Obstacle> _obstacles;

        public GridMap(int width, int height, int cellSize, List<Obstacle> obstacles)
        {
            Width = width / cellSize;
            Height = height / cellSize;
            CellSize = cellSize;
            _obstacles = obstacles;
            
            // グリッドセルを初期化
            Cells = new bool[Width, Height];
            UpdateGrid();
        }

        public void UpdateGrid()
        {
            // すべてのセルをクリア
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Cells[x, y] = false;
                }
            }

            // 障害物のあるセルに印をつける
            foreach (var obstacle in _obstacles)
            {
                int minX = Math.Max(0, obstacle.Bounds.Left / CellSize);
                int minY = Math.Max(0, obstacle.Bounds.Top / CellSize);
                int maxX = Math.Min(Width - 1, obstacle.Bounds.Right / CellSize);
                int maxY = Math.Min(Height - 1, obstacle.Bounds.Bottom / CellSize);

                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        Cells[x, y] = true;
                    }
                }
            }
        }

        public List<Vector2> GetNeighbors(Vector2 position)
        {
            List<Vector2> neighbors = new List<Vector2>();
            int gridX = (int)(position.X / CellSize);
            int gridY = (int)(position.Y / CellSize);

            // 8方向の隣接セルをチェック
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue; // 自分自身はスキップ

                    int nx = gridX + dx;
                    int ny = gridY + dy;

                    // グリッド範囲内かつ障害物がない場合、隣接セルとして追加
                    if (nx >= 0 && nx < Width && ny >= 0 && ny < Height && !Cells[nx, ny])
                    {
                        neighbors.Add(new Vector2(nx * CellSize + CellSize / 2, ny * CellSize + CellSize / 2));
                    }
                }
            }

            return neighbors;
        }

        public bool IsWalkable(Vector2 position)
        {
            int gridX = (int)(position.X / CellSize);
            int gridY = (int)(position.Y / CellSize);

            // グリッド範囲外または障害物がある場合は歩行不可
            if (gridX < 0 || gridX >= Width || gridY < 0 || gridY >= Height || Cells[gridX, gridY])
            {
                return false;
            }

            return true;
        }

        public Vector2 WorldToGrid(Vector2 worldPosition)
        {
            return new Vector2(
                (int)(worldPosition.X / CellSize),
                (int)(worldPosition.Y / CellSize)
            );
        }

        public Vector2 GridToWorld(Vector2 gridPosition)
        {
            return new Vector2(
                gridPosition.X * CellSize + CellSize / 2,
                gridPosition.Y * CellSize + CellSize / 2
            );
        }

        public void DrawGrid(SpriteBatch spriteBatch, Texture2D texture)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (Cells[x, y])
                    {
                        // 障害物セルを描画
                        Rectangle rect = new Rectangle(x * CellSize, y * CellSize, CellSize, CellSize);
                        spriteBatch.Draw(texture, rect, Color.Gray * 0.5f);
                    }
                }
            }
        }
    }
} 
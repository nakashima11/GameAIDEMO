using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GameAIDemo.Utilities;

namespace GameAIDemo.AI.AStar
{
    public class AStarNode
    {
        public Vector2 Position { get; set; }
        public float GCost { get; set; } // 開始点からのコスト
        public float HCost { get; set; } // 目標点までの推定コスト
        public float FCost => GCost + HCost; // 合計コスト
        public AStarNode Parent { get; set; }

        public AStarNode(Vector2 position)
        {
            Position = position;
        }
    }

    public class AStarPathfinder
    {
        private GridMap _gridMap;

        public AStarPathfinder(GridMap gridMap)
        {
            _gridMap = gridMap;
        }

        public List<Vector2> FindPath(Vector2 startPos, Vector2 endPos)
        {
            // グリッド上の位置に変換
            Vector2 startGrid = _gridMap.WorldToGrid(startPos);
            Vector2 endGrid = _gridMap.WorldToGrid(endPos);

            // 開始点と終了点が同じ場合
            if (startGrid == endGrid)
            {
                return new List<Vector2> { startPos, endPos };
            }

            // 開始点または終了点が障害物内にある場合
            if (!_gridMap.IsWalkable(startPos) || !_gridMap.IsWalkable(endPos))
            {
                return new List<Vector2>();
            }

            // オープンリストとクローズドリストを初期化
            List<AStarNode> openList = new List<AStarNode>();
            HashSet<Vector2> closedList = new HashSet<Vector2>();
            Dictionary<Vector2, AStarNode> allNodes = new Dictionary<Vector2, AStarNode>();

            // 開始ノードを作成
            AStarNode startNode = new AStarNode(_gridMap.GridToWorld(startGrid));
            startNode.GCost = 0;
            startNode.HCost = CalculateHeuristic(startGrid, endGrid);
            openList.Add(startNode);
            allNodes[startGrid] = startNode;

            while (openList.Count > 0)
            {
                // F値が最小のノードを見つける
                AStarNode currentNode = openList[0];
                for (int i = 1; i < openList.Count; i++)
                {
                    if (openList[i].FCost < currentNode.FCost || 
                        (openList[i].FCost == currentNode.FCost && openList[i].HCost < currentNode.HCost))
                    {
                        currentNode = openList[i];
                    }
                }

                // 現在のノードをオープンリストから削除し、クローズドリストに追加
                openList.Remove(currentNode);
                Vector2 currentGrid = _gridMap.WorldToGrid(currentNode.Position);
                closedList.Add(currentGrid);

                // 目標に到達した場合
                if (currentGrid == endGrid)
                {
                    return ReconstructPath(currentNode);
                }

                // 隣接ノードを処理
                foreach (Vector2 neighborPos in _gridMap.GetNeighbors(currentNode.Position))
                {
                    Vector2 neighborGrid = _gridMap.WorldToGrid(neighborPos);

                    // すでに処理済みのノードはスキップ
                    if (closedList.Contains(neighborGrid))
                        continue;

                    // 新しいノードを作成または取得
                    AStarNode neighborNode;
                    if (!allNodes.TryGetValue(neighborGrid, out neighborNode))
                    {
                        neighborNode = new AStarNode(neighborPos);
                        allNodes[neighborGrid] = neighborNode;
                    }

                    // G値を計算 (現在ノードのG値 + 現在ノードから隣接ノードへの距離)
                    float tentativeGCost = currentNode.GCost + Vector2.Distance(currentNode.Position, neighborPos);

                    // より良い経路が見つかった場合、または新しいノードの場合
                    if (!openList.Contains(neighborNode) || tentativeGCost < neighborNode.GCost)
                    {
                        neighborNode.Parent = currentNode;
                        neighborNode.GCost = tentativeGCost;
                        neighborNode.HCost = CalculateHeuristic(neighborGrid, endGrid);

                        if (!openList.Contains(neighborNode))
                        {
                            openList.Add(neighborNode);
                        }
                    }
                }
            }

            // 経路が見つからなかった場合
            return new List<Vector2>();
        }

        private float CalculateHeuristic(Vector2 start, Vector2 end)
        {
            // ユークリッド距離をヒューリスティックとして使用
            return Vector2.Distance(start, end);
        }

        private List<Vector2> ReconstructPath(AStarNode targetNode)
        {
            List<Vector2> path = new List<Vector2>();
            AStarNode currentNode = targetNode;

            while (currentNode != null)
            {
                path.Add(currentNode.Position);
                currentNode = currentNode.Parent;
            }

            path.Reverse(); // 開始点から目標点への順序に並び替え
            return path;
        }
    }
} 
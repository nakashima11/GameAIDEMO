using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameAIDemo.Entities;
using GameAIDemo.Utilities;

namespace GameAIDemo.AI.AStar
{
    public enum AStarAgentState
    {
        Patrol, // 定期的に新しい巡回地点を選択
        Chase,  // プレイヤーを追跡
        Attack  // プレイヤーに十分近づいたら攻撃
    }

    public class AStarAgent : GameObject
    {
        private const float AGENT_SPEED = 150f;
        private const float DETECTION_RADIUS = 200f;
        private const float ATTACK_RADIUS = 50f;
        private const float PATH_NODE_RADIUS = 5f;
        private const float WAYPOINT_THRESHOLD = 15f;

        private AStarPathfinder _pathfinder;
        private GridMap _gridMap;
        private Player _player;
        private AStarAgentState _currentState;
        private List<Vector2> _currentPath;
        private int _currentPathIndex;
        private Vector2 _patrolTarget;
        private Random _random;
        private float _stateTimer;

        public AStarAgentState CurrentState 
        { 
            get => _currentState; 
            private set
            {
                if (_currentState != value)
                {
                    _currentState = value;
                    OnStateChanged();
                }
            }
        }

        public AStarAgent(Vector2 position, GridMap gridMap, Player player) 
            : base(position, 15f, Color.Red)
        {
            _gridMap = gridMap;
            _pathfinder = new AStarPathfinder(gridMap);
            _player = player;
            _currentState = AStarAgentState.Patrol;
            _currentPath = new List<Vector2>();
            _currentPathIndex = 0;
            _random = new Random();
            _stateTimer = 0f;
            
            // 最初の巡回地点を設定
            ChooseRandomPatrolTarget();
        }

        public override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _stateTimer += deltaTime;

            // プレイヤーまでの距離を計算
            float distanceToPlayer = Vector2.Distance(Position, _player.Position);

            // 状態管理
            switch (CurrentState)
            {
                case AStarAgentState.Patrol:
                    // 定期的に新しい巡回地点を選択（5秒ごと）
                    if (_stateTimer > 5f)
                    {
                        ChooseRandomPatrolTarget();
                        _stateTimer = 0f;
                    }

                    // プレイヤーが検出範囲内に入ったら追跡状態に移行
                    if (distanceToPlayer < DETECTION_RADIUS)
                    {
                        CurrentState = AStarAgentState.Chase;
                    }
                    
                    // 巡回地点に向かって移動
                    FollowPath();
                    break;

                case AStarAgentState.Chase:
                    // プレイヤーが検出範囲外に出たら巡回状態に戻る
                    if (distanceToPlayer > DETECTION_RADIUS)
                    {
                        CurrentState = AStarAgentState.Patrol;
                        ChooseRandomPatrolTarget();
                    }
                    // プレイヤーが攻撃範囲内に入ったら攻撃状態に移行
                    else if (distanceToPlayer < ATTACK_RADIUS)
                    {
                        CurrentState = AStarAgentState.Attack;
                    }
                    
                    // 定期的にプレイヤーへの経路を再計算（0.5秒ごと）
                    if (_stateTimer > 0.5f)
                    {
                        _currentPath = _pathfinder.FindPath(Position, _player.Position);
                        _currentPathIndex = 0;
                        _stateTimer = 0f;
                    }
                    
                    // プレイヤーに向かって移動
                    FollowPath();
                    break;

                case AStarAgentState.Attack:
                    // プレイヤーが攻撃範囲外に出たら追跡状態に戻る
                    if (distanceToPlayer > ATTACK_RADIUS)
                    {
                        CurrentState = AStarAgentState.Chase;
                    }
                    
                    // 攻撃中は静止
                    Velocity = Vector2.Zero;
                    
                    // プレイヤーの方向を向く
                    Vector2 directionToPlayer = _player.Position - Position;
                    if (directionToPlayer != Vector2.Zero)
                    {
                        directionToPlayer.Normalize();
                        Rotation = (float)Math.Atan2(directionToPlayer.Y, directionToPlayer.X);
                    }
                    break;
            }

            base.Update(gameTime);
        }

        private void FollowPath()
        {
            if (_currentPath.Count == 0 || _currentPathIndex >= _currentPath.Count)
            {
                Velocity = Vector2.Zero;
                return;
            }

            // 次の経路ポイントへの方向を計算
            Vector2 targetPosition = _currentPath[_currentPathIndex];
            Vector2 direction = targetPosition - Position;
            float distanceToTarget = direction.Length();

            // ポイントに十分近づいたら次のポイントへ
            if (distanceToTarget < WAYPOINT_THRESHOLD)
            {
                _currentPathIndex++;
                if (_currentPathIndex >= _currentPath.Count)
                {
                    Velocity = Vector2.Zero;
                    return;
                }
                targetPosition = _currentPath[_currentPathIndex];
                direction = targetPosition - Position;
                distanceToTarget = direction.Length();
            }

            // 方向を正規化して移動
            if (distanceToTarget > 0)
            {
                direction /= distanceToTarget;
                Velocity = direction * AGENT_SPEED;
                Rotation = (float)Math.Atan2(direction.Y, direction.X);
            }
            else
            {
                Velocity = Vector2.Zero;
            }
        }

        private void ChooseRandomPatrolTarget()
        {
            const int MAX_ATTEMPTS = 10;
            int attempts = 0;
            Vector2 target;

            do
            {
                // ランダムな地点を選択
                int x = _random.Next(0, _gridMap.Width);
                int y = _random.Next(0, _gridMap.Height);
                target = _gridMap.GridToWorld(new Vector2(x, y));
                attempts++;
            } 
            while (!_gridMap.IsWalkable(target) && attempts < MAX_ATTEMPTS);

            // 経路を計算
            _patrolTarget = target;
            _currentPath = _pathfinder.FindPath(Position, _patrolTarget);
            _currentPathIndex = 0;
        }

        private void OnStateChanged()
        {
            _stateTimer = 0f;
            
            // 状態に応じて色を変更
            switch (CurrentState)
            {
                case AStarAgentState.Patrol:
                    Color = Color.Red;
                    break;
                case AStarAgentState.Chase:
                    Color = Color.Purple;
                    break;
                case AStarAgentState.Attack:
                    Color = Color.OrangeRed;
                    break;
            }
        }

        public override void Draw(SpriteBatch spriteBatch, Texture2D texture)
        {
            base.Draw(spriteBatch, texture);

            // パスを描画
            if (_currentPath.Count > 0)
            {
                for (int i = 0; i < _currentPath.Count - 1; i++)
                {
                    DrawLine(spriteBatch, texture, _currentPath[i], _currentPath[i + 1], Color.Green);
                }
            }

            // センサー範囲を描画
            DrawCircle(spriteBatch, texture, Position, DETECTION_RADIUS, Color.Yellow * 0.3f);
            DrawCircle(spriteBatch, texture, Position, ATTACK_RADIUS, Color.Red * 0.3f);
        }

        private void DrawLine(SpriteBatch spriteBatch, Texture2D texture, Vector2 start, Vector2 end, Color color)
        {
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);
            spriteBatch.Draw(
                texture,
                start,
                null,
                color,
                angle,
                Vector2.Zero,
                new Vector2(edge.Length() / texture.Width, 2f / texture.Height),
                SpriteEffects.None,
                0f
            );
        }

        private void DrawCircle(SpriteBatch spriteBatch, Texture2D texture, Vector2 center, float radius, Color color)
        {
            spriteBatch.Draw(
                texture,
                center,
                null,
                color,
                0f,
                new Vector2(texture.Width / 2, texture.Height / 2),
                radius * 2 / texture.Width,
                SpriteEffects.None,
                0f
            );
        }
    }
} 
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameAIDemo.Entities;
using GameAIDemo.Utilities;

namespace GameAIDemo.AI.BehaviorTree
{
    public enum BTAgentState
    {
        Idle,       // 待機状態
        Wander,     // うろつき状態
        Pursue,     // 追跡状態
        Attack,     // 攻撃状態
        Flee        // 逃走状態
    }

    public class BTAgent : GameObject
    {
        private const float AGENT_SPEED = 150f;
        private const float WANDER_SPEED = 80f;
        private const float DETECTION_RADIUS = 180f;
        private const float ATTACK_RADIUS = 50f;
        private const float LOW_HEALTH_THRESHOLD = 0.3f; // 30%以下で逃走を考慮
        
        private GridMap _gridMap;
        private Player _player;
        private List<Obstacle> _obstacles;
        private BTNode _rootNode;
        private Random _random;
        private Vector2 _targetPosition;
        private Vector2 _wanderTarget;
        private float _stateTimer;
        private float _health = 1.0f; // 体力（0.0〜1.0）
        
        public BTAgentState CurrentState { get; private set; }
        // 障害物を無視するかどうかのフラグ
        public bool IgnoreObstacles { get; private set; } = false;

        public BTAgent(Vector2 position, GridMap gridMap, Player player, List<Obstacle> obstacles) 
            : base(position, 15f, Color.Orange)
        {
            _gridMap = gridMap;
            _player = player;
            _obstacles = obstacles;
            _random = new Random();
            CurrentState = BTAgentState.Idle;
            _stateTimer = 0f;
            
            // ビヘイビアツリーの構築
            BuildBehaviorTree();
        }

        private void BuildBehaviorTree()
        {
            // トップレベルのセレクターノード
            var rootSelector = new BTSelectorNode("RootSelector");
            
            // 逃走行動（体力が低い場合）
            var fleeSequence = new BTSequenceNode("FleeSequence");
            fleeSequence.AddChild(new BTConditionNode("IsLowHealth", IsLowHealth));
            fleeSequence.AddChild(new BTActionNode("Flee", DoFlee));
            
            // 攻撃行動（プレイヤーが攻撃範囲内）
            var attackSequence = new BTSequenceNode("AttackSequence");
            attackSequence.AddChild(new BTConditionNode("IsPlayerInAttackRange", IsPlayerInAttackRange));
            attackSequence.AddChild(new BTActionNode("Attack", DoAttack));
            
            // 追跡行動（プレイヤーが検出範囲内）
            var pursueSequence = new BTSequenceNode("PursueSequence");
            pursueSequence.AddChild(new BTConditionNode("IsPlayerDetected", IsPlayerDetected));
            pursueSequence.AddChild(new BTActionNode("Pursue", DoPursue));
            
            // うろつき行動（デフォルト）
            var wanderSequence = new BTSequenceNode("WanderSequence");
            wanderSequence.AddChild(new BTActionNode("Wander", DoWander));
            
            // 優先順位順にツリーに追加
            rootSelector.AddChild(fleeSequence);      // 最高優先度：逃走
            rootSelector.AddChild(attackSequence);    // 次の優先度：攻撃
            rootSelector.AddChild(pursueSequence);    // 次の優先度：追跡
            rootSelector.AddChild(wanderSequence);    // 最低優先度：うろつき
            
            _rootNode = rootSelector;
        }

        public override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _stateTimer += deltaTime;
            
            // ビヘイビアツリーの実行
            _rootNode.Execute();
            
            // 位置を更新（障害物を無視するかどうかで処理を分岐）
            if (!IgnoreObstacles)
            {
                // 障害物を考慮して移動
                UpdatePositionWithObstacles(deltaTime);
            }
            else
            {
                // 障害物を無視して直線的に移動
                Position += Velocity * deltaTime;
            }
            
            // 体力の自然回復（テスト用）
            if (_health < 1.0f)
            {
                _health += 0.02f * deltaTime;
                _health = MathHelper.Clamp(_health, 0f, 1.0f);
            }
        }

        #region Condition Methods
        private bool IsLowHealth()
        {
            return _health <= LOW_HEALTH_THRESHOLD;
        }
        
        private bool IsPlayerDetected()
        {
            float distanceToPlayer = Vector2.Distance(Position, _player.Position);
            return distanceToPlayer <= DETECTION_RADIUS;
        }
        
        private bool IsPlayerInAttackRange()
        {
            float distanceToPlayer = Vector2.Distance(Position, _player.Position);
            return distanceToPlayer <= ATTACK_RADIUS;
        }
        #endregion

        #region Action Methods
        private BTNodeStatus DoIdle()
        {
            if (CurrentState != BTAgentState.Idle)
            {
                CurrentState = BTAgentState.Idle;
                Color = Color.Orange;
                Velocity = Vector2.Zero;
                // 待機状態では障害物を考慮する
                IgnoreObstacles = false;
            }
            
            // 一定時間後にうろつき状態に移行
            if (_stateTimer > 2f)
            {
                _stateTimer = 0f;
                return BTNodeStatus.Failure; // 失敗を返して次のノードに移行
            }
            
            return BTNodeStatus.Running;
        }
        
        private BTNodeStatus DoWander()
        {
            if (CurrentState != BTAgentState.Wander)
            {
                CurrentState = BTAgentState.Wander;
                Color = Color.Orange;
                
                // うろつき状態では障害物を考慮する
                IgnoreObstacles = false;
                
                // 新しいうろつき目標を設定
                ChooseRandomWanderTarget();
                _stateTimer = 0f;
            }
            
            // 一定時間ごとに新しい目標を設定
            if (_stateTimer > 3f)
            {
                ChooseRandomWanderTarget();
                _stateTimer = 0f;
            }
            
            // 目標に向かって移動
            Vector2 direction = _wanderTarget - Position;
            float distance = direction.Length();
            
            // 目標に到達したらランダムに新しい目標を設定
            if (distance < 20f)
            {
                ChooseRandomWanderTarget();
            }
            else
            {
                // 目標方向に移動
                direction.Normalize();
                Velocity = direction * WANDER_SPEED;
                Rotation = (float)Math.Atan2(direction.Y, direction.X);
            }
            
            return BTNodeStatus.Success; // 常に成功を返す
        }
        
        private BTNodeStatus DoPursue()
        {
            if (CurrentState != BTAgentState.Pursue)
            {
                CurrentState = BTAgentState.Pursue;
                Color = Color.DarkOrange;
                _stateTimer = 0f;
                // 追跡状態では障害物を無視する
                IgnoreObstacles = true;
            }
            
            // プレイヤーを追跡
            Vector2 direction = _player.Position - Position;
            float distance = direction.Length();
            
            // 方向を正規化して移動
            direction.Normalize();
            Velocity = direction * AGENT_SPEED;
            Rotation = (float)Math.Atan2(direction.Y, direction.X);
            
            return BTNodeStatus.Success;
        }
        
        private BTNodeStatus DoAttack()
        {
            if (CurrentState != BTAgentState.Attack)
            {
                CurrentState = BTAgentState.Attack;
                Color = Color.Red;
                _stateTimer = 0f;
                // 攻撃状態でも障害物を無視し続ける
                IgnoreObstacles = true;
            }
            
            // 攻撃中は停止
            Velocity = Vector2.Zero;
            
            // プレイヤーの方向を向く
            Vector2 direction = _player.Position - Position;
            if (direction != Vector2.Zero)
            {
                direction.Normalize();
                Rotation = (float)Math.Atan2(direction.Y, direction.X);
            }
            
            // プレイヤーに定期的にダメージを与える（仮想的な実装）
            // 実際のゲームでは、ここでプレイヤーにダメージを与えるロジックを実装
            
            return BTNodeStatus.Success;
        }
        
        private BTNodeStatus DoFlee()
        {
            if (CurrentState != BTAgentState.Flee)
            {
                CurrentState = BTAgentState.Flee;
                Color = Color.Yellow;
                _stateTimer = 0f;
                // 逃走状態では障害物を考慮する
                IgnoreObstacles = false;
            }
            
            // プレイヤーから離れる方向を計算
            Vector2 direction = Position - _player.Position;
            float distance = direction.Length();
            
            // 十分に離れたら、回復モードに入る
            if (distance > DETECTION_RADIUS * 1.5f)
            {
                // 隠れて休む（うろつきと同様の動きをするが、速度は遅め）
                if (CurrentState != BTAgentState.Idle)
                {
                    CurrentState = BTAgentState.Idle;
                    Velocity = Vector2.Zero;
                }
                return BTNodeStatus.Success;
            }
            
            // 方向を正規化して移動
            if (direction != Vector2.Zero)
            {
                direction.Normalize();
                Velocity = direction * AGENT_SPEED * 1.2f; // 逃走時は少し速く
                Rotation = (float)Math.Atan2(direction.Y, direction.X);
            }
            
            return BTNodeStatus.Running;
        }
        #endregion

        private void ChooseRandomWanderTarget()
        {
            const int MAX_ATTEMPTS = 10;
            int attempts = 0;
            Vector2 target;

            do
            {
                // ランダムな点を現在位置から一定範囲内で選択
                float angle = (float)_random.NextDouble() * MathHelper.TwoPi;
                float distance = (float)_random.NextDouble() * 200f + 50f;
                target = Position + new Vector2(
                    (float)Math.Cos(angle) * distance, 
                    (float)Math.Sin(angle) * distance
                );
                
                // 範囲内に収める
                target.X = MathHelper.Clamp(target.X, 50, _gridMap.Width * _gridMap.CellSize - 50);
                target.Y = MathHelper.Clamp(target.Y, 50, _gridMap.Height * _gridMap.CellSize - 50);
                
                attempts++;
            } 
            while (!_gridMap.IsWalkable(target) && attempts < MAX_ATTEMPTS);

            _wanderTarget = target;
        }

        public override void Draw(SpriteBatch spriteBatch, Texture2D texture)
        {
            base.Draw(spriteBatch, texture);
            
            // センサー範囲を描画
            DrawCircle(spriteBatch, texture, Position, DETECTION_RADIUS, Color.Yellow * 0.3f);
            DrawCircle(spriteBatch, texture, Position, ATTACK_RADIUS, Color.Red * 0.3f);
            
            // 体力バーを描画
            DrawHealthBar(spriteBatch, texture);
            
            // うろつき目標を描画（デバッグ用）
            if (CurrentState == BTAgentState.Wander)
            {
                DrawLine(spriteBatch, texture, Position, _wanderTarget, Color.Cyan * 0.5f);
            }
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
                new Vector2(edge.Length() / texture.Width, 1f / texture.Height),
                SpriteEffects.None,
                0f
            );
        }

        private void DrawHealthBar(SpriteBatch spriteBatch, Texture2D texture)
        {
            // 体力バーの背景（灰色）
            Rectangle backgroundRect = new Rectangle(
                (int)(Position.X - 20),
                (int)(Position.Y - 30),
                40,
                5
            );
            spriteBatch.Draw(texture, backgroundRect, Color.DarkGray);
            
            // 体力バー（緑〜赤）
            Rectangle healthRect = new Rectangle(
                (int)(Position.X - 20),
                (int)(Position.Y - 30),
                (int)(40 * _health),
                5
            );
            
            // 体力に応じて色を変更（1.0=緑、0.5=黄色、0.0=赤）
            Color healthColor = new Color(
                (1.0f - _health) * 2.0f, // R
                _health * 2.0f,          // G
                0f                       // B
            );
            
            spriteBatch.Draw(texture, healthRect, healthColor);
        }

        // 障害物を考慮した位置更新メソッド
        private void UpdatePositionWithObstacles(float deltaTime)
        {
            // 次の位置を計算
            Vector2 nextPosition = Position + Velocity * deltaTime;
            
            // 一時的なGameObjectを作成して次の位置での衝突チェック用に使用
            GameObject tempAgent = new SimpleGameObject(nextPosition, Radius, Color.Transparent);
            
            // 障害物との衝突をチェック
            foreach (var obstacle in _obstacles)
            {
                if (obstacle.Intersects(tempAgent))
                {
                    // 障害物と衝突する場合は移動をキャンセルまたは滑らせる
                    // 障害物の中心を計算
                    Vector2 obstacleCenter = new Vector2(
                        obstacle.Bounds.X + obstacle.Bounds.Width / 2,
                        obstacle.Bounds.Y + obstacle.Bounds.Height / 2
                    );
                    
                    Vector2 toObstacle = obstacleCenter - Position;
                    float distanceToObstacle = toObstacle.Length();
                    
                    if (distanceToObstacle > 0)
                    {
                        // 障害物からの反発ベクトルを計算
                        toObstacle.Normalize();
                        Vector2 slideDirection = Velocity - Vector2.Dot(Velocity, toObstacle) * toObstacle;
                        
                        // 滑る方向に移動
                        if (slideDirection.Length() > 0)
                        {
                            slideDirection.Normalize();
                            nextPosition = Position + slideDirection * AGENT_SPEED * 0.5f * deltaTime;
                        }
                        else
                        {
                            // 滑る方向がない場合は移動しない
                            nextPosition = Position;
                        }
                        break;
                    }
                }
            }
            
            // グリッドマップのセルが歩行可能かチェック
            if (_gridMap.IsWalkable(nextPosition))
            {
                Position = nextPosition;
            }
        }

        // 衝突検出用の単純なGameObject実装
        private class SimpleGameObject : GameObject
        {
            public SimpleGameObject(Vector2 position, float radius, Color color) 
                : base(position, radius, color)
            {
            }
        }
    }
} 
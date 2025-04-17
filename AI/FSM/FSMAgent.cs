using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameAIDemo.Entities;
using GameAIDemo.Utilities;

namespace GameAIDemo.AI.FSM
{
    public class FSMAgent
    {
        public Vector2 Position { get; set; }
        public float Radius { get; set; } = 15f;
        public Color Color { get; set; } = Color.Yellow;
        
        // 検知範囲の定数
        private const float DETECTION_RADIUS = 150f;      // プレイヤー検知範囲
        private const float DETECTION_LOST_RADIUS = 250f; // プレイヤーロスト範囲
        
        private const float RAD_CHASE = 200.0f;
        private const float RAD_DETECT = 150.0f;
        
        private readonly Entity entity;
        private readonly GridMap gridMap;
        private readonly Player target;
        
        // テクスチャとスプライトバッチの参照
        private SpriteBatch spriteBatch;
        private Texture2D texture;
        
        public FSMAgent(Vector2 position, GridMap gridMap, Player target)
        {
            Position = position;
            this.gridMap = gridMap;
            this.target = target;
            
            // FSMを使用するエンティティを初期化
            entity = new Entity("FSMAgent", position);
            
            // 初期状態を巡回状態にする
            entity.TriggerEvent("Patrol");
        }
        
        public void Update(GameTime gameTime)
        {
            // プレイヤーの位置を更新
            UpdatePlayerPosition();
            
            // エンティティを更新
            entity.Update(gameTime);
            
            // エンティティの位置を同期
            Position = entity.Position;
            
            // ターゲット（プレイヤー）との距離を計算
            float distanceToPlayer = Vector2.Distance(Position, target.Position);
            
            // 現在の状態を取得
            string currentState = entity.GetCurrentStateName();
            
            // プレイヤーの近接状態に応じて適切なイベントをトリガー
            if (distanceToPlayer < DETECTION_RADIUS && currentState != "ChaseState")
            {
                // プレイヤーが近くにいて追跡状態でなければ、追跡開始
                entity.TriggerEvent("PlayerDetected");
            }
            else if (distanceToPlayer > DETECTION_LOST_RADIUS && currentState == "ChaseState")
            {
                // プレイヤーが遠くにいて追跡状態なら、追跡終了
                entity.TriggerEvent("PlayerLost");
            }
            
            // 現在の状態に基づいて色を変更
            UpdateColor();
        }
        
        // 内部のプレイヤーエンティティの位置を実際のプレイヤーの位置に更新
        private void UpdatePlayerPosition()
        {
            if (entity != null)
            {
                Entity.UpdatePlayerPosition(target.Position);
            }
        }
        
        private void UpdateColor()
        {
            string stateName = entity.GetCurrentStateName();
            
            switch (stateName)
            {
                case "IdleState":
                    Color = Color.Yellow;
                    break;
                case "PatrolState":
                    Color = Color.Green;
                    break;
                case "ChaseState":
                    Color = Color.Red;
                    break;
                default:
                    Color = Color.White;
                    break;
            }
        }
        
        public void Draw(SpriteBatch spriteBatch, Texture2D texture)
        {
            this.spriteBatch = spriteBatch;
            this.texture = texture;
            
            // 現在の状態を取得
            string currentState = entity.GetCurrentStateName();
            
            // 索敵範囲を描画
            DrawDetectionRange(currentState);
            
            // エージェントを描画
            DrawAgent();
        }
        
        // 索敵範囲を描画するメソッド
        private void DrawDetectionRange(string currentState)
        {
            // 追跡状態によって色を変える
            Color detectionColor;
            Color lostColor;
            
            if (currentState == "ChaseState")
            {
                // 追跡中は赤系の色で表示
                detectionColor = new Color(255, 0, 0, 30);    // 薄い赤
                lostColor = new Color(255, 100, 100, 20);     // さらに薄いピンク
            }
            else
            {
                // 非追跡中は青系の色で表示
                detectionColor = new Color(0, 0, 255, 30);    // 薄い青
                lostColor = new Color(100, 100, 255, 20);     // さらに薄い青
            }
            
            // 追跡ロスト範囲（大きい円）を描画
            float lostScale = DETECTION_LOST_RADIUS * 2 / texture.Width;
            spriteBatch.Draw(
                texture,
                Position,
                null,
                lostColor,
                0f,
                new Vector2(texture.Width / 2, texture.Height / 2),
                lostScale,
                SpriteEffects.None,
                0f
            );
            
            // 検知範囲（小さい円）を描画
            float detectionScale = DETECTION_RADIUS * 2 / texture.Width;
            spriteBatch.Draw(
                texture,
                Position,
                null,
                detectionColor,
                0f,
                new Vector2(texture.Width / 2, texture.Height / 2),
                detectionScale,
                SpriteEffects.None,
                0f
            );
        }
        
        private void DrawAgent()
        {
            spriteBatch.Draw(
                texture,
                Position,
                null,
                Color,
                0f,
                new Vector2(texture.Width / 2, texture.Height / 2),
                Radius * 2 / texture.Width,
                SpriteEffects.None,
                0f
            );
        }
    }
} 
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GameAIDemo.Entities;
using GameAIDemo.Utilities;
using GameAIDemo.AI.AStar;
using GameAIDemo.AI.BehaviorTree;
using GameAIDemo.AI.FSM;

namespace GameAIDemo
{
    public enum AIMode
    {
        AStar,
        BehaviorTree,
        FSM
    }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D _circleTexture;
        private Texture2D _rectangleTexture;
        private SpriteFont _font;  // フォントを追加
        
        private Player _player;
        private List<Obstacle> _obstacles;
        private GridMap _gridMap;
        
        // A*パスファインディング
        private List<AStarAgent> _astarAgents;
        
        // ビヘイビアツリー
        private List<BTAgent> _btAgents;
        
        // FSM
        private List<FSMAgent> _fsmAgents;
        
        // 現在のAIモード
        private AIMode _currentMode = AIMode.AStar;
        
        // UI制御
        private bool _showHelp = true;
        private KeyboardState _previousKeyboardState;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            
            // ウィンドウサイズを設定
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
        }

        protected override void Initialize()
        {
            // 画面サイズを設定
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.ApplyChanges();
            
            // 初期化
            _obstacles = new List<Obstacle>();
            _astarAgents = new List<AStarAgent>();
            _btAgents = new List<BTAgent>();
            _fsmAgents = new List<FSMAgent>();
            
            base.Initialize();
            
            // マップと障害物を初期化
            InitializeMapAndObstacles();
            
            // プレイヤーを作成
            _player = new Player(new Vector2(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 2));
            
            // AIエージェントを作成
            CreateAIAgents();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // テクスチャを作成
            _circleTexture = CreateCircleTexture(100);
            _rectangleTexture = new Texture2D(GraphicsDevice, 1, 1);
            _rectangleTexture.SetData(new[] { Color.White });
            
            // フォントをロード
            try
            {
                _font = Content.Load<SpriteFont>("Font");
            }
            catch (Exception ex)
            {
                // フォントがロードできなくても続行できるようにする
                System.Diagnostics.Debug.WriteLine($"Font loading error: {ex.Message}");
            }
            
            // プレイヤーを作成
            _player = new Player(new Vector2(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 2));
            
            // 障害物とマップを作成
            InitializeMapAndObstacles();
            
            // AIエージェントを作成
            CreateAIAgents();
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();
            
            // ESCキーでゲーム終了
            if (keyboardState.IsKeyDown(Keys.Escape))
                Exit();
                
            // AIモードの切り替え
            if (keyboardState.IsKeyDown(Keys.D1) && _previousKeyboardState.IsKeyUp(Keys.D1))
            {
                if (_currentMode != AIMode.AStar)
                {
                    _currentMode = AIMode.AStar;
                    ReinitializeMapAndAgents();
                }
            }
            else if (keyboardState.IsKeyDown(Keys.D2) && _previousKeyboardState.IsKeyUp(Keys.D2))
            {
                if (_currentMode != AIMode.BehaviorTree)
                {
                    _currentMode = AIMode.BehaviorTree;
                    ReinitializeMapAndAgents();
                }
            }
            else if (keyboardState.IsKeyDown(Keys.D3) && _previousKeyboardState.IsKeyUp(Keys.D3))
            {
                if (_currentMode != AIMode.FSM)
                {
                    _currentMode = AIMode.FSM;
                    ReinitializeMapAndAgents();
                }
            }
                
            // ヘルプの表示/非表示
            if (keyboardState.IsKeyDown(Keys.H) && _previousKeyboardState.IsKeyUp(Keys.H))
                _showHelp = !_showHelp;
                
            // AIのリセット
            if (keyboardState.IsKeyDown(Keys.R) && _previousKeyboardState.IsKeyUp(Keys.R))
                ResetAIAgents();
            
            // プレイヤーの更新
            _player.Update(gameTime);
            
            // 画面境界との衝突判定
            ConstrainPlayerToScreen();
            
            // 障害物との衝突判定
            HandlePlayerObstacleCollision();
            
            // 現在のモードに応じてAIを更新
            switch (_currentMode)
            {
                case AIMode.AStar:
                    foreach (var agent in _astarAgents)
                        agent.Update(gameTime);
                    break;
                    
                case AIMode.BehaviorTree:
                    foreach (var agent in _btAgents)
                        agent.Update(gameTime);
                    break;
                    
                case AIMode.FSM:
                    foreach (var agent in _fsmAgents)
                        agent.Update(gameTime);
                    break;
            }
            
            _previousKeyboardState = keyboardState;
            
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin();
            
            // グリッドマップを描画
            _gridMap.DrawGrid(_spriteBatch, _rectangleTexture);
            
            // 障害物を描画
            foreach (var obstacle in _obstacles)
                obstacle.Draw(_spriteBatch, _rectangleTexture);
            
            // 現在のモードに応じてAIを描画
            switch (_currentMode)
            {
                case AIMode.AStar:
                    foreach (var agent in _astarAgents)
                        agent.Draw(_spriteBatch, _circleTexture);
                    break;
                    
                case AIMode.BehaviorTree:
                    foreach (var agent in _btAgents)
                        agent.Draw(_spriteBatch, _circleTexture);
                    break;
                    
                case AIMode.FSM:
                    foreach (var agent in _fsmAgents)
                        agent.Draw(_spriteBatch, _circleTexture);
                    break;
            }
            
            // プレイヤーを描画
            _player.Draw(_spriteBatch, _circleTexture);
            
            // ヘルプテキストを描画
            if (_showHelp)
                DrawHelpText();
            
            // 現在のモードを表示
            DrawModeInfo();
            
            _spriteBatch.End();

            base.Draw(gameTime);
        }

        // マップと障害物を初期化するヘルパーメソッド
        private void InitializeMapAndObstacles()
        {
            _obstacles.Clear();
            
            // 矩形境界を作成
            CreateRectangularMap();
            
            // グリッドマップを作成/更新
            _gridMap = new GridMap(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight, 20, _obstacles);
        }

        // モードを切り替えた際にマップとエージェントを再初期化
        private void ReinitializeMapAndAgents()
        {
            // マップと障害物を再作成
            InitializeMapAndObstacles();
            
            // AIエージェントを再作成
            ResetAIAgents();
        }

        // 矩形の標準マップを作成
        private void CreateRectangularMap()
        {
            // 画面の端に壁を作成
            int wallThickness = 20;
            
            // 上の壁
            _obstacles.Add(new Obstacle(new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, wallThickness), Color.Gray));
            
            // 下の壁
            _obstacles.Add(new Obstacle(new Rectangle(0, _graphics.PreferredBackBufferHeight - wallThickness, _graphics.PreferredBackBufferWidth, wallThickness), Color.Gray));
            
            // 左の壁
            _obstacles.Add(new Obstacle(new Rectangle(0, 0, wallThickness, _graphics.PreferredBackBufferHeight), Color.Gray));
            
            // 右の壁
            _obstacles.Add(new Obstacle(new Rectangle(_graphics.PreferredBackBufferWidth - wallThickness, 0, wallThickness, _graphics.PreferredBackBufferHeight), Color.Gray));
            
            // 中央に障害物を作成
            _obstacles.Add(new Obstacle(new Rectangle(400, 200, 100, 100), Color.Gray));
            _obstacles.Add(new Obstacle(new Rectangle(700, 400, 150, 80), Color.Gray));
            _obstacles.Add(new Obstacle(new Rectangle(300, 500, 80, 120), Color.Gray));
            _obstacles.Add(new Obstacle(new Rectangle(900, 150, 120, 120), Color.Gray));
            _obstacles.Add(new Obstacle(new Rectangle(500, 350, 80, 80), Color.Gray));
        }

        private void CreateAIAgents()
        {
            Random random = new Random();
            
            // エージェントリストをクリア
            _astarAgents.Clear();
            _btAgents.Clear();
            _fsmAgents.Clear();
            
            // A*エージェントを作成
            for (int i = 0; i < 2; i++)
            {
                Vector2 position = GetRandomPosition(random);
                _astarAgents.Add(new AStarAgent(position, _gridMap, _player));
            }
            
            // ビヘイビアツリーエージェントを作成
            for (int i = 0; i < 2; i++)
            {
                Vector2 position = GetRandomPosition(random);
                _btAgents.Add(new BTAgent(position, _gridMap, _player, _obstacles));
            }
            
            // FSMエージェントを作成
            for (int i = 0; i < 2; i++)
            {
                Vector2 position = GetRandomPosition(random);
                _fsmAgents.Add(new FSMAgent(position, _gridMap, _player));
            }
        }

        private Vector2 GetRandomPosition(Random random)
        {
            const int MAX_ATTEMPTS = 20;
            Vector2 position;
            bool isValid = false;
            int attempts = 0;
            
            do
            {
                int x = random.Next(50, _graphics.PreferredBackBufferWidth - 50);
                int y = random.Next(50, _graphics.PreferredBackBufferHeight - 50);
                position = new Vector2(x, y);
                
                isValid = true;
                foreach (var obstacle in _obstacles)
                {
                    // GameObject抽象クラスのインスタンスを作成できないため、直接判定
                    float radius = 20f;
                    Vector2 closestPoint = new Vector2(
                        MathHelper.Clamp(position.X, obstacle.Bounds.Left, obstacle.Bounds.Right),
                        MathHelper.Clamp(position.Y, obstacle.Bounds.Top, obstacle.Bounds.Bottom)
                    );
                    
                    if (Vector2.Distance(position, closestPoint) < radius)
                    {
                        isValid = false;
                        break;
                    }
                }
                
                attempts++;
            } 
            while (!isValid && attempts < MAX_ATTEMPTS);
            
            return position;
        }

        private void ResetAIAgents()
        {
            Random random = new Random();
            
            // エージェントリストをクリア
            _astarAgents.Clear();
            _btAgents.Clear();
            _fsmAgents.Clear();
            
            // 新しいエージェントを作成
            // A*エージェントを作成
            for (int i = 0; i < 2; i++)
            {
                Vector2 position = GetRandomPosition(random);
                _astarAgents.Add(new AStarAgent(position, _gridMap, _player));
            }
            
            // ビヘイビアツリーエージェントを作成
            for (int i = 0; i < 2; i++)
            {
                Vector2 position = GetRandomPosition(random);
                _btAgents.Add(new BTAgent(position, _gridMap, _player, _obstacles));
            }
            
            // FSMエージェントを作成
            for (int i = 0; i < 2; i++)
            {
                Vector2 position = GetRandomPosition(random);
                _fsmAgents.Add(new FSMAgent(position, _gridMap, _player));
            }
        }

        private void ConstrainPlayerToScreen()
        {
            int margin = 30;
            float previousX = _player.Position.X;
            float previousY = _player.Position.Y;
            
            // 画面の境界に到達したときの位置を制限
            _player.Position = new Vector2(
                MathHelper.Clamp(_player.Position.X, margin, _graphics.PreferredBackBufferWidth - margin),
                MathHelper.Clamp(_player.Position.Y, margin, _graphics.PreferredBackBufferHeight - margin)
            );
            
            // 角に近づいたかをチェック
            bool isNearCorner = 
                (_player.Position.X < margin + 20 && _player.Position.Y < margin + 20) ||
                (_player.Position.X > _graphics.PreferredBackBufferWidth - margin - 20 && _player.Position.Y < margin + 20) ||
                (_player.Position.X < margin + 20 && _player.Position.Y > _graphics.PreferredBackBufferHeight - margin - 20) ||
                (_player.Position.X > _graphics.PreferredBackBufferWidth - margin - 20 && _player.Position.Y > _graphics.PreferredBackBufferHeight - margin - 20);
            
            // 角に近い場合、中心方向への緩やかな力を加える
            if (isNearCorner)
            {
                Vector2 screenCenter = new Vector2(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 2);
                Vector2 toCenter = screenCenter - _player.Position;
                
                if (toCenter != Vector2.Zero)
                {
                    toCenter.Normalize();
                    // 現在の速度と中心への方向を組み合わせる（緩やかな修正）
                    _player.Velocity = Vector2.Lerp(_player.Velocity, toCenter * _player.Velocity.Length(), 0.2f);
                }
            }
        }

        private void HandlePlayerObstacleCollision()
        {
            bool hadCollision = false;
            Vector2 totalPushDirection = Vector2.Zero;
            
            foreach (var obstacle in _obstacles)
            {
                // 近接点による衝突チェック（より正確）
                Vector2 closestPoint = new Vector2(
                    MathHelper.Clamp(_player.Position.X, obstacle.Bounds.Left, obstacle.Bounds.Right),
                    MathHelper.Clamp(_player.Position.Y, obstacle.Bounds.Top, obstacle.Bounds.Bottom)
                );
                
                float distance = Vector2.Distance(_player.Position, closestPoint);
                
                if (distance < _player.Radius)
                {
                    hadCollision = true;
                    
                    // 障害物からプレイヤーへの方向
                    Vector2 pushDirection = _player.Position - closestPoint;
                    if (pushDirection != Vector2.Zero)
                    {
                        pushDirection.Normalize();
                        // より強く押し出す
                        totalPushDirection += pushDirection;
                    }
                }
            }
            
            if (hadCollision && totalPushDirection != Vector2.Zero)
            {
                // 複数の障害物からの方向を平均化
                totalPushDirection.Normalize();
                
                // 障害物から大きく押し出す + 速度を調整
                _player.Position += totalPushDirection * (_player.Radius * 1.2f);
                
                // 衝突後の速度を反射方向に設定
                Vector2 reflection = Vector2.Reflect(_player.Velocity, totalPushDirection);
                reflection *= 0.7f; // 減衰させる
                
                // 現在の速度と反射速度を混合
                _player.Velocity = Vector2.Lerp(_player.Velocity, reflection, 0.5f);
                
                // 角に近いかチェック
                bool isNearCorner = IsPlayerNearCorner();
                
                if (isNearCorner)
                {
                    // 角からの脱出方向を強化
                    Vector2 screenCenter = new Vector2(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 2);
                    Vector2 toCenter = screenCenter - _player.Position;
                    
                    if (toCenter != Vector2.Zero)
                    {
                        toCenter.Normalize();
                        _player.Velocity = Vector2.Lerp(_player.Velocity, toCenter * _player.Velocity.Length() * 1.5f, 0.4f);
                        // 中心方向に少し押し出す
                        _player.Position += toCenter * 5f;
                    }
                }
            }
            
            // 最終確認 - プレイヤーが障害物の中にいないことを確認
            EnsurePlayerNotInObstacles();
        }

        // プレイヤーが角に近いかを判定
        private bool IsPlayerNearCorner()
        {
            int margin = 40; // 角検出マージン
            
            return (_player.Position.X < margin && _player.Position.Y < margin) ||
                   (_player.Position.X > _graphics.PreferredBackBufferWidth - margin && _player.Position.Y < margin) ||
                   (_player.Position.X < margin && _player.Position.Y > _graphics.PreferredBackBufferHeight - margin) ||
                   (_player.Position.X > _graphics.PreferredBackBufferWidth - margin && _player.Position.Y > _graphics.PreferredBackBufferHeight - margin);
        }

        // プレイヤーが障害物の中にいないことを確認
        private void EnsurePlayerNotInObstacles()
        {
            foreach (var obstacle in _obstacles)
            {
                Vector2 closestPoint = new Vector2(
                    MathHelper.Clamp(_player.Position.X, obstacle.Bounds.Left, obstacle.Bounds.Right),
                    MathHelper.Clamp(_player.Position.Y, obstacle.Bounds.Top, obstacle.Bounds.Bottom)
                );
                
                float distance = Vector2.Distance(_player.Position, closestPoint);
                
                if (distance < _player.Radius)
                {
                    // まだ障害物内にいる場合は強制的に外に押し出す
                    Vector2 pushDirection = _player.Position - closestPoint;
                    if (pushDirection != Vector2.Zero)
                    {
                        pushDirection.Normalize();
                        _player.Position = closestPoint + pushDirection * (_player.Radius + 2f);
                    }
                }
            }
        }

        private Texture2D CreateCircleTexture(int radius)
        {
            int diameter = radius * 2;
            Texture2D texture = new Texture2D(GraphicsDevice, diameter, diameter);
            Color[] colorData = new Color[diameter * diameter];
            
            float radiusSquared = radius * radius;
            
            for (int x = 0; x < diameter; x++)
            {
                for (int y = 0; y < diameter; y++)
                {
                    int index = x * diameter + y;
                    Vector2 pos = new Vector2(x - radius, y - radius);
                    if (pos.LengthSquared() <= radiusSquared)
                    {
                        colorData[index] = Color.White;
                    }
                    else
                    {
                        colorData[index] = Color.Transparent;
                    }
                }
            }
            
            texture.SetData(colorData);
            return texture;
        }

        private void DrawHelpText()
        {
            if (_font == null) 
            {
                // フォントがロードできない場合は視覚的なUIを表示
                DrawVisualHelpText();
                return;
            }

            // テキストベースのヘルプを表示
            int y = 10;
            int lineHeight = 25;
            
            string helpText = 
                "GAME AI DEMO - CONTROLS\n" +
                "WASD / Arrow Keys: Move Player\n" +
                "1: A* Pathfinding AI\n" +
                "2: Behavior Tree AI\n" +
                "3: FSM AI\n" +
                "R: Reset AI Positions\n" +
                "H: Toggle Help\n" +
                "ESC: Exit Game";
            
            // 背景を描画
            _spriteBatch.Draw(
                _rectangleTexture, 
                new Rectangle(5, 5, 300, 200), 
                new Color(0, 0, 0, 180)
            );
            
            // テキスト行ごとに描画
            string[] lines = helpText.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                Color textColor = Color.White;
                
                // 行ごとに色を変える
                if (i == 2) textColor = Color.Red;      // A*
                else if (i == 3) textColor = Color.Orange; // Behavior Tree
                else if (i == 4) textColor = Color.Yellow; // FSM
                else if (i == 0) textColor = Color.LightBlue; // タイトル
                
                _spriteBatch.DrawString(
                    _font,
                    lines[i],
                    new Vector2(10, y),
                    textColor
                );
                
                y += lineHeight;
            }
        }

        // 視覚的なヘルプ表示（フォントが使えない場合のフォールバック）
        private void DrawVisualHelpText()
        {
            // 操作方法の説明を表示
            int y = 10;
            int height = 25;
            int width = 25;
            int margin = 5;
            int textWidth = 180;
            
            // タイトル
            _spriteBatch.Draw(_rectangleTexture, new Rectangle(10, y, width + margin + textWidth, height), Color.DarkBlue * 0.9f);
            y += height + margin;
            
            // WASDキー - プレイヤー移動
            _spriteBatch.Draw(_rectangleTexture, new Rectangle(10, y, width, height), Color.White * 0.7f);
            _spriteBatch.Draw(_rectangleTexture, new Rectangle(10 + width + margin, y, textWidth, height), Color.White * 0.5f);
            y += height + margin;
            
            // 1キー - A*
            _spriteBatch.Draw(_rectangleTexture, new Rectangle(10, y, width, height), Color.Red * 0.7f);
            _spriteBatch.Draw(_rectangleTexture, new Rectangle(10 + width + margin, y, textWidth, height), Color.White * 0.5f);
            // A*と表記
            DrawBox(_rectangleTexture, new Vector2(10 + width + margin + 10, y + height/2), Color.Red, 3);
            DrawBox(_rectangleTexture, new Vector2(10 + width + margin + 20, y + height/2), Color.Red, 3);
            DrawBox(_rectangleTexture, new Vector2(10 + width + margin + 35, y + height/2), Color.Red, 3);
            y += height + margin;
            
            // 2キー - ビヘイビアツリー
            _spriteBatch.Draw(_rectangleTexture, new Rectangle(10, y, width, height), Color.Orange * 0.7f);
            _spriteBatch.Draw(_rectangleTexture, new Rectangle(10 + width + margin, y, textWidth, height), Color.White * 0.5f);
            // ツリーアイコン
            DrawLine(_spriteBatch, _rectangleTexture, 
                new Vector2(10 + width + margin + 10, y + 5), 
                new Vector2(10 + width + margin + 25, y + height/2), 
                Color.Orange);
            DrawLine(_spriteBatch, _rectangleTexture, 
                new Vector2(10 + width + margin + 40, y + 5), 
                new Vector2(10 + width + margin + 25, y + height/2), 
                Color.Orange);
            y += height + margin;
            
            // 3キー - FSM
            _spriteBatch.Draw(_rectangleTexture, new Rectangle(10, y, width, height), Color.Yellow * 0.7f);
            _spriteBatch.Draw(_rectangleTexture, new Rectangle(10 + width + margin, y, textWidth, height), Color.White * 0.5f);
            // FSMと表記
            DrawBox(_rectangleTexture, new Vector2(10 + width + margin + 10, y + height/2), Color.Yellow, 3);
            DrawBox(_rectangleTexture, new Vector2(10 + width + margin + 20, y + height/2), Color.Yellow, 3);
            DrawBox(_rectangleTexture, new Vector2(10 + width + margin + 35, y + height/2), Color.Yellow, 3);
            y += height + margin;
            
            // Rキー - リセット
            _spriteBatch.Draw(_rectangleTexture, new Rectangle(10, y, width, height), Color.Cyan * 0.7f);
            _spriteBatch.Draw(_rectangleTexture, new Rectangle(10 + width + margin, y, textWidth, height), Color.White * 0.5f);
            y += height + margin;
            
            // Hキー - ヘルプ
            _spriteBatch.Draw(_rectangleTexture, new Rectangle(10, y, width, height), Color.Green * 0.7f);
            _spriteBatch.Draw(_rectangleTexture, new Rectangle(10 + width + margin, y, textWidth, height), Color.White * 0.5f);
            y += height + margin;
            
            // ESCキー - 終了
            _spriteBatch.Draw(_rectangleTexture, new Rectangle(10, y, width, height), Color.Gray * 0.7f);
            _spriteBatch.Draw(_rectangleTexture, new Rectangle(10 + width + margin, y, textWidth, height), Color.White * 0.5f);
        }

        private void DrawBox(Texture2D texture, Vector2 position, Color color, float size)
        {
            _spriteBatch.Draw(
                texture,
                position, 
                null,
                color,
                0f,
                new Vector2(0.5f),
                new Vector2(size),
                SpriteEffects.None,
                0f
            );
        }

        private void DrawLine(SpriteBatch spriteBatch, Texture2D texture, Vector2 start, Vector2 end, Color color)
        {
            Vector2 direction = end - start;
            float length = direction.Length();
            if (length == 0) return;

            direction.Normalize();

            int segments = (int)length;
            for (int i = 0; i < segments; i++)
            {
                float t = (float)i / segments;
                Vector2 point = start + direction * t;
                spriteBatch.Draw(
                    texture,
                    point,
                    null,
                    color,
                    0f,
                    Vector2.Zero,
                    new Vector2(1, 1),
                    SpriteEffects.None,
                    0f
                );
            }
        }

        private void DrawModeInfo()
        {
            // 現在のAIモードを表示
            Color modeColor = Color.White;
            string modeName = "";
            
            switch (_currentMode)
            {
                case AIMode.AStar:
                    modeColor = Color.Red;
                    modeName = "A* PATHFINDING";
                    break;
                case AIMode.BehaviorTree:
                    modeColor = Color.Orange;
                    modeName = "BEHAVIOR TREE";
                    break;
                case AIMode.FSM:
                    modeColor = Color.Yellow;
                    modeName = "FSM";
                    break;
            }
            
            // テキストでの表示（フォントがある場合）
            if (_font != null)
            {
                // 背景
                _spriteBatch.Draw(
                    _rectangleTexture,
                    new Rectangle(10, _graphics.PreferredBackBufferHeight - 40, 300, 30),
                    new Color(0, 0, 0, 180)
                );
                
                // モード番号
                int modeNumber = _currentMode == AIMode.AStar ? 1 : _currentMode == AIMode.BehaviorTree ? 2 : 3;
                _spriteBatch.DrawString(
                    _font,
                    $"MODE {modeNumber}:",
                    new Vector2(15, _graphics.PreferredBackBufferHeight - 35),
                    modeColor
                );
                
                // モード名
                _spriteBatch.DrawString(
                    _font,
                    modeName,
                    new Vector2(100, _graphics.PreferredBackBufferHeight - 35),
                    modeColor
                );
                
                // AIの説明テキスト
                string description = GetAIDescription(_currentMode);
                _spriteBatch.DrawString(
                    _font,
                    description,
                    new Vector2(10, _graphics.PreferredBackBufferHeight - 70),
                    Color.White
                );
            }
            else
            {
                // フォントがない場合は視覚的なUIを表示
                DrawVisualModeInfo();
            }
        }

        private string GetAIDescription(AIMode mode)
        {
            switch (mode)
            {
                case AIMode.AStar:
                    return "Efficient path planning with obstacle avoidance";
                case AIMode.BehaviorTree:
                    return "Decision making with hierarchical state management";
                case AIMode.FSM:
                    return "Finite State Machine for complex decision-making";
                default:
                    return "";
            }
        }

        // 視覚的なモード表示（フォントが使えない場合のフォールバック）
        private void DrawVisualModeInfo()
        {
            // 現在のAIモードを表示
            Color modeColor = Color.White;
            
            switch (_currentMode)
            {
                case AIMode.AStar:
                    modeColor = Color.Red;
                    break;
                case AIMode.BehaviorTree:
                    modeColor = Color.Orange;
                    break;
                case AIMode.FSM:
                    modeColor = Color.Yellow;
                    break;
            }
            
            // モード表示の背景
            _spriteBatch.Draw(
                _rectangleTexture,
                new Rectangle(10, _graphics.PreferredBackBufferHeight - 40, 300, 30),
                Color.Black * 0.7f
            );
            
            // 現在のモードのカラーインジケータ
            _spriteBatch.Draw(
                _rectangleTexture,
                new Rectangle(15, _graphics.PreferredBackBufferHeight - 35, 20, 20),
                modeColor
            );
            
            // モード番号を表示
            int modeNumber = _currentMode == AIMode.AStar ? 1 : _currentMode == AIMode.BehaviorTree ? 2 : 3;
            _spriteBatch.Draw(
                _circleTexture,
                new Vector2(50, _graphics.PreferredBackBufferHeight - 25),
                null,
                Color.White,
                0f,
                new Vector2(_circleTexture.Width / 2, _circleTexture.Height / 2),
                12f / _circleTexture.Width,
                SpriteEffects.None,
                0f
            );
            
            // モード番号を表示（中央に数字）
            // 最も単純な方法で数字を描画
            string numberText = (modeNumber).ToString();
            _spriteBatch.Draw(
                _rectangleTexture, 
                new Vector2(50 - 3, _graphics.PreferredBackBufferHeight - 25 - 5), 
                null,
                Color.Black,
                0f,
                Vector2.Zero,
                new Vector2(6, 2),
                SpriteEffects.None,
                0f
            );
            
            // AIタイプのアイコンを表示
            _spriteBatch.Draw(
                _rectangleTexture,
                new Rectangle(80, _graphics.PreferredBackBufferHeight - 35, 220, 20),
                modeColor * 0.3f
            );
            
            // AIタイプごとに特徴的なアイコンを描画
            switch (_currentMode)
            {
                case AIMode.AStar:
                    // A*パスファインディングのアイコン（グリッド上のパス）
                    Vector2 startPoint = new Vector2(100, _graphics.PreferredBackBufferHeight - 25);
                    Vector2 endPoint = new Vector2(180, _graphics.PreferredBackBufferHeight - 25);
                    DrawLine(_spriteBatch, _rectangleTexture, startPoint, endPoint, Color.White);
                    
                    // 経路上の障害物を表現
                    _spriteBatch.Draw(
                        _rectangleTexture,
                        new Rectangle(130, _graphics.PreferredBackBufferHeight - 35, 10, 20),
                        Color.Gray * 0.8f
                    );
                    
                    // 迂回ルートを表現
                    Vector2 detourPoint1 = new Vector2(120, _graphics.PreferredBackBufferHeight - 15);
                    Vector2 detourPoint2 = new Vector2(150, _graphics.PreferredBackBufferHeight - 15);
                    DrawLine(_spriteBatch, _rectangleTexture, startPoint, detourPoint1, Color.Green);
                    DrawLine(_spriteBatch, _rectangleTexture, detourPoint1, detourPoint2, Color.Green);
                    DrawLine(_spriteBatch, _rectangleTexture, detourPoint2, endPoint, Color.Green);
                    break;
                    
                case AIMode.BehaviorTree:
                    // ビヘイビアツリーのアイコン（ツリー構造）
                    Vector2 treeRoot = new Vector2(140, _graphics.PreferredBackBufferHeight - 35);
                    Vector2 node1 = new Vector2(120, _graphics.PreferredBackBufferHeight - 25);
                    Vector2 node2 = new Vector2(160, _graphics.PreferredBackBufferHeight - 25);
                    Vector2 leaf1 = new Vector2(110, _graphics.PreferredBackBufferHeight - 15);
                    Vector2 leaf2 = new Vector2(130, _graphics.PreferredBackBufferHeight - 15);
                    Vector2 leaf3 = new Vector2(150, _graphics.PreferredBackBufferHeight - 15);
                    Vector2 leaf4 = new Vector2(170, _graphics.PreferredBackBufferHeight - 15);
                    
                    // ツリーのエッジ
                    DrawLine(_spriteBatch, _rectangleTexture, treeRoot, node1, Color.White);
                    DrawLine(_spriteBatch, _rectangleTexture, treeRoot, node2, Color.White);
                    DrawLine(_spriteBatch, _rectangleTexture, node1, leaf1, Color.White);
                    DrawLine(_spriteBatch, _rectangleTexture, node1, leaf2, Color.White);
                    DrawLine(_spriteBatch, _rectangleTexture, node2, leaf3, Color.White);
                    DrawLine(_spriteBatch, _rectangleTexture, node2, leaf4, Color.White);
                    
                    // ノードの表現
                    _spriteBatch.Draw(
                        _circleTexture,
                        treeRoot,
                        null,
                        Color.Orange,
                        0f,
                        new Vector2(_circleTexture.Width / 2, _circleTexture.Height / 2),
                        4f / _circleTexture.Width,
                        SpriteEffects.None,
                        0f
                    );
                    
                    _spriteBatch.Draw(
                        _circleTexture,
                        node1,
                        null,
                        Color.Orange * 0.8f,
                        0f,
                        new Vector2(_circleTexture.Width / 2, _circleTexture.Height / 2),
                        3f / _circleTexture.Width,
                        SpriteEffects.None,
                        0f
                    );
                    
                    _spriteBatch.Draw(
                        _circleTexture,
                        node2,
                        null,
                        Color.Orange * 0.8f,
                        0f,
                        new Vector2(_circleTexture.Width / 2, _circleTexture.Height / 2),
                        3f / _circleTexture.Width,
                        SpriteEffects.None,
                        0f
                    );
                    
                    // 葉ノード
                    _spriteBatch.Draw(
                        _circleTexture,
                        leaf1,
                        null,
                        Color.White,
                        0f,
                        new Vector2(_circleTexture.Width / 2, _circleTexture.Height / 2),
                        2f / _circleTexture.Width,
                        SpriteEffects.None,
                        0f
                    );
                    
                    _spriteBatch.Draw(
                        _circleTexture,
                        leaf2,
                        null,
                        Color.White,
                        0f,
                        new Vector2(_circleTexture.Width / 2, _circleTexture.Height / 2),
                        2f / _circleTexture.Width,
                        SpriteEffects.None,
                        0f
                    );
                    
                    _spriteBatch.Draw(
                        _circleTexture,
                        leaf3,
                        null,
                        Color.White,
                        0f,
                        new Vector2(_circleTexture.Width / 2, _circleTexture.Height / 2),
                        2f / _circleTexture.Width,
                        SpriteEffects.None,
                        0f
                    );
                    
                    _spriteBatch.Draw(
                        _circleTexture,
                        leaf4,
                        null,
                        Color.White,
                        0f,
                        new Vector2(_circleTexture.Width / 2, _circleTexture.Height / 2),
                        2f / _circleTexture.Width,
                        SpriteEffects.None,
                        0f
                    );
                    break;
            }
        }
    }
}

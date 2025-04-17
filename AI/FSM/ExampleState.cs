using System;
using Microsoft.Xna.Framework;

namespace GameAIDemo.AI.FSM
{
    // FSMの使用例を示すサンプル状態クラス
    public class IdleState : IState
    {
        private readonly Entity owner;
        private float idleTimer = 0f;
        private const float MAX_IDLE_TIME = 2.0f; // 2秒間アイドル状態を維持

        public IdleState(Entity owner)
        {
            this.owner = owner;
        }

        public void Enter()
        {
            Console.WriteLine("アイドル状態に入りました");
            // タイマーをリセット
            idleTimer = 0f;
        }

        public void Update(GameTime gameTime)
        {
            // アイドル中の処理
            // 一定時間経過したら巡回状態に遷移
            idleTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (idleTimer >= MAX_IDLE_TIME)
            {
                owner.TriggerEvent("Patrol");
            }
        }

        public void Exit()
        {
            Console.WriteLine("アイドル状態から出ました");
        }
    }

    public class PatrolState : IState
    {
        private readonly Entity owner;
        private Vector2 targetPosition;
        private float patrolSpeed = 50f;
        private Random rand = new Random();

        public PatrolState(Entity owner)
        {
            this.owner = owner;
        }

        public void Enter()
        {
            Console.WriteLine("巡回状態に入りました");
            // 次の巡回地点を設定
            SetNewPatrolPoint();
        }

        public void Update(GameTime gameTime)
        {
            // 巡回ポイントに向かって移動
            Vector2 direction = Vector2.Normalize(targetPosition - owner.Position);
            owner.Position += direction * patrolSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;

            // 目標地点に近づいたら新しい巡回地点を設定
            if (Vector2.Distance(owner.Position, targetPosition) < 10f)
            {
                SetNewPatrolPoint();
            }
        }

        public void Exit()
        {
            Console.WriteLine("巡回状態から出ました");
        }

        private void SetNewPatrolPoint()
        {
            // 現在位置から適度な距離の新しい巡回地点を設定（画面内に収まるように）
            int screenWidth = 1280;  // 実際の画面幅に合わせて調整
            int screenHeight = 720;  // 実際の画面高さに合わせて調整
            int margin = 50;  // 画面端からのマージン
            
            // 現在位置から100〜300ピクセル範囲内のランダムな点を選択
            float angle = (float)(rand.NextDouble() * Math.PI * 2); // ランダムな角度
            float distance = rand.Next(100, 300); // ランダムな距離
            
            float newX = owner.Position.X + (float)Math.Cos(angle) * distance;
            float newY = owner.Position.Y + (float)Math.Sin(angle) * distance;
            
            // 画面内に収める
            newX = MathHelper.Clamp(newX, margin, screenWidth - margin);
            newY = MathHelper.Clamp(newY, margin, screenHeight - margin);
            
            targetPosition = new Vector2(newX, newY);
        }
    }

    public class ChaseState : IState
    {
        private readonly Entity owner;
        private readonly Entity target;
        private float chaseSpeed = 100f;

        public ChaseState(Entity owner, Entity target)
        {
            this.owner = owner;
            this.target = target;
        }

        public void Enter()
        {
            Console.WriteLine("追跡状態に入りました");
            // 追跡のアニメーションや効果音など
        }

        public void Update(GameTime gameTime)
        {
            if (target == null) return;

            // ターゲットに向かって移動
            Vector2 direction = Vector2.Normalize(target.Position - owner.Position);
            owner.Position += direction * chaseSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        public void Exit()
        {
            Console.WriteLine("追跡状態から出ました");
        }
    }

    // FSMの使用例を示すためのエンティティクラス
    public class Entity
    {
        public Vector2 Position { get; set; }
        public string Name { get; set; }
        private StateMachine stateMachine;
        private static Entity player;

        public Entity(string name, Vector2 position)
        {
            Name = name;
            Position = position;
            InitializeStateMachine();
        }

        // プレイヤーの位置を更新するメソッド（FSMAgentから呼び出す）
        public static void UpdatePlayerPosition(Vector2 position)
        {
            if (player != null)
            {
                player.Position = position;
            }
        }

        private void InitializeStateMachine()
        {
            stateMachine = new StateMachine();
            
            // 状態を追加
            stateMachine.AddState(new IdleState(this));
            stateMachine.AddState(new PatrolState(this));
            
            // プレイヤーを想定したターゲット
            if (Name != "Player")
            {
                // プレイヤーがまだ作成されていない場合は作成
                if (player == null)
                {
                    player = new Entity("Player", new Vector2(400, 300));
                }
                stateMachine.AddState(new ChaseState(this, player));

                // 状態間の遷移を設定
                // アイドル状態から「Patrol」イベントが発生すると巡回状態に遷移
                stateMachine.AddTransition<IdleState, PatrolState>("Patrol", () => true);
                
                // 巡回状態から「PlayerDetected」イベントが発生すると追跡状態に遷移
                stateMachine.AddTransition<PatrolState, ChaseState>("PlayerDetected", () => true);
                
                // 追跡状態から「PlayerLost」イベントが発生するとアイドル状態に戻る
                stateMachine.AddTransition<ChaseState, IdleState>("PlayerLost", () => true);

                // 自動遷移の例: プレイヤーが近くにいたら自動的に追跡状態に遷移
                stateMachine.AddTransition<PatrolState, ChaseState>("Auto", () => 
                    Vector2.Distance(Position, player.Position) < 100f);
            }

            // 初期状態を設定
            stateMachine.SetState<IdleState>();
        }

        public void Update(GameTime gameTime)
        {
            // FSMの更新
            stateMachine.Update(gameTime);
        }

        // イベントをトリガーするメソッド
        public void TriggerEvent(string eventName)
        {
            stateMachine.TriggerEvent(eventName);
        }

        // 現在の状態を確認するメソッド
        public string GetCurrentStateName()
        {
            return stateMachine.GetCurrentState()?.GetType().Name ?? "None";
        }
    }
} 
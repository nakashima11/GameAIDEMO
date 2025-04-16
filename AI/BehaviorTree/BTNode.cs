using System;
using System.Collections.Generic;

namespace GameAIDemo.AI.BehaviorTree
{
    public enum BTNodeStatus
    {
        Success,   // ノードの処理が成功した
        Failure,   // ノードの処理が失敗した
        Running    // ノードの処理が進行中
    }

    // ビヘイビアツリーの基本ノードクラス
    public abstract class BTNode
    {
        public string Name { get; set; }
        
        protected BTNode(string name)
        {
            Name = name;
        }
        
        // ノードを実行する抽象メソッド
        public abstract BTNodeStatus Execute();
    }

    // 複合ノード（子ノードを持つノード）の基本クラス
    public abstract class BTCompositeNode : BTNode
    {
        protected List<BTNode> Children { get; set; }
        
        protected BTCompositeNode(string name) : base(name)
        {
            Children = new List<BTNode>();
        }
        
        public void AddChild(BTNode child)
        {
            Children.Add(child);
        }
    }

    // シーケンスノード（子ノードを順番に実行し、1つでも失敗すると失敗を返す）
    public class BTSequenceNode : BTCompositeNode
    {
        private int _currentChild = 0;
        
        public BTSequenceNode(string name) : base(name) { }
        
        public override BTNodeStatus Execute()
        {
            // 現在の子がない場合は成功を返す
            if (Children.Count == 0)
                return BTNodeStatus.Success;
            
            // 前回実行中だった子から再開
            while (_currentChild < Children.Count)
            {
                BTNodeStatus status = Children[_currentChild].Execute();
                
                if (status == BTNodeStatus.Running)
                {
                    return BTNodeStatus.Running;
                }
                else if (status == BTNodeStatus.Failure)
                {
                    // 子が失敗したら、シーケンス全体が失敗
                    _currentChild = 0;
                    return BTNodeStatus.Failure;
                }
                
                // 子が成功したら次の子へ
                _currentChild++;
            }
            
            // すべての子が成功
            _currentChild = 0;
            return BTNodeStatus.Success;
        }
    }

    // セレクターノード（子ノードを順番に実行し、1つでも成功すると成功を返す）
    public class BTSelectorNode : BTCompositeNode
    {
        private int _currentChild = 0;
        
        public BTSelectorNode(string name) : base(name) { }
        
        public override BTNodeStatus Execute()
        {
            // 現在の子がない場合は失敗を返す
            if (Children.Count == 0)
                return BTNodeStatus.Failure;
            
            // 前回実行中だった子から再開
            while (_currentChild < Children.Count)
            {
                BTNodeStatus status = Children[_currentChild].Execute();
                
                if (status == BTNodeStatus.Running)
                {
                    return BTNodeStatus.Running;
                }
                else if (status == BTNodeStatus.Success)
                {
                    // 子が成功したら、セレクター全体が成功
                    _currentChild = 0;
                    return BTNodeStatus.Success;
                }
                
                // 子が失敗したら次の子へ
                _currentChild++;
            }
            
            // すべての子が失敗
            _currentChild = 0;
            return BTNodeStatus.Failure;
        }
    }

    // 条件ノード（条件を評価し、trueなら成功、falseなら失敗を返す）
    public class BTConditionNode : BTNode
    {
        private Func<bool> _condition;
        
        public BTConditionNode(string name, Func<bool> condition) : base(name)
        {
            _condition = condition;
        }
        
        public override BTNodeStatus Execute()
        {
            return _condition() ? BTNodeStatus.Success : BTNodeStatus.Failure;
        }
    }

    // アクションノード（特定の動作を実行する）
    public class BTActionNode : BTNode
    {
        private Func<BTNodeStatus> _action;
        
        public BTActionNode(string name, Func<BTNodeStatus> action) : base(name)
        {
            _action = action;
        }
        
        public override BTNodeStatus Execute()
        {
            return _action();
        }
    }

    // インバーターノード（子ノードの結果を反転させる）
    public class BTInverterNode : BTNode
    {
        private BTNode _child;
        
        public BTInverterNode(string name, BTNode child) : base(name)
        {
            _child = child;
        }
        
        public override BTNodeStatus Execute()
        {
            BTNodeStatus status = _child.Execute();
            
            if (status == BTNodeStatus.Success)
                return BTNodeStatus.Failure;
            else if (status == BTNodeStatus.Failure)
                return BTNodeStatus.Success;
            
            return status; // Running状態は変更しない
        }
    }

    // パラレルノード（すべての子を同時に実行）
    public class BTParallelNode : BTCompositeNode
    {
        public enum Policy
        {
            RequireOne,  // 1つでも成功すれば成功
            RequireAll   // すべて成功する必要がある
        }
        
        private Policy _successPolicy;
        private Policy _failurePolicy;
        
        public BTParallelNode(string name, Policy successPolicy, Policy failurePolicy) : base(name)
        {
            _successPolicy = successPolicy;
            _failurePolicy = failurePolicy;
        }
        
        public override BTNodeStatus Execute()
        {
            int successCount = 0;
            int failureCount = 0;
            
            foreach (BTNode child in Children)
            {
                BTNodeStatus status = child.Execute();
                
                if (status == BTNodeStatus.Success)
                {
                    successCount++;
                    
                    // RequireOneポリシーでは1つの成功で成功
                    if (_successPolicy == Policy.RequireOne)
                        return BTNodeStatus.Success;
                }
                else if (status == BTNodeStatus.Failure)
                {
                    failureCount++;
                    
                    // RequireOneポリシーでは1つの失敗で失敗
                    if (_failurePolicy == Policy.RequireOne)
                        return BTNodeStatus.Failure;
                }
            }
            
            // RequireAllポリシーでの判定
            if (_successPolicy == Policy.RequireAll && successCount == Children.Count)
                return BTNodeStatus.Success;
            
            if (_failurePolicy == Policy.RequireAll && failureCount == Children.Count)
                return BTNodeStatus.Failure;
            
            return BTNodeStatus.Running;
        }
    }
} 
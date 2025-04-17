using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace GameAIDemo.AI.FSM
{
    // 状態のインターフェース
    public interface IState
    {
        void Enter();
        void Update(GameTime gameTime);
        void Exit();
    }

    // 有限状態マシン (FSM) の Mode 3 実装
    // Mode 3: 階層型FSMとイベント駆動型の組み合わせ
    public class StateMachine
    {
        private IState currentState;
        private IState previousState;
        private Dictionary<Type, IState> states = new Dictionary<Type, IState>();
        private Dictionary<string, Func<bool>> transitions = new Dictionary<string, Func<bool>>();
        private List<StateMachine> subStateMachines = new List<StateMachine>();
        private StateMachine parentStateMachine;

        public StateMachine(StateMachine parent = null)
        {
            parentStateMachine = parent;
            if (parent != null)
            {
                parent.AddSubStateMachine(this);
            }
        }

        public void AddState(IState state)
        {
            states[state.GetType()] = state;
        }

        public void SetState<T>() where T : IState
        {
            var type = typeof(T);
            if (!states.ContainsKey(type))
            {
                throw new InvalidOperationException($"状態 {type.Name} は登録されていません");
            }

            if (currentState != null)
            {
                currentState.Exit();
                previousState = currentState;
            }

            currentState = states[type];
            currentState.Enter();
        }

        public void AddTransition<TFrom, TTo>(string eventName, Func<bool> condition)
            where TFrom : IState
            where TTo : IState
        {
            string key = $"{typeof(TFrom).Name}_{eventName}_{typeof(TTo).Name}";
            transitions[key] = condition;
        }

        public void TriggerEvent(string eventName)
        {
            if (currentState == null) return;

            string prefix = $"{currentState.GetType().Name}_{eventName}_";
            foreach (var transition in transitions)
            {
                if (transition.Key.StartsWith(prefix) && transition.Value())
                {
                    string stateName = transition.Key.Substring(prefix.Length);
                    Type stateType = Type.GetType($"GameAIDemo.AI.FSM.{stateName}");
                    if (stateType != null && states.ContainsKey(stateType))
                    {
                        currentState.Exit();
                        previousState = currentState;
                        currentState = states[stateType];
                        currentState.Enter();
                        return;
                    }
                }
            }

            // サブステートマシンにイベントを伝播
            foreach (var subFSM in subStateMachines)
            {
                subFSM.TriggerEvent(eventName);
            }
        }

        public void RevertToPreviousState()
        {
            if (previousState != null)
            {
                currentState.Exit();
                currentState = previousState;
                currentState.Enter();
            }
        }

        public void Update(GameTime gameTime)
        {
            currentState?.Update(gameTime);

            // サブステートマシンの更新
            foreach (var subFSM in subStateMachines)
            {
                subFSM.Update(gameTime);
            }

            // 自動遷移の確認
            CheckAutomaticTransitions();
        }

        private void CheckAutomaticTransitions()
        {
            if (currentState == null) return;

            string prefix = $"{currentState.GetType().Name}_Auto_";
            foreach (var transition in transitions)
            {
                if (transition.Key.StartsWith(prefix) && transition.Value())
                {
                    string stateName = transition.Key.Substring(prefix.Length);
                    Type stateType = Type.GetType($"GameAIDemo.AI.FSM.{stateName}");
                    if (stateType != null && states.ContainsKey(stateType))
                    {
                        currentState.Exit();
                        previousState = currentState;
                        currentState = states[stateType];
                        currentState.Enter();
                        return;
                    }
                }
            }
        }

        private void AddSubStateMachine(StateMachine subFSM)
        {
            subStateMachines.Add(subFSM);
        }

        public IState GetCurrentState()
        {
            return currentState;
        }

        public bool IsInState<T>() where T : IState
        {
            return currentState?.GetType() == typeof(T);
        }
    }
} 
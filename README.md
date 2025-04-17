# GameAIDemo

ゲームAI技術のデモンストレーションプロジェクト。AIアルゴリズムを視覚的に体験できるインタラクティブなアプリケーションです。

## 概要

このプロジェクトは、ゲーム開発において重要なAI技術を学習・理解するために作られたデモアプリケーションです。現在、以下の3つのAIモードを実装しています：

1. **A*パスファインディング**（モード1）：障害物を避けながら目標に最短経路で到達するアルゴリズム
2. **ビヘイビアツリー**（モード2）：階層的な意思決定システムを使った複雑なAI行動モデル
3. **有限状態マシン（FSM）**（モード3）：状態遷移に基づく明確なAI行動管理システム

## システム要件

- .NET 8.0以上
- 対応OS：Windows, macOS, Linux
- グラフィックカード：基本的なグラフィック処理が可能なもの
- メモリ：最低2GB

## インストール方法

1. リポジトリをクローン：
```
git clone https://github.com/yourusername/GameAIDemo.git
```

2. プロジェクトディレクトリに移動：
```
cd GameAIDemo
```

3. アプリケーションをビルド：
```
dotnet build
```

4. アプリケーションを実行：
```
dotnet run
```

## 操作方法

### 基本操作
- **WASD / 矢印キー**：プレイヤーの移動
- **1**：A*パスファインディングモードに切り替え
- **2**：ビヘイビアツリーモードに切り替え 
- **3**：有限状態マシン（FSM）モードに切り替え
- **R**：AIエージェントの位置をリセット
- **H**：ヘルプ表示の切り替え
- **ESC**：ゲーム終了

### プレイヤー
白い円がプレイヤーです。WASDまたは矢印キーで自由に移動できます。障害物に接触するとプレイヤーは反発します。

### 障害物
灰色の四角形は障害物です。

## AIモードの詳細

### モード1：A*パスファインディング
**特徴**：
- 赤色のエージェント
- グリッド上のセルを利用した経路探索
- 障害物を最適に回避してプレイヤーを追跡
- グリッドベースの移動パターン

A*アルゴリズムはヒューリスティックを使った効率的な経路探索アルゴリズムです。エージェントはプレイヤーへの最短経路を常に計算し直し、障害物を回避しながら追跡します。

### モード2：ビヘイビアツリー
**特徴**：
- オレンジ色のエージェント
- 階層的な意思決定システム
- 複数の行動パターン（追跡、攻撃、逃走、うろつきなど）
- 体力に応じた行動変化
- 追跡時は障害物を無視して直進

ビヘイビアツリーは階層的な意思決定システムで、エージェントの状況に応じて異なる行動を実行します：
- **うろつき**：プレイヤーが検出範囲外の場合、ランダムに移動
- **追跡**：プレイヤーを発見すると直線的に追跡（障害物を無視）
- **攻撃**：プレイヤーに十分近づくと攻撃状態に移行

### モード3：有限状態マシン（FSM）
**特徴**：
- 状態に応じた色の変化（黄色：アイドル状態、緑色：巡回状態、赤色：追跡状態）
- 明確な視覚的な検知範囲表示（青色の円）
- 階層型とイベント駆動型を組み合わせたMode 3実装
- プレイヤーとの距離に基づく状態遷移

有限状態マシン（FSM）は、エージェントが明確に定義された状態間を遷移する単純かつ効果的なAI設計手法です：
- **アイドル状態**（黄色）：短時間停止後、自動的に巡回状態に移行
- **巡回状態**（緑色）：ランダムなポイント間を巡回
- **追跡状態**（赤色）：プレイヤーを検知すると直接追跡

エージェントはプレイヤーとの距離に基づいて状態を変化させます。内側の検知円（150ピクセル）内にプレイヤーが入ると追跡を開始し、外側の円（250ピクセル）より遠ざかると追跡を終了します。

## 技術的詳細

### プロジェクト構造
- **Entities/**: ゲーム内のエンティティ（プレイヤー、障害物など）
- **Utilities/**: ユーティリティクラス（グリッドマップなど）
- **AI/**: AIアルゴリズムの実装
  - **AStar/**: A*パスファインディングの実装
  - **BehaviorTree/**: ビヘイビアツリーの実装
  - **FSM/**: 有限状態マシンの実装

### 使用技術
- **MonoGame**: ゲームフレームワーク
- **C#**: プログラミング言語
- **.NET Core**: フレームワーク

## 新卒就活用：実装AI技術の詳細解説

このセクションでは、ゲーム開発において重要なAI技術を実装するにあたっての技術的な背景、工夫点、実用性について詳しく解説します。

### 1. A*パスファインディングアルゴリズム

**理論的背景**:
- A*（エースター）は、1968年に発表された最良優先探索のアルゴリズムです
- ダイクストラ法と貪欲法のハイブリッド手法として考案されました
- ヒューリスティック関数（推測関数）を使用して探索効率を高めています
- 評価関数 f(n) = g(n) + h(n) を用いて最適なパスを選択します
  - g(n): スタートからノードnまでの実コスト
  - h(n): ノードnからゴールまでの推定コスト（ヒューリスティック）

**実装の工夫点**:
- 空間的効率のためにグリッドベースのマップを使用（動的なマップ生成）
- オープンリストとクローズドリストを効率的に管理するためのデータ構造設計
- マンハッタン距離をベースにしたヒューリスティック関数の実装
- 障害物回避ロジックの最適化（対角線移動の考慮）
- 再計算タイミングの最適化（プレイヤー位置が大きく変わった時のみ）
- パフォーマンス維持のための計算負荷管理

**ゲーム開発での実用性**:
- NPCの経路探索（敵キャラクターの移動制御）
- RTS（リアルタイムストラテジー）ゲームでのユニット移動指示
- オープンワールドでの最短経路計算
- RPGやアドベンチャーゲームのクエスト自動誘導
- 戦術的AI実装のベースとなる技術

**アピールポイント**:
- 複雑なアルゴリズムの理解と実装能力
- 効率化とパフォーマンスチューニングの経験
- データ構造（ヒープ、優先度キュー）の実践的活用
- 空間的思考と問題解決アプローチ
- ゲーム開発における実用的なAI技術の実装経験

### 2. ビヘイビアツリー

**理論的背景**:
- ビヘイビアツリーは2000年代初頭から現代のゲームAIで広く採用されている設計パターン
- Halo（Bungie社）など多くのAAA級タイトルで実装されている技術
- 階層的なノード構造で複雑な行動を表現
- 基本ノードタイプ：
  - シーケンス（Sequence）：子ノードを順に実行
  - セレクタ（Selector）：子ノードを条件に応じて選択
  - パラレル（Parallel）：複数のノードを並行実行
  - デコレータ（Decorator）：ノードの動作を修飾

**実装の工夫点**:
- コンポジットパターンによる拡張性の高い設計
- 再利用可能なビヘイビアノードのモジュール化
- 条件チェックと行動実行の分離による柔軟な状態管理
- 複数レベルの意思決定ロジックを階層化
- 動的な優先度に基づいた行動選択
- 状態遷移のスムーズな処理（アニメーション連携）

**ゲーム開発での実用性**:
- 敵AI行動パターンの多様化
- NPC（ノンプレイヤーキャラクター）の自律的行動設計
- 状況認識と複雑な判断の実装
- 拡張性の高いAI設計（新規行動の追加が容易）
- 大規模ゲームでの複数エージェント管理

**アピールポイント**:
- オブジェクト指向設計原則の深い理解と実装
- デザインパターン（コンポジット、オブザーバー）の実践的適用
- 再利用可能なコンポーネント設計能力
- 複雑な行動ロジックのモデリングスキル
- 業界標準のゲームAI手法の理解と実装経験

### 3. 有限状態マシン（FSM）

**理論的背景**:
- 有限状態マシンは計算理論の基本概念で、ゲームAIの基礎技術として広く使用
- 1950年代から理論的に確立された古典的アルゴリズム
- 明確に定義された状態（State）と遷移（Transition）の集合で構成
- 各状態は入力に応じた振る舞いと遷移条件を持つ
- FSMの種類：階層型、イベント駆動型、確率的FSMなど

**実装の工夫点**:
- ステートパターンによるクリーンな状態実装
- イベント駆動型遷移と条件ベース遷移の併用
- 階層型FSMの実装による複雑な状態管理
- 状態間データの共有メカニズム
- 条件評価の最適化（不要な評価回避）
- デバッグ向け視覚的フィードバック（状態の色による表現）

**ゲーム開発での実用性**:
- キャラクターの基本AI（待機、巡回、攻撃など）
- ゲームステージの進行管理
- UIの状態管理（メニュー画面遷移など）
- 物理オブジェクトの状態制御
- 敵の行動パターン設計
- インタラクティブな環境オブジェクト

**アピールポイント**:
- 状態設計と遷移ロジックの体系的モデリング能力
- ステートパターン等のデザインパターンの実践的理解
- イベント駆動型プログラミングの経験
- 可読性と保守性を考慮したコード設計能力
- 複雑なシステムを論理的に分解する思考力

## まとめ：ゲームAI技術の統合的理解

このプロジェクトでは、ゲーム開発における3つの代表的なAI技術を実装・可視化しました。各技術には長所と短所があり、用途に応じた適切な選択と組み合わせが重要です：

- **A***: パス探索に特化した効率的なアルゴリズム。最適な経路を見つけるが、複雑な行動パターンには不向き。
- **ビヘイビアツリー**: 複雑な行動パターンを階層的に表現できる柔軟なシステム。設計が複雑化する可能性がある。
- **FSM**: シンプルで理解しやすい状態管理。状態数が増えると管理が難しくなる場合がある。

実践的なゲーム開発では、これらの技術を状況に応じて適切に組み合わせることで、より洗練されたゲームAIを実現できます。このプロジェクトは、それらの技術を個別に理解し、統合的に活用するための基盤となります。

## 参考文献

### A*パスファインディング
1. Hart, P. E., Nilsson, N. J., & Raphael, B. (1968). "A Formal Basis for the Heuristic Determination of Minimum Cost Paths". *IEEE Transactions on Systems Science and Cybernetics*, 4(2), 100-107.
2. Millington, I., & Funge, J. (2009). *Artificial Intelligence for Games* (2nd ed.). Morgan Kaufmann Publishers.
3. Patel, A. (2021). "Introduction to the A* Algorithm" [Online]. Available: https://www.redblobgames.com/pathfinding/a-star/introduction.html
4. Cui, X., & Shi, H. (2011). "A*-based Pathfinding in Modern Computer Games". *International Journal of Computer Science and Network Security*, 11(1), 125-130.
5. Sturtevant, N. R. (2012). "Benchmarks for Grid-Based Pathfinding". *IEEE Transactions on Computational Intelligence and AI in Games*, 4(2), 144-148.

### ビヘイビアツリー
1. Isla, D. (2005). "Handling Complexity in the Halo 2 AI". *Game Developers Conference*.
2. Champandard, A. J. (2007). "Behavior Trees for Next-Gen Game AI". *AIGameDev.com*. 
3. Colledanchise, M., & Ögren, P. (2018). *Behavior Trees in Robotics and AI: An Introduction*. CRC Press.
4. Marzinotto, A., Colledanchise, M., Smith, C., & Ögren, P. (2014). "Towards a Unified Behavior Trees Framework for Robot Control". *IEEE International Conference on Robotics and Automation (ICRA)*, 5420-5427.
5. Simpson, C. (2014). "Behavior Trees for AI: How They Work" [Online]. Available: https://www.gamedeveloper.com/programming/behavior-trees-for-ai-how-they-work
6. Shoulson, A., & Badler, N. I. (2011). "POSH: A Framework for Behavior Trees in Games with Scripted Control". *Journal of Visual Languages & Computing*, 22(6), 369-381.

### 有限状態マシン（FSM）
1. Hopcroft, J. E., Motwani, R., & Ullman, J. D. (2006). *Introduction to Automata Theory, Languages, and Computation* (3rd ed.). Pearson.
2. Buckland, M. (2005). *Programming Game AI by Example*. Wordware Publishing.
3. Rabin, S. (Ed.). (2013). *Game AI Pro: Collected Wisdom of Game AI Professionals*. CRC Press.
4. Dill, K. (2013). "A Game AI Approach to Autonomous Control of Virtual Characters". *Game Developers Conference*.
5. Fu, D., & Houlette, R. (2004). "The Ultimate Guide to FSMs in Games". In S. Rabin (Ed.), *AI Game Programming Wisdom 2* (pp. 283-302). Charles River Media.
6. Kienzle, J., Duala-Ekoko, E., & Gélineau, S. (2009). "Designing and Implementing a FSM-Based Object Behavior Specification Language". *IEEE International Conference on Software Engineering and Formal Methods*, 297-306.

### 総合的なゲームAI
1. Russell, S., & Norvig, P. (2020). *Artificial Intelligence: A Modern Approach* (4th ed.). Pearson.
2. Yannakakis, G. N., & Togelius, J. (2018). *Artificial Intelligence and Games*. Springer.
3. Rabin, S. (Ed.). (2002-2019). *AI Game Programming Wisdom* (Vol. 1-4). Charles River Media.
4. Schwab, B. (2014). *AI Game Engine Programming* (3rd ed.). Course Technology PTR.

## ライセンス

MITライセンス 


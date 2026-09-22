# DodgeRunner

Unity 6000.6.2f1 製の WebGL ミニゲーム（3D ドッジ・ランナー）。自動前進するキューブを 3 レーン間で動かし、ジャンプで赤い障害物を避けながら走行距離を伸ばします。このリポジトリはハッカソン作品で、主題は「AI（Claude Code Fable 5.1）が公式 Unity CLI と Pipeline パッケージでライブ Editor を駆動し、外部アセット無しにすべてコード生成でゲームを作る」ことです。

## 操作方法

| キー | 動作 |
| --- | --- |
| Space / Enter | ゲーム開始 |
| A / D または ← / → | 左右レーン移動 |
| Space | ジャンプ |
| R | GAME OVER 後にリトライ |

スコアは走行距離。赤い障害物に当たると GAME OVER です。

## 必要環境

- Unity 6000.6.2f1（WebGL Build Support モジュール込み）
- Unity CLI 1.0.0-beta.8（`~/.unity/bin/unity`）と `com.unity.pipeline` パッケージ
- [just](https://github.com/casey/just)
- Python 3（ローカル配信用）

## 開発フロー

```sh
just setup        # Pipeline パッケージを導入（初回のみ）
just open         # Editor を起動
just build-scene  # SceneBuilder でシーンを生成
just play         # Play モードで動作確認
just build-web    # Editor を閉じて WebGL をビルド（Build/WebGL）
just serve        # http://localhost:8080 で配信
```

その他: `just status`（Editor 状態）、`just screenshot`（Game ビュー撮影）、`just test`（EditMode テスト）。一覧は `just` で表示できます。

## ディレクトリ構成

```
.
├── README.md
├── justfile                  # Unity CLI 操作のコマンド集
├── Build/WebGL/              # WebGL ビルド出力（gitignore 済み）
└── game/                     # Unity プロジェクト（URP Blank テンプレート）
    └── Assets/Scripts/
        ├── GameManager.cs    # 状態遷移（Ready / Playing / GameOver）とスコア
        ├── PlayerController.cs
        ├── ObstacleSpawner.cs
        ├── TrackScroller.cs
        ├── HudController.cs
        └── Editor/
            ├── SceneBuilder.cs  # シーンをコードから生成
            └── Builder.cs       # WebGL バッチビルド
```

## AI 駆動の開発フロー

このプロジェクトは Unity 公式の Claude Code プラグイン `unity@unity-agent-plugin` を使い、AI がライブ Editor を直接操作して開発しました。

- `unity open` で起動した Editor に対し、`unity command eval` で C# を即時実行してシーンを生成・修正
- `unity command editor_play` と `unity command screenshot` で動作確認し、結果を見てコードを反復
- `unity build` で Editor を閉じたバッチモードから WebGL ビルドを実行

シーンやプレハブは手作業で作らず、すべて `SceneBuilder.Build()` が生成するため、変更履歴が C# の diff として残ります。

## TODO

- EditMode テストの拡充（ゲームロジックの単体テスト）
- lefthook（gitleaks / pinact / `just --fmt --check`）の導入
- nix flake + direnv による開発環境の再現
- `knowledge/`（OKF）の導入と開発知見の記録
- GitHub Actions による CI（テストと WebGL ビルド）

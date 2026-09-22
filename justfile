set shell := ["bash", "-cu"]

export PATH := env_var("HOME") + "/.unity/bin:" + env_var("PATH")

project := justfile_directory() + "/game"

# レシピ一覧を表示する
default:
    just --list

# Unity Pipeline パッケージをプロジェクトへ導入する（初回のみ）
setup:
    unity pipeline install --project-path {{ project }}

# Unity Editor でプロジェクトを開く（以降のライブ操作の前提）
open:
    unity open {{ project }}

# 起動中の Editor の状態を JSON で確認する
status:
    unity status --format json

# SceneBuilder でシーンをコードから再生成する（Editor 起動中に実行）
build-scene:
    unity command eval --project-path {{ project }} 'DodgeRunner.EditorTools.SceneBuilder.Build(); return "ok";'

# Editor の Play モードを切り替える
play:
    unity command editor_play --project-path {{ project }}

# Game ビューのスクリーンショットを shot.png に保存する
screenshot:
    unity command screenshot --project-path {{ project }} --output shot.png --width 1280 --height 720

# EditMode テストを実行し、結果を test-results.xml に出力する
test:
    unity test {{ project }} --mode EditMode --output test-results.xml

# Editor を閉じてから WebGL をバッチビルドする（出力先: Build/WebGL）
build-web:
    unity close {{ project }} || true
    unity build {{ project }} --target WebGL --execute-method DodgeRunner.EditorTools.Builder.BuildWebGL -o {{ justfile_directory() }}/Build/WebGL --timeout 1500

# WebGL ビルドをローカル配信する（http://localhost:8080）
serve:
    python3 -m http.server 8080 --directory {{ justfile_directory() }}/Build/WebGL

# WebGL ビルド成果物を dist/ にコピーする（GitHub Pages のデプロイ対象。commit して push すると配信される）
dist:
    rm -rf {{ justfile_directory() }}/dist
    cp -R {{ justfile_directory() }}/Build/WebGL {{ justfile_directory() }}/dist
    touch {{ justfile_directory() }}/dist/.nojekyll

# MoreFunMultiplay

Beat Saber のマルチプレイヤー（Multiplayer+ / BeatSaberPlus）をもっと楽しく遊ぶための、独自勝負種目・ランキング拡張MODです。  
通常のスコア競争とは一味違う、運動量対決や地蔵プレイ、チキンレースなどの特殊ルールでマルチプレイを楽しめます。

さらに、**リアルタイム WebViewer** を搭載しており、OBS等の配信ソフトやブラウザからランキング状況をオーバーレイ表示できます。

---

## 🎮 主なゲームモード

1. **運動量 (Sweat)**
   - プレイ中のヘッドセットやコントローラーの移動距離・加速度をリアルタイム計測。
   - 単にスコアを競うのではなく、「最も身体を動かして汗をかいたプレイヤー」をランキング！
2. **地蔵 (Jizo)**
   - 運動量とは逆に、「いかに頭や体を動かさず、最小限の動きでノーツを叩けたか」を競う忍耐ルール。
3. **チキンレース (Chicken Race)**
   - 設定した目標スコア（例: 80%）に「最も近く、かつ超えない（または最も近い）」絶妙なスコアコントロールを競う心理戦ルール。

---

## 📺 リアルタイム WebViewer（OBSオーバーレイ）

- 曲終了時の結果データやルーム内の対戦状況をリアルタイムでブラウザに配信可能。
- `UserData/MoreFunMultiplay_WebViewer.html` をOBSの「ブラウザ」ソースやChrome等で開くだけで、配信画面にスタイリッシュな対戦リザルトをオーバーレイ表示できます。

---

## 🔧 前提MOD
- [BSIPA](https://github.com/beat-saber-modding-group/BeatSaber-IPA-Reloaded) (4.2.0+)
- [BeatSaberMarkupLanguage](https://github.com/monkeymanboy/BeatSaberMarkupLanguage) (BSML 1.6.0+)
- Multiplayer+ または BeatSaberPlus_Multiplayer

---

## 📦 インストール方法
1. 最新リリースの `MoreFunMultiplay_v0.3.0.zip` をダウンロードします。
2. 解凍した `Plugins` および `UserData` フォルダを、Beat Saber のインストール先フォルダへ上書きコピーしてください。

---

## ⚙️ 設定
- ゲーム内のGameplay Setup画面、またはMod Settings画面から、勝負種目（運動量 / 地蔵 / チキンレース）や目標パーセンテージの変更が可能です。
- リザルト用フローティングスクリーンの位置・角度はドラッグして自由に変更・保存できます。

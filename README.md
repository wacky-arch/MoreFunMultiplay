# MoreFunMultiplay

Beat Saber のマルチプレイヤー（Multiplayer+ / BeatSaberPlus）をもっと楽しく遊ぶための、独自勝負種目・ランキング拡張MODです。  
通常のスコア競争とは一味違う、運動量対決や地蔵プレイ、チキンレースなどの特殊ルールでマルチプレイを楽しめます。  
さらに、**リアルタイム WebViewer** を搭載しており、OBS等の配信ソフトやブラウザからランキング状況をオーバーレイ表示できます。

A custom game mode and ranking extension mod for Beat Saber multiplayer (Multiplayer+ / BeatSaberPlus).  
Enjoy unique party modes such as Sweat (movement/calories), Jizo (stillness challenge), and Chicken Race (target score precision).  
Includes a **Real-time WebViewer** to overlay live match standings in OBS or web browsers.

---

> [!WARNING]
> ### 免責事項 / Disclaimer
> 
> **日本語:**  
> 本MODは wacky が個人的に楽しむために作成・カスタマイズしているものです。  
> そのため、他のPC環境や将来のBeat Saberバージョンでの動作は保証できません。  
> 導入およびご利用はすべて自己責任にてお願いいたします。
> 
> **English:**  
> This mod is created and maintained by wacky purely for personal enjoyment.  
> As such, proper operation in other environments or future Beat Saber versions is not guaranteed.  
> Please use this mod entirely at your own risk.

---

## 🎮 主なゲームモード / Game Modes

1. **運動量 (Sweat)**
   - **日本語**: プレイ中のヘッドセットやコントローラーの移動距離・加速度をリアルタイム計測。「最も身体を動かして汗をかいたプレイヤー」をランキング！
   - **English**: Measures HMD and controller movements and acceleration in real time. Ranks players by who moved the most and broke a sweat!
2. **地蔵 (Jizo)**
   - **日本語**: 運動量とは逆に、「いかに頭や体を動かさず、最小限の動きでノーツを叩けたか」を競う忍耐ルール。
   - **English**: The opposite of Sweat—a stillness challenge competing for who can hit notes with minimal head and body movement.
3. **チキンレース (Chicken Race)**
   - **日本語**: 設定した目標スコア（例: 80%）に「最も近く、かつ超えない（または最も近い）」絶妙なスコアコントロールを競う心理戦ルール。
   - **English**: A precision score-control match where players aim to get as close as possible to a target score % (e.g. 80%) without exceeding it.

---

## 📺 リアルタイム WebViewer / Real-time WebViewer
- **日本語**: `UserData/MoreFunMultiplay_WebViewer.html` をOBSの「ブラウザ」ソースやChrome等で開くだけで、配信画面に対戦リザルトやルーム状況を美しくオーバーレイ表示できます。常時SSEストリーム接続により低遅延かつ低負荷で同期します。
- **English**: Simply open `UserData/MoreFunMultiplay_WebViewer.html` in an OBS Browser Source or web browser to display stylish live match results and standings. Uses Server-Sent Events (SSE) for low-latency, rate-limit-safe real-time updates.

---

## 🔧 前提MOD / Prerequisites
- [BSIPA](https://github.com/beat-saber-modding-group/BeatSaber-IPA-Reloaded) (4.2.0+)
- [BeatSaberMarkupLanguage](https://github.com/monkeymanboy/BeatSaberMarkupLanguage) (BSML 1.6.0+)
- Multiplayer+ または BeatSaberPlus_Multiplayer

---

## 📦 インストール方法 / Installation
1. `Plugins/MoreFunMultiplay.dll` および `UserData/MoreFunMultiplay_WebViewer.html` を、Beat Saber のインストール先フォルダへ上書きコピーしてください。
2. Copy `Plugins/MoreFunMultiplay.dll` and `UserData/MoreFunMultiplay_WebViewer.html` into your Beat Saber directory.

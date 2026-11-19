# MoreFunMultiplay

Beat Saber のマルチプレイヤー（Multiplayer+ / BeatSaberPlus）をもっと楽しく遊ぶための、独自勝負種目（全5種目）・ランキング拡張MODです。  
通常のスコア競争とは一味違う、運動量対決・地蔵プレイ・チキンレース・シンメトリー・Wild の5種類の特殊ルールでマルチプレイを楽しめます。  
さらに、**リアルタイム WebViewer** を搭載しており、世界中どこからでもブラウザやOBS等の配信ソフトからランキング状況をオーバーレイ表示できます。

A custom game mode (5 modes) and ranking extension mod for Beat Saber multiplayer (Multiplayer+ / BeatSaberPlus).  
Enjoy 5 unique party modes: Sweat, Jizo, Chicken Race, Symmetry, and Wild.  
Includes a **Real-time WebViewer** accessible worldwide via web browser or OBS stream overlays.

---

> [!WARNING]
> ### 免責事項 / Disclaimer
> 
> 本MODは wacky が個人的に楽しむために作成・カスタマイズしているものです。  
> そのため、他のPC環境や将来のBeat Saberバージョンでの動作は保証できません。  
> 導入およびご利用はすべて自己責任にてお願いいたします。
> 
> This mod is created and maintained by wacky purely for personal enjoyment.  
> As such, proper operation in other environments or future Beat Saber versions is not guaranteed.  
> Please use this mod entirely at your own risk.

---

## 📺 リアルタイム WebViewer / Live WebViewer

### 🌐 公開Webビューアー / Hosted Online WebViewer
👉 **[https://mfm-viewer.pages.dev/](https://mfm-viewer.pages.dev/)**

- 上記のWebサイトにアクセスするだけで、**世界中の誰でも**PC、スマートフォン、タブレット等のブラウザからリアルタイムに対戦ランキングやルーム稼働状況を閲覧できます！
- OBS等の配信ソフトで「ブラウザ」ソースを追加し、URLに `https://mfm-viewer.pages.dev/?room=[ルームコード]` を指定すれば、配信画面にスタイリッシュな対戦リザルトを直接オーバーレイ表示できます。
- ローカル環境用として同梱されている `UserData/MoreFunMultiplay_WebViewer.html` を直接開いて利用することも可能です。
- 常時SSE（Server-Sent Events）ストリーム接続により、API制限を回避しつつ超低遅延でリアルタイム同期します。

- Anyone around the world can view live match rankings and active multiplayer rooms simply by visiting **[https://mfm-viewer.pages.dev/](https://mfm-viewer.pages.dev/)** from any browser (PC, mobile, tablet)!
- Easily used as an OBS Browser Source by setting the URL to `https://mfm-viewer.pages.dev/?room=[ROOM_CODE]` for clean, real-time stream overlays.
- The bundled local file `UserData/MoreFunMultiplay_WebViewer.html` is also available for offline/local use.
- Powered by persistent Server-Sent Events (SSE) streaming for instantaneous updates without hitting rate limits.

---

## 🎮 主なゲームモード（全5種目） / Game Modes (5 Modes)

1. **🔥 運動量ランキング (Sweat)**
   - プレイ中のヘッドセットやコントローラーの移動距離・加速度をリアルタイム計測。「最も身体を動かして汗をかいたプレイヤー」をランキング！
   - Measures HMD and controller movements and acceleration in real time. Ranks players by who moved the most and broke a sweat!
2. **🗿 地蔵スタイル賞 (Jizo)**
   - 運動量とは逆に、「いかに頭を動かさず、最小限の動きでノーツを叩けたか」を競う忍耐ルール。
   - The opposite of Sweat—a stillness challenge competing for who can hit notes with minimal head and body movement.
3. **🎯 チキンレース (Chicken Race)**
   - 設定した目標スコア（例: 80%）に「最も近く、かつ超えない（または最も近い）」絶妙なスコアコントロールを競う心理戦ルール。
   - A precision score-control match where players aim to get as close as possible to a target score % (e.g. 80%) without exceeding it.
4. **⚖️ シンメトリー (Symmetry)**
   - 左手と右手の移動距離の均等さ（左右差の小ささ）× 精度% で評価。左右バランス良く綺麗に振って高精度を出したプレイヤーが上位！
   - Evaluates left/right hand movement symmetry combined with accuracy (`Accuracy / (1 + LR Diff%)`). Rewards balanced swings with high accuracy!
5. **💥 Wild**
   - 「クリアした中で最もミスが多いプレイヤー（ギリギリ生還）」が上位になるカオスルール！（※Fail時は高精度なプレイヤーが上位）
   - A chaotic survival mode where the cleared player with the **most misses** wins! (Among failed players, higher accuracy ranks higher.)

---

## 🔧 前提MOD / Prerequisites
- [BSIPA](https://github.com/beat-saber-modding-group/BeatSaber-IPA-Reloaded) (4.2.0+)
- [BeatSaberMarkupLanguage](https://github.com/monkeymanboy/BeatSaberMarkupLanguage) (BSML 1.6.0+)
- Multiplayer+ または BeatSaberPlus_Multiplayer

---

## 📦 インストール方法 / Installation
1. `Plugins/MoreFunMultiplay.dll` および `UserData/MoreFunMultiplay_WebViewer.html` を、Beat Saber のインストール先フォルダへ上書きコピーしてください。
2. Copy `Plugins/MoreFunMultiplay.dll` and `UserData/MoreFunMultiplay_WebViewer.html` into your Beat Saber directory.

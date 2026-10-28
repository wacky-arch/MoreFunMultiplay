using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace MoreFunMultiplay
{
    internal class PluginConfig
    {
        public static PluginConfig Instance { get; set; }

        // 現在選択されている勝負種目 (0:なし, 1:運動量, 2:地蔵, 3:チキンレース)
        public virtual int SelectedGameMode { get; set; } = (int)GameMode.None;

        // ターゲットスコア（パーセント）。デフォルト80%
        public virtual float TargetScorePercentage { get; set; } = 80.0f;

        // リザルト用フローティングスクリーンの位置・回転・折り畳み状態
        public virtual float FloatingPosX { get; set; } = 0.0f;
        public virtual float FloatingPosY { get; set; } = 2.25f;
        public virtual float FloatingPosZ { get; set; } = 2.5f;
        public virtual float FloatingRotX { get; set; } = -12.0f;
        public virtual float FloatingRotY { get; set; } = 0.0f;
        public virtual float FloatingRotZ { get; set; } = 0.0f;
        public virtual bool IsResultsCollapsed { get; set; } = false;
    }
}

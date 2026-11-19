namespace MoreFunMultiplay;

internal class PluginConfig
{
	public static PluginConfig Instance { get; set; }

	public virtual int SelectedGameMode { get; set; }

	public virtual float TargetScorePercentage { get; set; } = 80f;


	public virtual float FloatingPosX { get; set; }

	public virtual float FloatingPosY { get; set; } = 2.25f;


	public virtual float FloatingPosZ { get; set; } = 2.5f;


	public virtual float FloatingRotX { get; set; } = -12f;


	public virtual float FloatingRotY { get; set; }

	public virtual float FloatingRotZ { get; set; }

	public virtual bool IsResultsCollapsed { get; set; }
}

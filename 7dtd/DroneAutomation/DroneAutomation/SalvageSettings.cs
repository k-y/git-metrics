using System.Collections.Generic;

namespace DroneAutomation;

public sealed class SalvageSettings
{
	public float Radius = 4f;

	public float VerticalRadius = 4f;

	public float SecondsPerStep = 1.5f;

	public float MaxCatchupSeconds = 5f;

	public float LowQualityReach = 0.55f;

	public float LowQualityTimeMult = 2f;

	public bool SalvageWorkstations;

	public bool SalvageInPOIs = true;

	public bool SalvageSwitches;

	public readonly HashSet<string> ExcludedBlocks = new HashSet<string>();

	public void Clamp()
	{
		if (Radius < 0f)
		{
			Radius = 0f;
		}
		if (VerticalRadius < 0f)
		{
			VerticalRadius = 0f;
		}
		if (SecondsPerStep < 0.05f)
		{
			SecondsPerStep = 0.05f;
		}
		if (MaxCatchupSeconds < 0f)
		{
			MaxCatchupSeconds = 0f;
		}
		QualityScale.ClampKnobs(ref LowQualityReach, ref LowQualityTimeMult);
	}
}

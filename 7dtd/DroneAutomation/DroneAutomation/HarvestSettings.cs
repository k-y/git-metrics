namespace DroneAutomation;

public sealed class HarvestSettings
{
	public float Radius = 6f;

	public float VerticalRadius = 4f;

	public float SecondsPerTarget = 1f;

	public float MaxCatchupSeconds = 5f;

	public float LowQualityReach = 0.55f;

	public float LowQualityTimeMult = 2f;

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
		if (SecondsPerTarget < 0.05f)
		{
			SecondsPerTarget = 0.05f;
		}
		if (MaxCatchupSeconds < 0f)
		{
			MaxCatchupSeconds = 0f;
		}
		QualityScale.ClampKnobs(ref LowQualityReach, ref LowQualityTimeMult);
	}
}

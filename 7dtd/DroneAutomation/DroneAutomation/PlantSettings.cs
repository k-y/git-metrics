namespace DroneAutomation;

public sealed class PlantSettings
{
	public float Radius = 6f;

	public float VerticalRadius = 3f;

	public float SecondsPerPlant = 1f;

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
		if (SecondsPerPlant < 0.05f)
		{
			SecondsPerPlant = 0.05f;
		}
		if (MaxCatchupSeconds < 0f)
		{
			MaxCatchupSeconds = 0f;
		}
		QualityScale.ClampKnobs(ref LowQualityReach, ref LowQualityTimeMult);
	}
}

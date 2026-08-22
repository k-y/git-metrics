namespace DroneAutomation;

public sealed class VacuumSettings
{
	public float ContainerRadius = 5f;

	public float EntityRadius = 15f;

	public float VerticalRadius = 8f;

	public float SpeedMultiplier = 1f;

	public float MinSecondsPerTarget = 0.25f;

	public float ItemPickupSeconds = 0.25f;

	public float MaxCatchupSeconds = 5f;

	public float SkipIfPlayerWithin;

	public float ThrownGraceSeconds = 5f;

	public float LowQualityReach = 0.55f;

	public float LowQualityTimeMult = 2f;

	public void Clamp()
	{
		if (ContainerRadius < 0f)
		{
			ContainerRadius = 0f;
		}
		if (EntityRadius < 0f)
		{
			EntityRadius = 0f;
		}
		if (VerticalRadius < 0f)
		{
			VerticalRadius = 0f;
		}
		if (SpeedMultiplier <= 0f)
		{
			SpeedMultiplier = 1f;
		}
		if (MinSecondsPerTarget < 0.01f)
		{
			MinSecondsPerTarget = 0.01f;
		}
		if (ItemPickupSeconds < 0f)
		{
			ItemPickupSeconds = 0f;
		}
		if (MaxCatchupSeconds < 0f)
		{
			MaxCatchupSeconds = 0f;
		}
		if (ThrownGraceSeconds < 0f)
		{
			ThrownGraceSeconds = 0f;
		}
		QualityScale.ClampKnobs(ref LowQualityReach, ref LowQualityTimeMult);
	}
}

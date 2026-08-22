namespace DroneAutomation;

public sealed class DefenseSettings
{
	public float Range = 15f;

	public float SecondsPerShot = 0.4f;

	public float MaxCatchupSeconds = 2f;

	public float LowQualityReach = 0.6f;

	public float LowQualityTimeMult = 2f;

	public bool RequireLineOfSight = true;

	public void Clamp()
	{
		if (Range < 0f)
		{
			Range = 0f;
		}
		if (SecondsPerShot < 0.05f)
		{
			SecondsPerShot = 0.05f;
		}
		if (MaxCatchupSeconds < 0f)
		{
			MaxCatchupSeconds = 0f;
		}
		QualityScale.ClampKnobs(ref LowQualityReach, ref LowQualityTimeMult);
	}
}

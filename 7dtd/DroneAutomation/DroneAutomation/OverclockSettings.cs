using UnityEngine;

namespace DroneAutomation;

public sealed class OverclockSettings
{
	public float Q1TimeMult = 0.85f;

	public float Q6TimeMult = 0.5f;

	public void Clamp()
	{
		ClampFrac(ref Q1TimeMult);
		ClampFrac(ref Q6TimeMult);
		if (Q6TimeMult > Q1TimeMult)
		{
			Q6TimeMult = Q1TimeMult;
		}
	}

	public float SpeedMult(int _quality)
	{
		return Mathf.Lerp(Q1TimeMult, Q6TimeMult, QualityScale.T(_quality));
	}

	private static void ClampFrac(ref float _v)
	{
		if (_v > 1f)
		{
			_v = 1f;
		}
		if (_v < 0.05f)
		{
			_v = 0.05f;
		}
	}
}

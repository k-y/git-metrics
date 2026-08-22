using UnityEngine;

namespace DroneAutomation;

public static class QualityScale
{
	public const int MinQuality = 1;

	public const int MaxQuality = 6;

	public static float T(int _quality)
	{
		return (float)(Mathf.Clamp(_quality, 1, 6) - 1) / 5f;
	}

	public static float Reach(float _max, float _lowFrac, int _quality)
	{
		return _max * Mathf.Lerp(_lowFrac, 1f, T(_quality));
	}

	public static float Time(float _max, float _timeMult, int _quality)
	{
		return _max * Mathf.Lerp(_timeMult, 1f, T(_quality));
	}

	public static void ClampKnobs(ref float _lowQualityReach, ref float _lowQualityTimeMult)
	{
		if (_lowQualityReach < 0f)
		{
			_lowQualityReach = 0f;
		}
		if (_lowQualityReach > 1f)
		{
			_lowQualityReach = 1f;
		}
		if (_lowQualityTimeMult < 1f)
		{
			_lowQualityTimeMult = 1f;
		}
	}
}

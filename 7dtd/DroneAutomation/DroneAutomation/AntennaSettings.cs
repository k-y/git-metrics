using UnityEngine;

namespace DroneAutomation;

public sealed class AntennaSettings
{
	public float Q1ReachMult = 1.15f;

	public float Q6ReachMult = 1.6f;

	public void Clamp()
	{
		if (Q1ReachMult < 1f)
		{
			Q1ReachMult = 1f;
		}
		if (Q6ReachMult < 1f)
		{
			Q6ReachMult = 1f;
		}
		if (Q6ReachMult < Q1ReachMult)
		{
			Q6ReachMult = Q1ReachMult;
		}
	}

	public float ReachMult(int _quality)
	{
		return Mathf.Lerp(Q1ReachMult, Q6ReachMult, QualityScale.T(_quality));
	}
}

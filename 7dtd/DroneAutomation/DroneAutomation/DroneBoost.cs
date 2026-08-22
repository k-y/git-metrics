namespace DroneAutomation;

public readonly struct DroneBoost
{
	public readonly float SpeedMult;

	public readonly float ReachMult;

	public static readonly DroneBoost None = new DroneBoost(1f, 1f);

	public DroneBoost(float _speedMult, float _reachMult)
	{
		SpeedMult = _speedMult;
		ReachMult = _reachMult;
	}
}

namespace DroneAutomation;

public sealed class Pacer
{
	private readonly float maxCatchup;

	private ulong lastTick;

	private float credit;

	public float Credit => credit;

	public Pacer(float _maxCatchup)
	{
		maxCatchup = _maxCatchup;
	}

	public void Accrue()
	{
		ulong ticks = GameTimer.Instance.ticks;
		if (ticks < lastTick)
		{
			lastTick = ticks;
		}
		if (lastTick == 0L)
		{
			lastTick = ticks;
			return;
		}
		float num = GameTimer.Instance.ticksPerSecond;
		if (num <= 0f)
		{
			num = 20f;
		}
		credit += (float)(ticks - lastTick) / num;
		lastTick = ticks;
		if (credit > maxCatchup)
		{
			credit = maxCatchup;
		}
	}

	public bool TrySpend(float _seconds)
	{
		if (credit < _seconds)
		{
			return false;
		}
		credit -= _seconds;
		return true;
	}
}

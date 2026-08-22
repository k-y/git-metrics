using UnityEngine;

namespace DroneAutomation;

public sealed class DefenseCore
{
	private readonly DefenseSettings settings;

	private readonly Pacer pacer;

	private MachineGunWeapon weapon;

	public DefenseCore(DefenseSettings _settings)
	{
		settings = _settings;
		pacer = new Pacer(_settings.MaxCatchupSeconds);
	}

	public bool Tick(EntityDrone _drone, int _quality, DroneBoost _boost)
	{
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Expected O, but got Unknown
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		pacer.Accrue();
		if (settings.Range <= 0f)
		{
			return false;
		}
		float num = QualityScale.Time(settings.SecondsPerShot, settings.LowQualityTimeMult, _quality) * _boost.SpeedMult;
		if (pacer.Credit < num)
		{
			return false;
		}
		if (weapon == null)
		{
			MachineGunWeapon val = new MachineGunWeapon((EntityAlive)(object)_drone);
			((Weapon)val).Init();
			if ((Object)(object)((Weapon)val).WeaponJoint == (Object)null)
			{
				return false;
			}
			weapon = val;
		}
		EntityAlive nearestEnemyInRange = _drone.GetNearestEnemyInRange(((Entity)_drone).position);
		if ((Object)(object)nearestEnemyInRange == (Object)null || ((Entity)nearestEnemyInRange).IsDead())
		{
			return false;
		}
		float num2 = QualityScale.Reach(settings.Range, settings.LowQualityReach, _quality) * _boost.ReachMult;
		Vector3 val2 = ((Entity)nearestEnemyInRange).position - ((Entity)_drone).position;
		if (((Vector3)(ref val2)).sqrMagnitude > num2 * num2)
		{
			return false;
		}
		if (settings.RequireLineOfSight && !((EntityAlive)_drone).CanSee(nearestEnemyInRange))
		{
			return false;
		}
		if (!pacer.TrySpend(num))
		{
			return false;
		}
		((Weapon)weapon).Fire(nearestEnemyInRange);
		return true;
	}
}

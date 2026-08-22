namespace DroneAutomation;

public interface IVacuumSink
{
	ItemStack[] Items { get; }

	(bool anyMoved, bool allMoved) TryStackItem(int _startIndex, ItemStack _stack);

	void SetSlot(int _index, ItemStack _stack);

	void MarkChanged();
}

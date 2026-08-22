namespace DroneAutomation;

public sealed class BagSink : IVacuumSink
{
	private readonly Bag bag;

	public ItemStack[] Items => bag.items;

	public BagSink(Bag _bag)
	{
		bag = _bag;
	}

	public (bool anyMoved, bool allMoved) TryStackItem(int _startIndex, ItemStack _stack)
	{
		return bag.TryStackItem(_startIndex, _stack);
	}

	public void SetSlot(int _index, ItemStack _stack)
	{
		bag.SetSlot(_index, _stack.Clone(), true);
	}

	public void MarkChanged()
	{
		bag.onBackpackChanged();
	}
}

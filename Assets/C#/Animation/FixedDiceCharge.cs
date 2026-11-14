#if UNITY_EDITOR
// Test charge source: a fixed value instead of TorchDemo.
public class FixedDiceCharge : IDiceCharge
{
    public FixedDiceCharge(
        float charge)
    {
        Charge = charge;
    }

    public float Charge { get; }
}
#endif

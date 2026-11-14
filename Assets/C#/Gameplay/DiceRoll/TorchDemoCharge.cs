// Adapter: the charge of the original TorchDemo.
public class TorchDemoCharge : IDiceCharge
{
    private readonly TorchDemo torch;

    public TorchDemoCharge(
        TorchDemo torch)
    {
        this.torch = torch;
    }

    public float Charge =>
        torch.Charge;
}

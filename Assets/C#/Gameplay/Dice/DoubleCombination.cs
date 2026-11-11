public class DoubleCombination : IDiceCombination
{
    public string Name =>
        "DOUBLE!";

    public int Priority =>
        1;

    public int Coins =>
        1;

    public bool Matches(
        int[] sortedValues)
    {
        return sortedValues[0] == sortedValues[1] ||
               sortedValues[1] == sortedValues[2];
    }
}

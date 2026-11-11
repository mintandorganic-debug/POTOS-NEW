public class TripleCombination : IDiceCombination
{
    public string Name =>
        "TRIPLE!";

    public int Priority =>
        2;

    public int Coins =>
        2;

    public bool Matches(
        int[] sortedValues)
    {
        return sortedValues[0] == sortedValues[1] &&
               sortedValues[1] == sortedValues[2];
    }
}

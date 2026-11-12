public class TripleSixCombination : IDiceCombination
{
    public string Name =>
        "TRIPLE 6!";

    public int Priority =>
        5;

    public int Coins =>
        6;

    public bool Matches(
        int[] sortedValues)
    {
        return sortedValues[0] == 6 &&
               sortedValues[1] == 6 &&
               sortedValues[2] == 6;
    }
}

public class StraightCombination : IDiceCombination
{
    public string Name =>
        "STRAIGHT!";

    public int Priority =>
        3;

    public int Coins =>
        3;

    public bool Matches(
        int[] sortedValues)
    {
        return sortedValues[1] == sortedValues[0] + 1 &&
               sortedValues[2] == sortedValues[1] + 1;
    }
}

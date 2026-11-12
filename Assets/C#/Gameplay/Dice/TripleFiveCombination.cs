public class TripleFiveCombination : IDiceCombination
{
    public string Name =>
        "TRIPLE 5!";

    public int Priority =>
        4;

    public int Coins =>
        5;

    public bool Matches(
        int[] sortedValues)
    {
        return sortedValues[0] == 5 &&
               sortedValues[1] == 5 &&
               sortedValues[2] == 5;
    }
}

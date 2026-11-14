public interface IDiceCombination
{
    string Name { get; }

    int Priority { get; }

    int Coins { get; }

    bool Matches(
        int[] sortedValues);
}

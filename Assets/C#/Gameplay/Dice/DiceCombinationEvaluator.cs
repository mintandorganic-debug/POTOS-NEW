using System.Collections.Generic;
using System.Linq;

public class DiceCombinationEvaluator
{
    private readonly IReadOnlyList<IDiceCombination> combinations;

    public DiceCombinationEvaluator(
        IEnumerable<IDiceCombination> combinations)
    {
        this.combinations =
            combinations
                .OrderByDescending(c => c.Priority)
                .ToList();

        GameLog.Write(
            "DiceCombinationEvaluator",
            $"Created combinations={this.combinations.Count}");
    }

    public static DiceCombinationEvaluator CreateDefault()
    {
        return new DiceCombinationEvaluator(
            new IDiceCombination[]
            {
                new TripleSixCombination(),
                new TripleFiveCombination(),
                new StraightCombination(),
                new TripleCombination(),
                new DoubleCombination()
            });
    }

    public IDiceCombination Evaluate(
        int[] sortedValues)
    {
        IDiceCombination found =
            combinations.FirstOrDefault(
                c => c.Matches(
                    sortedValues));

        GameLog.Write(
            "DiceCombinationEvaluator",
            $"Evaluate values={string.Join(",", sortedValues)} combination={(found != null ? found.Name : "none")}");

        return found;
    }
}

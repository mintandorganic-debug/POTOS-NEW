#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class DiceRollEvaluation
{
    public static string Describe(
        int[] values)
    {
        int[] sorted =
            values
                .OrderBy(v => v)
                .ToArray();

        IDiceCombination combination =
            DiceCombinationEvaluator
                .CreateDefault()
                .Evaluate(
                    sorted);

        return
            combination != null
                ? $"{combination.Name} (+{combination.Coins})"
                : "no combination";
    }
}
#endif

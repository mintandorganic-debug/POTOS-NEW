using System;

public static class DiceRules
{
    public const int DiceCount = 3;
    public const int ChipsPerPip = 5;

    public static int[] Roll()
    {
        int[] values =
            new int[DiceCount];

        for (int i = 0;
             i < DiceCount;
             i++)
        {
            values[i] =
                UnityEngine.Random.Range(
                    1,
                    7);
        }

        Array.Sort(
            values);

        GameLog.Write(
            "DiceRules",
            $"Roll sorted values={string.Join(",", values)}");

        return values;
    }

    public static int Sum(
        int[] values)
    {
        int sum = 0;

        foreach (int value in values)
        {
            sum += value;
        }

        return sum;
    }

    public static int BaseChips(
        int[] values)
    {
        return Sum(values) * ChipsPerPip;
    }
}

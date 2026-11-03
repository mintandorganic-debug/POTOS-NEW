using System;
using UnityEngine;

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

        GameLog.Write(
            "DiceRules",
            $"Roll values={string.Join(",", values)}");

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

    public static int ChipPayout(
        int[] values)
    {
        return Sum(values) * ChipsPerPip;
    }

    public static int CoinPayout(
        int[] values)
    {
        int[] sorted =
            (int[])values.Clone();

        Array.Sort(sorted);

        bool triple =
            sorted[0] == sorted[1] &&
            sorted[1] == sorted[2];

        if (triple && sorted[0] == 6)
        {
            return 6;
        }

        if (triple && sorted[0] == 5)
        {
            return 5;
        }

        if (IsStraight(sorted))
        {
            return 3;
        }

        if (triple)
        {
            return 2;
        }

        if (sorted[0] == sorted[1] ||
            sorted[1] == sorted[2])
        {
            return 1;
        }

        return 0;
    }

    public static string ComboName(
        int[] values)
    {
        int[] sorted =
            (int[])values.Clone();

        Array.Sort(sorted);

        bool triple =
            sorted[0] == sorted[1] &&
            sorted[1] == sorted[2];

        if (triple && sorted[0] == 6)
        {
            return "TRIPLE 6!";
        }

        if (triple && sorted[0] == 5)
        {
            return "TRIPLE 5!";
        }

        if (IsStraight(sorted))
        {
            return "STRAIGHT!";
        }

        if (triple)
        {
            return "TRIPLE!";
        }

        if (sorted[0] == sorted[1] ||
            sorted[1] == sorted[2])
        {
            return "DOUBLE!";
        }

        return string.Empty;
    }

    private static bool IsStraight(
        int[] sorted)
    {
        return sorted[1] == sorted[0] + 1 &&
               sorted[2] == sorted[1] + 1;
    }
}

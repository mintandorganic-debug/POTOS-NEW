#if UNITY_EDITOR
using UnityEngine;

// Shows the rolled values and the combination in the panel.
public class DiceResultDisplay : MonoBehaviour
{
    private DiceResultPanel panel;

    public int[] LastValues { get; private set; } = new int[0];
    public string LastCombination { get; private set; } = string.Empty;

    private void Awake()
    {
        panel =
            gameObject.AddComponent<DiceResultPanel>();
    }

    public void Show(
        int[] values)
    {
        LastValues = values;

        LastCombination =
            DiceRollEvaluation.Describe(
                values);

        GameLog.Write(
            "DiceResultDisplay",
            $"Result {string.Join(" ", LastValues)} - {LastCombination}");

        panel.Set(
            LastValues,
            LastCombination);
    }

    public void Hide()
    {
        panel.Clear();
    }
}
#endif

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

// Facade: reads the rolled values, shows the panel and starts the spinning dice.
public class DiceResultDisplay : MonoBehaviour
{
    private DiceResultPanel panel;
    private DiceSpinView spinView;

    public int[] LastValues { get; private set; } = new int[0];
    public string LastCombination { get; private set; } = string.Empty;

    private void Awake()
    {
        panel =
            gameObject.AddComponent<DiceResultPanel>();

        spinView =
            gameObject.AddComponent<DiceSpinView>();
    }

    public void Show(
        IList<Rigidbody> dice,
        float power)
    {
        LastValues =
            DiceRollEvaluation.ReadValues(
                dice);

        LastCombination =
            DiceRollEvaluation.Describe(
                LastValues);

        GameLog.Write(
            "DiceResultDisplay",
            $"Result {string.Join(" ", LastValues)} - {LastCombination}");

        panel.Set(
            LastValues,
            LastCombination);

        spinView.Show(
            LastValues,
            power);
    }

    public void Hide()
    {
        panel.Clear();

        spinView.Hide();
    }
}
#endif

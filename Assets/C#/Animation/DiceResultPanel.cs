#if UNITY_EDITOR
using UnityEngine;

public class DiceResultPanel : MonoBehaviour
{
    public const float BottomPixels = 75f;

    private const float Width = 260f;
    private const float Top = 20f;
    private const float Height = 55f;

    private int[] values = new int[0];
    private string combination = string.Empty;
    private bool visible;

    public void Set(
        int[] values,
        string combination)
    {
        this.values = values;
        this.combination = combination;

        visible = true;
    }

    public void Clear()
    {
        visible = false;
    }

    private void OnGUI()
    {
        if (!visible)
        {
            return;
        }

        GUIStyle title =
            new GUIStyle(
                GUI.skin.label)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter
            };

        float left =
            (Screen.width - Width) * 0.5f;

        GUI.Box(
            new Rect(
                left,
                Top,
                Width,
                Height),
            GUIContent.none);

        GUI.Label(
            new Rect(
                left,
                Top + 2f,
                Width,
                25f),
            string.Join("   ", values),
            title);

        GUI.Label(
            new Rect(
                left,
                Top + 27f,
                Width,
                25f),
            combination,
            title);
    }
}
#endif

using UnityEngine;
using UnityEngine.UI;

public class CardTint : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Graphic[] tintGraphics;

    [Header("Card Transition")]
    [SerializeField] private float enterScale = 0.7f;
    
    private Color[] baseColors;

    private Color currentTint = Color.white;

    public Color CurrentTint => currentTint;

    private void Awake()
    {
        baseColors = new Color[tintGraphics.Length];

        for (int i = 0; i < tintGraphics.Length; i++)
            baseColors[i] = tintGraphics[i].color;
    }

    public void SetTint(Color tint)
    {
        EnsureBaseColors();
        
        currentTint = tint;

        for (int i = 0; i < tintGraphics.Length; i++)
        {
            Color baseColor = baseColors[i];

            tintGraphics[i].color = new Color(
                baseColor.r * tint.r,
                baseColor.g * tint.g,
                baseColor.b * tint.b,
                baseColor.a
            );
        }
    }
    
    public void CopyBaseColorsFrom(CardTint source)
    {
        if (source == null)
            return;

        EnsureBaseColors();
        source.EnsureBaseColors();
        
        int count = Mathf.Min(
            baseColors.Length,
            source.baseColors.Length
        );

        for (int i = 0; i < count; i++)
            baseColors[i] = source.baseColors[i];
    }
    
    private void EnsureBaseColors()
    {
        if (baseColors != null)
            return;

        if (tintGraphics == null)
            return;

        baseColors = new Color[tintGraphics.Length];

        for (int i = 0; i < tintGraphics.Length; i++)
        {
            if (tintGraphics[i] != null)
                baseColors[i] = tintGraphics[i].color;
        }
    }

    public void ResetTint()
    {
        SetTint(Color.white);
    }
    
}
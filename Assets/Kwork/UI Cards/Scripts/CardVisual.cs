using UnityEngine;
using UnityEngine.UI;

namespace CardCarouselUI
{

    public class CardVisual : MonoBehaviour
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
    
        public void AddTintGraphic(Graphic graphic)
        {
            EnsureBaseColors();

            int count = tintGraphics.Length;

            System.Array.Resize(ref tintGraphics, count + 1);
            System.Array.Resize(ref baseColors, count + 1);

            tintGraphics[count] = graphic;
            baseColors[count] = graphic.color;
        }

        public void CopyBaseColorsFrom(CardVisual source)
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
}

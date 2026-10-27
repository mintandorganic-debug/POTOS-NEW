using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CardCategoryStyles",
    menuName = "Game/Card Category Styles")]
public class CardCategoryStyles : ScriptableObject
{
    [System.Serializable]
    public class Style
    {
        public Card.CardClass cardClass;
        public Color color = Color.white;
        public Sprite background;
        public Sprite frame;
        public Sprite icon;
        public Material sweepMaterial;
        public GameObject hoverVfx;
    }

    [SerializeField] private List<Style> styles = new();

    public bool TryGetStyle(
        Card.CardClass cardClass,
        out Style style)
    {
        foreach (Style entry in styles)
        {
            if (entry == null ||
                entry.cardClass != cardClass)
            {
                continue;
            }

            style = entry;
            return true;
        }

        style = null;
        return false;
    }
}

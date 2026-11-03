using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardInfoPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image image;

    public void Show(CardData data)
    {
        GameLog.Write(
            "CardInfoPanel",
            $"Show card={(data != null ? data.name : "NULL")}");

        if (data == null)
        {
            GameLog.Write(
                "CardInfoPanel",
                "Show skipped: card data is NULL");

            return;
        }

        nameText.text =
            data.CardName;

        descriptionText.text =
            data.Description;

        image.sprite =
            data.Sprite;

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        GameLog.Write(
            "CardInfoPanel",
            "Hide");

        gameObject.SetActive(false);
    }
}

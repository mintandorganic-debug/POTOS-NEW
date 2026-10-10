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
        if (data == null)
            return;

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
        gameObject.SetActive(false);
    }
}
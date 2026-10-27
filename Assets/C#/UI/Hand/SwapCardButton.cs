using System;
using UnityEngine;
using UnityEngine.UI;

public class SwapCardButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private RawImage image;
    [SerializeField] private GameObject selectedMark;
    [SerializeField] private RectTransform visualRoot;
    [SerializeField] private float selectedLift = 30f;

    private uint cardNetId;
    private bool isSelected;

    public uint CardNetId =>
        cardNetId;

    public bool IsSelected =>
        isSelected;

    public void Setup(
        uint cardNetId,
        Texture texture,
        Action<SwapCardButton> onClick)
    {
        this.cardNetId = cardNetId;

        if (image != null)
            image.texture = texture;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(
                () => onClick?.Invoke(this));
        }

        SetSelected(false);
    }

    public void SetSelected(
        bool value)
    {
        isSelected = value;

        if (selectedMark != null)
            selectedMark.SetActive(value);

        if (visualRoot != null)
        {
            visualRoot.anchoredPosition =
                value
                    ? Vector2.up * selectedLift
                    : Vector2.zero;
        }
    }
}

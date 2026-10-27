using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HandCardView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CardHover hover;
    [SerializeField] private CardTint tint;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Images")]
    [SerializeField] private Image background;
    [SerializeField] private Image frame;
    [SerializeField] private Image art;

    [Header("Text")]
    [SerializeField] private TMP_Text description;

    [Header("State")]
    [SerializeField] private GameObject unavailableOverlay;

    private static readonly Dictionary<Texture2D, Sprite> textureSprites = new();

    private RectTransform rectTransform;

    private Card card;

    private bool isFocused;

    public RectTransform Rect =>
        rectTransform;

    public Card Card =>
        card;

    private void Awake()
    {
        rectTransform =
            transform as RectTransform;

        if (hover == null)
            hover = GetComponent<CardHover>();

        if (tint == null)
            tint = GetComponent<CardTint>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (art == null &&
            hover != null)
        {
            art = hover.CardImage;
        }

        if (description == null &&
            hover != null &&
            hover.DescriptionGroup != null)
        {
            description =
                hover.DescriptionGroup.GetComponentInChildren<TMP_Text>(true);
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (frame != null)
            frame.enabled = false;
    }

    public void Bind(
        Card card,
        CardCategoryStyles styles)
    {
        this.card = card;

        SetFocused(false);

        if (card == null ||
            card.Data == null)
        {
            return;
        }

        CardData data =
            card.Data;

        hover?.ResetVisual();

        Sprite sprite =
            GetArtSprite(data);

        if (art != null &&
            sprite != null)
        {
            art.sprite = sprite;
        }

        if (description != null)
        {
            description.text =
                data.Description;
        }

        if (styles != null &&
            styles.TryGetStyle(
                data.CardClass,
                out CardCategoryStyles.Style style))
        {
            ApplyStyle(style);
        }

        hover?.CacheBaseSprite();

        RefreshAvailability();
    }

    private static Sprite GetArtSprite(
        CardData data)
    {
        if (data.Sprite != null)
            return data.Sprite;

        if (data.texture == null)
            return null;

        if (!textureSprites.TryGetValue(
                data.texture,
                out Sprite sprite))
        {
            sprite =
                Sprite.Create(
                    data.texture,
                    new Rect(
                        0f,
                        0f,
                        data.texture.width,
                        data.texture.height),
                    new Vector2(0.5f, 0.5f));

            textureSprites.Add(
                data.texture,
                sprite);
        }

        return sprite;
    }

    private void ApplyStyle(
        CardCategoryStyles.Style style)
    {
        if (background != null &&
            style.background != null)
        {
            background.sprite = style.background;
        }

        if (frame != null &&
            style.frame != null)
        {
            frame.sprite = style.frame;
        }

        if (hover == null)
            return;

        hover.SetCategoryIcon(style.icon);
        hover.SetFrameMaterial(style.sweepMaterial);
        hover.SetVfxPrefab(style.hoverVfx);
        hover.ShowAllStatusIcons();
    }

    public void RefreshAvailability()
    {
        if (unavailableOverlay == null)
            return;

        unavailableOverlay.SetActive(
            card != null &&
            !card.IsAvailable);
    }

    public void SetFocused(
        bool value)
    {
        if (isFocused == value)
            return;

        isFocused = value;

        if (frame != null)
            frame.enabled = value;

        hover?.SetHover(value);
    }

    public void SetTint(
        Color color)
    {
        tint?.SetTint(color);
    }

    public void SetAlpha(
        float alpha)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = alpha;
    }

    private void OnDisable()
    {
        isFocused = false;

        if (frame != null)
            frame.enabled = false;
    }
}

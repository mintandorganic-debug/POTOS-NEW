using UnityEngine;
using UnityEngine.UI;

public class DiceChargeBar : MonoBehaviour
{
    private const float Width = 360f;
    private const float Height = 45f;
    private const float Margin = 30f;
    private const float FillInset = 5f;

    private GameObject root;
    private Image fill;

    public void Show()
    {
        Build();

        root.SetActive(
            true);

        SetValue(
            0f);
    }

    public void Hide()
    {
        if (root != null)
        {
            root.SetActive(
                false);
        }
    }

    public void SetValue(
        float value)
    {
        if (fill != null)
        {
            fill.fillAmount =
                Mathf.Clamp01(
                    value);
        }
    }

    private void Build()
    {
        if (root != null)
        {
            return;
        }

        Sprite back =
            LoadSprite(
                "Dice/ChargeBarBack");

        Sprite front =
            LoadSprite(
                "Dice/ChargeBarFill");

        root =
            new GameObject("DiceChargeBar");

        root.transform.SetParent(
            transform,
            false);

        Canvas canvas =
            root.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        root.AddComponent<CanvasScaler>();

        Image background =
            CreateImage(
                "Back",
                root.transform,
                back);

        RectTransform backRect =
            background.rectTransform;

        backRect.anchorMin = new Vector2(0f, 1f);
        backRect.anchorMax = new Vector2(0f, 1f);
        backRect.pivot = new Vector2(0f, 1f);
        backRect.anchoredPosition = new Vector2(Margin, -Margin);
        backRect.sizeDelta = new Vector2(Width, Height);

        fill =
            CreateImage(
                "Fill",
                background.transform,
                front);

        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;

        RectTransform fillRect =
            fill.rectTransform;

        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(FillInset, FillInset);
        fillRect.offsetMax = new Vector2(-FillInset, -FillInset);
    }

    private static Image CreateImage(
        string objectName,
        Transform parent,
        Sprite sprite)
    {
        GameObject go =
            new GameObject(objectName);

        go.transform.SetParent(
            parent,
            false);

        Image image =
            go.AddComponent<Image>();

        image.sprite = sprite;
        image.raycastTarget = false;

        return image;
    }

    private static Sprite LoadSprite(
        string path)
    {
        Texture2D texture =
            Resources.Load<Texture2D>(
                path);

        if (texture == null)
        {
            GameLog.Write(
                "DiceChargeBar",
                $"Texture not found: {path}");

            return null;
        }

        return Sprite.Create(
            texture,
            new Rect(
                0f,
                0f,
                texture.width,
                texture.height),
            new Vector2(
                0.5f,
                0.5f));
    }
}

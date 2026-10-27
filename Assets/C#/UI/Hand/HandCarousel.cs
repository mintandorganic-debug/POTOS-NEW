using System;
using System.Collections.Generic;
using UnityEngine;

public class HandCarousel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform root;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private HandCardView cardTemplate;
    [SerializeField] private CardCategoryStyles categoryStyles;

    [Header("Layout")]
    [SerializeField] private float cardSpacing = 200f;
    [SerializeField] private float centerScale = 1.5f;
    [SerializeField] private float sideScale = 0.8f;
    [SerializeField] private int visibleSideCards = 1;
    [SerializeField] private Color centerColor = Color.white;
    [SerializeField] private Color sideColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    [Header("Tilt")]
    [Range(45f, 90f)]
    [SerializeField] private float tiltAngle = 72f;

    [Header("Animation")]
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private float showSpeed = 6f;
    [SerializeField] private float hiddenOffset = -300f;

    [Header("Description")]
    [SerializeField] private float descriptionDelay = 0.5f;

    [Header("Hand Anchor")]
    [SerializeField] private float anchorOffset = 0.12f;
    [SerializeField] private float anchorSpeed = 10f;

    private readonly List<HandCardView> views = new();
    private readonly List<Card> cards = new();
    private readonly List<Transform> anchors = new();

    private Camera viewCamera;

    private PlayerHand hand;

    private Vector2 rootBasePosition;

    private int centerIndex = -1;
    private bool isOpen;
    private float visibility;
    private float focusTimer;

    public event Action<Card> CenterChanged;

    public bool IsOpen =>
        isOpen;

    public Card CenterCard =>
        centerIndex >= 0 &&
        centerIndex < cards.Count
            ? cards[centerIndex]
            : null;

    // =========================================================
    // LIFECYCLE
    // =========================================================

    private void Awake()
    {
        if (root != null)
            rootBasePosition = root.anchoredPosition;

        if (cardTemplate != null)
            cardTemplate.gameObject.SetActive(false);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        ApplyTilt();
        ApplyVisibility();
    }

    private void OnValidate()
    {
        ApplyTilt();
    }

    private void OnDestroy()
    {
        if (hand != null)
            hand.Changed -= OnHandChanged;
    }

    public void Initialize(
        PlayerHand hand)
    {
        if (this.hand != null)
            this.hand.Changed -= OnHandChanged;

        this.hand = hand;

        if (this.hand != null)
            this.hand.Changed += OnHandChanged;
    }

    public void SetAnchors(
        Camera camera,
        List<GameObject> sockets)
    {
        viewCamera = camera;

        anchors.Clear();

        if (sockets == null)
            return;

        foreach (GameObject socket in sockets)
        {
            if (socket != null)
                anchors.Add(socket.transform);
        }
    }

    private void LateUpdate()
    {
        if (visibility <= 0f ||
            viewCamera == null ||
            anchors.Count == 0 ||
            transform.parent != viewCamera.transform)
        {
            return;
        }

        float top = float.MinValue;

        foreach (Transform anchor in anchors)
        {
            Vector3 point =
                viewCamera.WorldToViewportPoint(
                    anchor.position);

            if (point.z > 0f)
                top = Mathf.Max(top, point.y);
        }

        if (top == float.MinValue)
            return;

        Vector3 position =
            transform.localPosition;

        float halfHeight =
            position.z *
            Mathf.Tan(
                viewCamera.fieldOfView *
                0.5f *
                Mathf.Deg2Rad);

        float targetY =
            (top + anchorOffset - 0.5f) *
            2f *
            halfHeight;

        position.y =
            visibility < 1f
                ? targetY
                : Mathf.Lerp(
                    position.y,
                    targetY,
                    1f - Mathf.Exp(
                        -anchorSpeed *
                        Time.deltaTime));

        transform.localPosition = position;
    }

    private void Update()
    {
        float target =
            isOpen
                ? 1f
                : 0f;

        visibility =
            Mathf.MoveTowards(
                visibility,
                target,
                showSpeed *
                Time.deltaTime);

        ApplyVisibility();

        if (visibility <= 0f)
            return;

        UpdateLayout(
            Time.deltaTime);

        UpdateFocus(
            Time.deltaTime);
    }

    // =========================================================
    // OPEN / CLOSE
    // =========================================================

    public void Show(
        Card focus)
    {
        if (!isOpen)
        {
            isOpen = true;

            Rebuild();

            if (visibility <= 0f)
                SnapLayout();
        }

        Focus(focus);
    }

    public void Hide()
    {
        if (!isOpen)
            return;

        isOpen = false;

        foreach (HandCardView view in views)
        {
            view.SetFocused(false);
        }
    }

    // =========================================================
    // NAVIGATION
    // =========================================================

    public void Focus(
        Card card)
    {
        if (card == null)
            return;

        int index =
            cards.IndexOf(card);

        if (index < 0)
        {
            Rebuild();

            index =
                cards.IndexOf(card);
        }

        if (index < 0)
            return;

        SetCenter(index);
    }

    public void Scroll(
        int direction)
    {
        if (!isOpen ||
            cards.Count == 0 ||
            direction == 0)
        {
            return;
        }

        int index =
            (centerIndex + (direction > 0 ? 1 : -1) + cards.Count) %
            cards.Count;

        SetCenter(index);
    }

    private void SetCenter(
        int index)
    {
        if (index == centerIndex)
            return;

        centerIndex = index;
        focusTimer = 0f;

        foreach (HandCardView view in views)
        {
            view.SetFocused(false);
        }

        RefreshOrder();

        CenterChanged?.Invoke(
            CenterCard);
    }

    // =========================================================
    // CARDS
    // =========================================================

    private void OnHandChanged()
    {
        if (!isOpen)
            return;

        Rebuild();
    }

    private void Rebuild()
    {
        Card previousCenter =
            CenterCard;

        cards.Clear();

        if (hand != null)
            cards.AddRange(hand.GetCards());

        EnsureViews(cards.Count);

        for (int i = 0;
             i < views.Count;
             i++)
        {
            bool used =
                i < cards.Count;

            views[i].gameObject.SetActive(used);

            if (used)
                views[i].Bind(cards[i], categoryStyles);
        }

        int index =
            previousCenter != null
                ? cards.IndexOf(previousCenter)
                : -1;

        centerIndex = -1;

        if (cards.Count == 0)
        {
            CenterChanged?.Invoke(null);
            return;
        }

        SetCenter(
            index >= 0
                ? index
                : 0);
    }

    private void EnsureViews(
        int count)
    {
        if (cardTemplate == null)
            return;

        while (views.Count < count)
        {
            HandCardView view =
                Instantiate(
                    cardTemplate,
                    cardTemplate.transform.parent);

            view.gameObject.name =
                $"{cardTemplate.gameObject.name} {views.Count}";

            views.Add(view);
        }
    }

    public void RefreshAvailability()
    {
        foreach (HandCardView view in views)
        {
            view.RefreshAvailability();
        }
    }

    // =========================================================
    // LAYOUT
    // =========================================================

    private void UpdateLayout(
        float deltaTime)
    {
        float t =
            1f - Mathf.Exp(
                -moveSpeed *
                deltaTime);

        for (int i = 0;
             i < cards.Count &&
             i < views.Count;
             i++)
        {
            GetTarget(
                i,
                out Vector2 position,
                out float scale,
                out float alpha,
                out Color color);

            HandCardView view =
                views[i];

            view.Rect.anchoredPosition =
                Vector2.Lerp(
                    view.Rect.anchoredPosition,
                    position,
                    t);

            view.Rect.localScale =
                Vector3.Lerp(
                    view.Rect.localScale,
                    Vector3.one * scale,
                    t);

            view.SetAlpha(alpha);
            view.SetTint(color);
        }
    }

    private void SnapLayout()
    {
        for (int i = 0;
             i < cards.Count &&
             i < views.Count;
             i++)
        {
            GetTarget(
                i,
                out Vector2 position,
                out float scale,
                out float alpha,
                out Color color);

            HandCardView view =
                views[i];

            view.Rect.anchoredPosition = position;
            view.Rect.localScale = Vector3.one * scale;

            view.SetAlpha(alpha);
            view.SetTint(color);
        }
    }

    private void GetTarget(
        int index,
        out Vector2 position,
        out float scale,
        out float alpha,
        out Color color)
    {
        int offset =
            GetOffset(index);

        bool isCenter =
            offset == 0;

        bool isVisible =
            Mathf.Abs(offset) <= visibleSideCards;

        position =
            Vector2.right *
            offset *
            cardSpacing;

        scale =
            isCenter
                ? centerScale
                : sideScale;

        alpha =
            isVisible
                ? 1f
                : 0f;

        Card card =
            cards[index];

        color =
            isCenter &&
            card != null &&
            card.IsAvailable
                ? centerColor
                : sideColor;
    }

    private int GetOffset(
        int index)
    {
        int count =
            cards.Count;

        if (count == 0 ||
            centerIndex < 0)
        {
            return 0;
        }

        int offset =
            index - centerIndex;

        if (offset > count / 2)
            offset -= count;

        if (offset < -count / 2)
            offset += count;

        return offset;
    }

    private void RefreshOrder()
    {
        List<HandCardView> ordered =
            new List<HandCardView>();

        for (int i = 0;
             i < cards.Count &&
             i < views.Count;
             i++)
        {
            ordered.Add(views[i]);
        }

        ordered.Sort(
            (a, b) =>
                Mathf.Abs(GetOffset(views.IndexOf(b)))
                    .CompareTo(
                        Mathf.Abs(GetOffset(views.IndexOf(a)))));

        foreach (HandCardView view in ordered)
        {
            view.transform.SetAsLastSibling();
        }
    }

    private void UpdateFocus(
        float deltaTime)
    {
        if (!isOpen ||
            visibility < 1f)
        {
            return;
        }

        if (centerIndex < 0 ||
            centerIndex >= views.Count)
        {
            return;
        }

        focusTimer += deltaTime;

        if (focusTimer < descriptionDelay)
            return;

        views[centerIndex].SetFocused(true);
    }

    // =========================================================
    // VISUAL
    // =========================================================

    private void ApplyVisibility()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = visibility;

        if (root == null)
            return;

        float eased =
            Mathf.SmoothStep(
                0f,
                1f,
                visibility);

        root.anchoredPosition =
            rootBasePosition +
            Vector2.up *
            hiddenOffset *
            (1f - eased);
    }

    private void ApplyTilt()
    {
        if (root == null)
            return;

        root.localRotation =
            Quaternion.Euler(
                90f - tiltAngle,
                0f,
                0f);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CardCarouselUI
{

    public class CardCarouselController : MonoBehaviour
    {
        [Header("Links")]
        [SerializeField] private CardComboController comboController;

        [Header("Cards")]
        [SerializeField] private List<RectTransform> cards = new();

        [Header("Positions")]
        [SerializeField] private Vector2 centerPosition = Vector2.zero;

        private const float CenterDefenseOverlap = 1f / 3f;
        private const float CenterDefenseScale = 0.875f;

        [SerializeField] private float cardSpacing = 350f;
        [SerializeField] private float comboSpacing = 100f;

        [Header("Input")]
        [SerializeField] private string leftKey = "A";
        [SerializeField] private string rightKey = "D";

        [Header("Movement")]
        [SerializeField] private float moveDuration = 0.25f;

        [Header("Visual")]
        [SerializeField] private float centerScale = 1f;
        [SerializeField] private float sideScale = 0.8f;
        [SerializeField] private Color centerColor = Color.white;
        [SerializeField] private Color sideColor = new Color(0.4f, 0.4f, 0.4f, 1f);

        [Header("Mouse")]
        [Tooltip("How long the pointer must rest on a side card before the carousel scrolls to it.")]
        [SerializeField] private float hoverScrollDelay = 0.15f;
        [SerializeField] private bool scrollWheelEnabled = true;

        [Header("Fly Out")]
        [SerializeField] private float flyOutDuration = 0.3f;
        [SerializeField] private float flyOutDistance = 600f;
        [SerializeField] private float flyOutScaleGain = 0.15f;

        private bool isFlyingOut;
        private RectTransform defenseFlightCard;
        private Coroutine defenseFlightMotion;
        private bool isDefenseReturning;
        private int hoverScrollIndex = -1;
        private float hoverScrollTimer;
        [Header("Debug")]
        [SerializeField] private bool debugLogs = true;
    
        private bool isMoving;

        private Key leftKeyCode = Key.A;
        private Key rightKeyCode = Key.D;

        private enum Selection
        {
            Card,
            LeftIcon,
            RightIcon
        }

        private Selection selection = Selection.Card;

        private int currentIndex = 0;
        private int previousIndex;
        private int moveDirection;

        private readonly List<Vector3> baseScales = new();
        private readonly List<float> hoverScales = new();
        private readonly List<CardVisual> cardVisuals = new();
        private readonly List<CardHover> cardHovers = new();
    
        public bool IsMoving => isMoving;
        internal bool HasDefenseFlight => defenseFlightCard != null;
        public int CurrentIndex => currentIndex;
        public IReadOnlyList<RectTransform> Cards => cards;

        public Color SideColor => sideColor;

        public Vector2 GetComboPosition(bool left)
        {
            float spacing = cardSpacing + comboSpacing;

            return centerPosition +
                   (left ? Vector2.left : Vector2.right) * spacing;
        }

        public Vector3 GetComboScale(RectTransform card)
        {
            int index = cards.IndexOf(card);

            if (index < 0)
                return card.localScale;

            return baseScales[index] * centerScale;
        }
    
        private void Awake()
        {
            leftKeyCode = ParseKey(leftKey, Key.A);
            rightKeyCode = ParseKey(rightKey, Key.D);
        }

        private void Start()
        {
            if (baseScales.Count == 0)
                RebuildCache();
        }

        public void SetCards(IReadOnlyList<RectTransform> newCards)
        {
            EndDefenseFlight(true);
            StopAllCoroutines();
            isMoving = false;
            isFlyingOut = false;
            ResetHoverScroll();

            foreach (RectTransform oldCard in cards)
            {
                if (oldCard != null && !ContainsCard(newCards, oldCard))
                    Destroy(oldCard.gameObject);
            }

            cards.Clear();
            cards.AddRange(newCards);

            currentIndex = 0;
            selection = Selection.Card;

            if (comboController != null)
                comboController.Invalidate();

            RebuildCache();
        }

        private void RebuildCache()
        {
            baseScales.Clear();
            cardVisuals.Clear();
            cardHovers.Clear();
            hoverScales.Clear();

            if (cards.Count == 0)
                return;

            currentIndex = Mathf.Clamp(currentIndex, 0, cards.Count - 1);

            for (int i = 0; i < cards.Count; i++)
            {
                baseScales.Add(cards[i].localScale);
                hoverScales.Add(1f);

                CardVisual visual = cards[i].GetComponent<CardVisual>();
                CardHover hover = cards[i].GetComponent<CardHover>();

                if (visual == null || hover == null)
                    return;

                cardVisuals.Add(visual);
                cardHovers.Add(hover);
            }

            RefreshCardsInstant();
        }

        private void Update()
        {
            if (HasDefenseFlight)
            {
                if (!isDefenseReturning && (Mouse.current?.rightButton.wasPressedThisFrame ?? false))
                {
                    if (defenseFlightMotion != null)
                        StopCoroutine(defenseFlightMotion);

                    isDefenseReturning = true;
                    defenseFlightMotion = StartCoroutine(MoveDefenseCard(defenseFlightCard, false));
                }

                return;
            }

            if (isMoving || isFlyingOut || cards.Count < 2)
                return;

            if (WasPressed(leftKeyCode))
            {
                Step(-1);
                return;
            }

            if (WasPressed(rightKeyCode))
            {
                Step(1);
                return;
            }

            if (selection != Selection.Card && WasPressed(Key.Enter))
                ActivateSelectedIcon();

            if (TryScrollWithWheel())
                return;

            UpdateHoverScroll();
        }

        private bool TryScrollWithWheel()
        {
            if (!scrollWheelEnabled || Mouse.current == null)
                return false;

            float scroll = Mouse.current.scroll.ReadValue().y;

            if (Mathf.Approximately(scroll, 0f))
                return false;

            // Wheel up = previous (left) card, wheel down = next (right) card.
            ScrollTo(scroll > 0f ? -1 : 1);
            return true;
        }

        private void UpdateHoverScroll()
        {
            int hoveredIndex = GetHoveredSideIndex();

            if (hoveredIndex < 0)
            {
                ResetHoverScroll();
                return;
            }

            if (hoveredIndex != hoverScrollIndex)
            {
                hoverScrollIndex = hoveredIndex;
                hoverScrollTimer = 0f;
                return;
            }

            hoverScrollTimer += Time.unscaledDeltaTime;

            if (hoverScrollTimer < hoverScrollDelay)
                return;

            ScrollTo(hoveredIndex == GetNextIndex(currentIndex) ? 1 : -1);
        }

        private int GetHoveredSideIndex()
        {
            int rightIndex = GetNextIndex(currentIndex);
            int leftIndex = GetPreviousIndex(currentIndex);

            if (IsPointerOver(rightIndex))
                return rightIndex;

            if (IsPointerOver(leftIndex))
                return leftIndex;

            return -1;
        }

        private bool IsPointerOver(int index)
        {
            return index != currentIndex &&
                   index >= 0 &&
                   index < cardHovers.Count &&
                   cardHovers[index].IsPointerInside;
        }

        private void ScrollTo(int direction)
        {
            ResetHoverScroll();
            SetSelection(Selection.Card);

            if (direction > 0)
                MoveRight();
            else
                MoveLeft();
        }

        private void ResetHoverScroll()
        {
            hoverScrollIndex = -1;
            hoverScrollTimer = 0f;
        }

        private void Step(int direction)
        {
            Selection backToCard = direction > 0 ? Selection.LeftIcon : Selection.RightIcon;

            if (selection == backToCard)
            {
                SetSelection(Selection.Card);
                return;
            }

            Selection iconAhead = direction > 0 ? Selection.RightIcon : Selection.LeftIcon;

            if (selection == Selection.Card && HasIcon(iconAhead))
            {
                SetSelection(iconAhead);
                return;
            }

            SetSelection(Selection.Card);

            if (direction > 0)
                MoveRight();
            else
                MoveLeft();
        }

        private bool HasIcon(Selection icon)
        {
            return GetIconButton(icon) != null;
        }

        private CardCraftButton GetIconButton(Selection icon)
        {
            if (comboController == null || icon == Selection.Card)
                return null;

            int neighbour = icon == Selection.RightIcon
                ? GetNextIndex(currentIndex)
                : GetPreviousIndex(currentIndex);

            return comboController.GetButton(cards[currentIndex], cards[neighbour]);
        }

        private void SetSelection(Selection next)
        {
            CardCraftButton previous = GetIconButton(selection);

            if (previous != null)
                previous.SetHighlighted(false);

            selection = next;

            CardCraftButton current = GetIconButton(selection);

            if (current != null)
                current.SetHighlighted(true);
        }

        private void ActivateSelectedIcon()
        {
            CardCraftButton button = GetIconButton(selection);

            if (button != null)
                button.Activate();
        }

        private static bool ContainsCard(
            IReadOnlyList<RectTransform> list,
            RectTransform card)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == card)
                    return true;
            }

            return false;
        }

        private static Key ParseKey(string keyName, Key fallback)
        {
            return System.Enum.TryParse(keyName, true, out Key key)
                ? key
                : fallback;
        }

        private static bool WasPressed(Key key)
        {
            return Keyboard.current != null &&
                   Keyboard.current[key].wasPressedThisFrame;
        }

        private void MoveLeft()
        {
            if (isMoving)
                return;

            previousIndex = currentIndex;
            moveDirection = -1;

            currentIndex = GetPreviousIndex(currentIndex);

            UpdateCardInteraction();
            StartCoroutine(AnimateCards());
        }

        private void MoveRight()
        {
            if (isMoving)
                return;

            previousIndex = currentIndex;
            moveDirection = 1;

            currentIndex = GetNextIndex(currentIndex);

            UpdateCardInteraction();
            PrepareCardsForAnimation();

            StartCoroutine(AnimateCards());
        }

        private void PrepareCardsForAnimation()
        {
            int leftIndex = GetPreviousIndex(currentIndex);
            int rightIndex = GetNextIndex(currentIndex);

            cards[leftIndex].gameObject.SetActive(true);
            cards[currentIndex].gameObject.SetActive(true);
            cards[rightIndex].gameObject.SetActive(true);

            cards[currentIndex].SetAsLastSibling();
        }

        private IEnumerator AnimateCards()
        {
            isMoving = true;

            int oldLeft = GetPreviousIndex(previousIndex);
            int oldCenter = previousIndex;
            int oldRight = GetNextIndex(previousIndex);

            int newLeft = GetPreviousIndex(currentIndex);
            int newCenter = currentIndex;
            int newRight = GetNextIndex(currentIndex);

            int enteringIndex;
            int leavingIndex;

            if (moveDirection > 0)
            {
                enteringIndex = newRight;
                leavingIndex = oldLeft;
            }
            else
            {
                enteringIndex = newLeft;
                leavingIndex = oldRight;
            }

            Vector2 leftPosition =
                centerPosition + Vector2.left * cardSpacing;

            Vector2 rightPosition =
                centerPosition + Vector2.right * cardSpacing;

            Vector2 hiddenLeftPosition =
                centerPosition + Vector2.left * cardSpacing * 2f;

            Vector2 hiddenRightPosition =
                centerPosition + Vector2.right * cardSpacing * 2f;

            cards[oldLeft].gameObject.SetActive(true);
            cards[oldCenter].gameObject.SetActive(true);
            cards[oldRight].gameObject.SetActive(true);
            cards[enteringIndex].gameObject.SetActive(true);

            RectTransform enteringCard = cards[enteringIndex];

            if (moveDirection > 0)
                enteringCard.anchoredPosition = hiddenRightPosition;
            else
                enteringCard.anchoredPosition = hiddenLeftPosition;

            enteringCard.localScale =
                baseScales[enteringIndex] * sideScale;

            cardVisuals[enteringIndex].SetTint(sideColor);

            cards[currentIndex].SetAsLastSibling();

            Dictionary<int, Vector2> startPositions = new();
            Dictionary<int, Vector2> targetPositions = new();

            Dictionary<int, Vector3> startScales = new();
            Dictionary<int, Vector3> targetScales = new();

            Dictionary<int, Color> startColors = new();
            Dictionary<int, Color> targetColors = new();

            int[] animatedCards =
            {
                oldLeft,
                oldCenter,
                oldRight,
                enteringIndex
            };

            foreach (int index in animatedCards)
            {
                RectTransform card = cards[index];
                CardVisual visual = cardVisuals[index];

                startPositions[index] = card.anchoredPosition;
                startScales[index] = card.localScale;
                startColors[index] = visual.CurrentTint;

                if (index == newCenter)
                {
                    targetPositions[index] = centerPosition;
                    targetScales[index] = baseScales[index] * centerScale;
                    targetColors[index] = centerColor;
                }
                else if (index == newLeft)
                {
                    bool isCombo = IsLeftCombo();

                    float spacing = isCombo
                        ? cardSpacing + comboSpacing
                        : cardSpacing;

                    targetPositions[index] = centerPosition + Vector2.left * spacing;
                    targetScales[index] = baseScales[index] * sideScale;
                    targetColors[index] = sideColor;
                }
                else if (index == newRight)
                {
                    bool isCombo = IsRightCombo();

                    float spacing = isCombo
                        ? cardSpacing + comboSpacing
                        : cardSpacing;

                    targetPositions[index] = centerPosition + Vector2.right * spacing;
                    targetScales[index] = baseScales[index] * sideScale;
                    targetColors[index] = sideColor;
                }
                else
                {
                    targetPositions[index] =
                        moveDirection > 0
                            ? hiddenLeftPosition
                            : hiddenRightPosition;

                    targetScales[index] =
                        baseScales[index] * sideScale * hoverScales[index];

                    targetColors[index] = sideColor;
                }
            }

            float time = 0f;

            while (time < moveDuration)
            {
                time += Time.deltaTime;

                float t = Mathf.Clamp01(time / moveDuration);
                t = Mathf.SmoothStep(0f, 1f, t);

                foreach (int index in animatedCards)
                {
                    RectTransform card = cards[index];

                    card.anchoredPosition = Vector2.Lerp(
                        startPositions[index],
                        targetPositions[index],
                        t
                    );

                    card.localScale = Vector3.Lerp(
                        startScales[index],
                        targetScales[index],
                        t
                    );

                    Color tint = Color.Lerp(
                        startColors[index],
                        targetColors[index],
                        t
                    );

                    cardVisuals[index].SetTint(tint);
                }

                yield return null;
            }

            RefreshCardsInstant();

            isMoving = false;

            cardHovers[currentIndex].Shake();
        }

        private void RefreshCardsInstant()
        {
            int leftIndex = GetPreviousIndex(currentIndex);
            int rightIndex = GetNextIndex(currentIndex);

            bool leftCombo = IsLeftCombo();
            bool rightCombo = IsRightCombo();

            for (int i = 0; i < cards.Count; i++)
            {
                bool isVisible =
                    i == leftIndex ||
                    i == currentIndex ||
                    i == rightIndex;

                cards[i].gameObject.SetActive(isVisible);

                if (!isVisible)
                    continue;

                GetCardVisual(
                    i,
                    out Vector2 position,
                    out Vector3 scale,
                    out Color color
                );

                cards[i].anchoredPosition = position;
                cards[i].localScale = scale;

                cardVisuals[i].SetTint(color);
            }

            UpdateCardInteraction();

            cards[currentIndex].SetAsLastSibling();
        }

        private void UpdateCardInteraction()
        {
            // Only the center card reacts to hover/click. Side cards are used
            // for hover-scrolling, so they must not show hover visuals.
            for (int i = 0; i < cardHovers.Count; i++)
            {
                cardHovers[i].SetInteractable(i == currentIndex);
            }
        }

        internal bool IsDefenseFlying(RectTransform card)
        {
            return defenseFlightCard == card && !isDefenseReturning;
        }

        internal void StartDefenseFlight(RectTransform card)
        {
            if (isMoving || isFlyingOut || HasDefenseFlight || cards.Count == 0 ||
                cards[currentIndex] != card)
            {
                return;
            }

            ResetHoverScroll();
            defenseFlightCard = card;
            defenseFlightMotion = StartCoroutine(MoveDefenseCard(card, true));
        }

        private IEnumerator MoveDefenseCard(RectTransform card, bool lift)
        {
            int index = cards.IndexOf(card);
            Vector2 startPosition = card.anchoredPosition;
            Vector3 startScale = card.localScale;
            Vector2 targetPosition = lift ? GetDefenseFlightPosition(index) : centerPosition;
            Vector3 targetScale = baseScales[index] * centerScale * hoverScales[index];
            if (lift)
                targetScale *= CenterDefenseScale;

            float elapsed = 0f;
            do
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(moveDuration, 0.01f)));
                card.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, progress);
                card.localScale = Vector3.Lerp(startScale, targetScale, progress);
                yield return null;
            } while (elapsed < moveDuration);

            defenseFlightMotion = null;
            if (!lift)
            {
                EndDefenseFlight(false);
                cardHovers[index].SetInteractable(true);
            }
        }

        private void EndDefenseFlight(bool resetPose)
        {
            if (defenseFlightMotion != null)
                StopCoroutine(defenseFlightMotion);

            if (resetPose && defenseFlightCard != null)
            {
                int index = cards.IndexOf(defenseFlightCard);
                if (index >= 0 && index < baseScales.Count)
                {
                    defenseFlightCard.anchoredPosition = centerPosition;
                    defenseFlightCard.localScale = baseScales[index] * centerScale * hoverScales[index];
                }
            }

            defenseFlightCard = null;
            defenseFlightMotion = null;
            isDefenseReturning = false;
        }

        private void OnDisable()
        {
            EndDefenseFlight(true);
        }

        public void FlyOut(RectTransform card, System.Action onComplete)
        {
            if (cards.IndexOf(card) < 0 || isFlyingOut)
            {
                onComplete?.Invoke();
                return;
            }

            if (isMoving)
            {
                StopAllCoroutines();
                isMoving = false;
                RefreshCardsInstant();
            }

            if (card == defenseFlightCard)
                EndDefenseFlight(false);

            StartCoroutine(AnimateFlyOut(card, onComplete));
        }

        private IEnumerator AnimateFlyOut(RectTransform card, System.Action onComplete)
        {
            isFlyingOut = true;
            ResetHoverScroll();

            int index = cards.IndexOf(card);

            if (index >= 0 && index < cardHovers.Count)
                cardHovers[index].SetInteractable(false);

            card.SetAsLastSibling();

            if (!card.TryGetComponent(out CanvasGroup group))
                group = card.gameObject.AddComponent<CanvasGroup>();

            group.blocksRaycasts = false;

            Vector2 startPosition = card.anchoredPosition;
            Vector2 targetPosition = startPosition + Vector2.up * flyOutDistance;
            Vector3 startScale = card.localScale;

            float time = 0f;

            while (time < flyOutDuration)
            {
                time += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(time / flyOutDuration);
                float eased = t * t;

                card.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, eased);
                card.localScale = startScale * (1f + flyOutScaleGain * t);
                group.alpha = 1f - t;

                yield return null;
            }

            RemoveCard(card);

            isFlyingOut = false;

            onComplete?.Invoke();
        }

        private void RemoveCard(RectTransform card)
        {
            int index = cards.IndexOf(card);

            if (index < 0)
                return;

            cards.RemoveAt(index);

            // Keep the cached base scales: the remaining cards already carry
            // carousel scaling, so re-reading localScale would compound it.
            if (index < baseScales.Count) baseScales.RemoveAt(index);
            if (index < hoverScales.Count) hoverScales.RemoveAt(index);
            if (index < cardVisuals.Count) cardVisuals.RemoveAt(index);
            if (index < cardHovers.Count) cardHovers.RemoveAt(index);

            Destroy(card.gameObject);

            selection = Selection.Card;

            if (comboController != null)
                comboController.Invalidate();

            if (cards.Count == 0)
            {
                currentIndex = 0;
                return;
            }

            if (index < currentIndex)
                currentIndex--;

            currentIndex = Mathf.Clamp(currentIndex, 0, cards.Count - 1);

            RefreshCardsInstant();
        }



        public void SetHoverScale(RectTransform card, float multiplier)
        {
        }

        private Vector2 GetDefenseFlightPosition(int index)
        {
            Rect cardRect = cards[index].rect;
            float sideTop = cardRect.yMax * baseScales[index].y * sideScale;
            float overlapLine = cardRect.yMin + cardRect.height * CenterDefenseOverlap;
            float lift = sideTop - overlapLine * baseScales[index].y * centerScale * CenterDefenseScale;
            return centerPosition + Vector2.up * lift;
        }

        private void GetCardVisual(
            int index,
            out Vector2 position,
            out Vector3 scale,
            out Color color
        )
        {
            Vector3 baseScale = baseScales[index];

            int leftIndex = GetPreviousIndex(currentIndex);
            int rightIndex = GetNextIndex(currentIndex);

            if (index == currentIndex)
            {
                position = centerPosition;
                scale = baseScale * centerScale * hoverScales[index];
                color = centerColor;
                return;
            }

            if (index == leftIndex)
            {
                bool isCombo = IsLeftCombo();

                float spacing = isCombo
                    ? cardSpacing + comboSpacing
                    : cardSpacing;

                position = centerPosition + Vector2.left * spacing;
                scale = baseScale * sideScale * hoverScales[index];
                color = sideColor;
                return;
            }

            if (index == rightIndex)
            {
                bool isCombo = IsRightCombo();

                float spacing = isCombo
                    ? cardSpacing + comboSpacing
                    : cardSpacing;

                position = centerPosition + Vector2.right * spacing;
                scale = baseScale * sideScale * hoverScales[index];
                color = sideColor;
                return;
            }

            position = centerPosition;
            scale = baseScale * sideScale;
            color = sideColor;
        }

        private int GetPreviousIndex(int index)
        {
            int previousIndex = index - 1;

            if (previousIndex < 0)
                previousIndex = cards.Count - 1;

            return previousIndex;
        }


        private int GetNextIndex(int index)
        {
            int nextIndex = index + 1;

            if (nextIndex >= cards.Count)
                nextIndex = 0;

            return nextIndex;
        }

        private bool IsLeftCombo()
        {
            int leftIndex = GetPreviousIndex(currentIndex);

            CardData centerCard = cards[currentIndex].GetComponent<CardData>();
            CardData leftCard = cards[leftIndex].GetComponent<CardData>();

            return comboController != null && comboController.IsCombo(centerCard, leftCard);
        }

        private bool IsRightCombo()
        {
            int rightIndex = GetNextIndex(currentIndex);

            CardData centerCard = cards[currentIndex].GetComponent<CardData>();
            CardData leftCard = cards[rightIndex].GetComponent<CardData>();

            return comboController != null && comboController.IsCombo(centerCard, leftCard);
        }
    
    }
}

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
            StopAllCoroutines();
            isMoving = false;

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
            if (isMoving || cards.Count < 2)
                return;

            if (WasPressed(leftKeyCode))
                Step(-1);

            if (WasPressed(rightKeyCode))
                Step(1);

            if (selection != Selection.Card && WasPressed(Key.Enter))
                ActivateSelectedIcon();
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
                    targetScales[index] =
                        baseScales[index] * centerScale;
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
            for (int i = 0; i < cardHovers.Count; i++)
            {
                cardHovers[i].SetInteractable(true);
            }
        }



        public void SetHoverScale(RectTransform card, float multiplier)
        {
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

using System;
using System.Collections.Generic;
using UnityEngine;

public class CardComboController : MonoBehaviour
{
    [Serializable]
    private class CardCombo
    {
        public string firstCardID;
        public string secondCardID;

        public GameObject resultCardPrefab;
        public GameObject craftVFXPrefab;
    }

    private class ComboIconInstance
    {
        public RectTransform icon;
        public RectTransform firstCard;
        public RectTransform secondCard;
    }

    [Header("Links")]
    [SerializeField] private CardCarouselController carousel;
    [SerializeField] private CardCraftController craftController;
    [SerializeField] private RectTransform comboIconParent;
    [SerializeField] private RectTransform comboIconPrefab;

    [Header("Combinations")]
    [SerializeField] private List<CardCombo> combinations = new();

    [Header("Missing Combo Visual")]
    [SerializeField] private Vector2 comboIconOffset;
    [SerializeField] private Color missingColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField, Range(0f, 1f)] private float missingAlpha = 0.25f;

    private int lastCenterIndex = -1;
    private RectTransform missingCardInstance;

    private readonly Dictionary<RectTransform, Vector3> baseScales = new();
    private readonly List<ComboIconInstance> comboIconInstances = new();

    private void Awake()
    {
        if (carousel == null)
            return;
    }

    private void Update()
    {
        if (carousel == null || carousel.IsMoving)
            return;

        if (lastCenterIndex == carousel.CurrentIndex)
            return;

        lastCenterIndex = carousel.CurrentIndex;

        Refresh();
    }

    private void LateUpdate()
    {
        UpdateComboIconPositions();
    }

    public bool IsCombo(CardComboId first, CardComboId second)
    {
        if (first == null || second == null)
            return false;

        foreach (CardCombo combo in combinations)
        {
            bool direct =
                combo.firstCardID == first.CardID &&
                combo.secondCardID == second.CardID;

            bool reverse =
                combo.firstCardID == second.CardID &&
                combo.secondCardID == first.CardID;

            if (direct || reverse)
                return true;
        }

        return false;
    }

    public bool TryGetComboPartner(
        CardComboId card,
        out string partnerCardID)
    {
        partnerCardID = null;

        if (card == null)
            return false;

        foreach (CardCombo combo in combinations)
        {
            if (combo.firstCardID == card.CardID)
            {
                partnerCardID = combo.secondCardID;
                return true;
            }

            if (combo.secondCardID == card.CardID)
            {
                partnerCardID = combo.firstCardID;
                return true;
            }
        }

        return false;
    }

    private void Refresh()
    {
        ClearMissingCard();
        ClearComboIcons();

        IReadOnlyList<RectTransform> cards = carousel.Cards;


        int centerIndex = carousel.CurrentIndex;

        RectTransform centerCard = cards[centerIndex];

        CardComboId centerData =
            centerCard.GetComponent<CardComboId>();

        if (!TryGetComboPartner(
                centerData,
                out string partnerCardID))
        {
            RefreshComboIcons();
            return;
        }

        int partnerIndex =
            FindCardIndex(partnerCardID,centerIndex);
        
        if (partnerIndex < 0)
        {
            RefreshComboIcons();
            return;
        }

        int leftIndex =
            GetPreviousIndex(centerIndex);

        int rightIndex =
            GetNextIndex(centerIndex);

        if (partnerIndex == leftIndex ||
            partnerIndex == rightIndex)
        {
            RefreshComboIcons();
            return;
        }

        ShowMissingLayout(
            centerCard,
            cards[partnerIndex],
            leftIndex,
            rightIndex
        );

        RefreshComboIcons();
    }

    private void ShowMissingLayout(
        RectTransform centerCard,
        RectTransform sourceCard,
        int leftIndex,
        int rightIndex)
    {
        IReadOnlyList<RectTransform> cards = carousel.Cards;

        RectTransform rightCard = cards[rightIndex];

        rightCard.gameObject.SetActive(false);

        CreateMissingCard(
            centerCard,
            sourceCard
        );
    }

    private void RefreshComboIcons()
    {
        ClearComboIcons();

        IReadOnlyList<RectTransform> cards =
            carousel.Cards;

        List<RectTransform> visibleCards = new();

        for (int i = 0; i < cards.Count; i++)
        {
            RectTransform card = cards[i];

            if (!card.gameObject.activeSelf)
                continue;

            visibleCards.Add(card);
        }

        if (missingCardInstance != null &&
            missingCardInstance.gameObject.activeSelf)
        {
            visibleCards.Add(missingCardInstance);
        }

        visibleCards.Sort((a, b) =>
            a.anchoredPosition.x.CompareTo(
                b.anchoredPosition.x
            )
        );

        for (int i = 0; i < visibleCards.Count - 1; i++)
        {
            RectTransform firstCard =
                visibleCards[i];

            RectTransform secondCard =
                visibleCards[i + 1];

            CardComboId firstData =
                firstCard.GetComponent<CardComboId>();

            CardComboId secondData =
                secondCard.GetComponent<CardComboId>();

            if (firstData == null ||
                secondData == null)
            {
                continue;
            }

            if (!IsCombo(firstData, secondData))
                continue;

            SpawnComboIcon(
                firstCard,
                secondCard
            );
        }
    }

    private void SpawnComboIcon(
        RectTransform firstCard,
        RectTransform secondCard)
    {
        if (comboIconPrefab == null)
            return;

        RectTransform parent =
            comboIconParent != null
                ? comboIconParent
                : firstCard.parent as RectTransform;

        RectTransform icon = Instantiate(
            comboIconPrefab,
            parent
        );

        icon.gameObject.SetActive(true);

        icon.anchoredPosition =
            (firstCard.anchoredPosition +
             secondCard.anchoredPosition) * 0.5f +
            comboIconOffset;

        icon.SetAsLastSibling();

        comboIconInstances.Add(
            new ComboIconInstance
            {
                icon = icon,
                firstCard = firstCard,
                secondCard = secondCard
            }
        );

        CardCraftButton craftButton =
            icon.GetComponentInChildren<CardCraftButton>(true);

        if (craftButton != null)
        {

            bool firstMissing =
                firstCard == missingCardInstance;

            bool secondMissing =
                secondCard == missingCardInstance;

            craftButton.Setup(
                craftController,
                firstCard,
                secondCard,
                firstMissing,
                secondMissing
            );
        }
    }

    private void UpdateComboIconPositions()
        {
            for (int i = 0; i < comboIconInstances.Count; i++)
            {
                ComboIconInstance combo = comboIconInstances[i];

                if (combo.icon == null ||
                    combo.firstCard == null ||
                    combo.secondCard == null)
                {
                    continue;
                }

                combo.icon.anchoredPosition =
                    (combo.firstCard.anchoredPosition +
                     combo.secondCard.anchoredPosition) * 0.5f +
                    comboIconOffset;
            }
        }

        private void CreateMissingCard(
            RectTransform centerCard,
            RectTransform sourceCard)
        {
            bool sourceWasActive =
                sourceCard.gameObject.activeSelf;

            sourceCard.gameObject.SetActive(false);

            CardHover sourceHover = sourceCard.GetComponent<CardHover>();

            if (sourceHover != null)
            {
                sourceHover.CacheBaseSprite();
                sourceHover.ResetVisual();
            }
            
            GameObject instance = Instantiate(
                sourceCard.gameObject,
                sourceCard.parent
            );
            
            CardHover missingCardHover = instance.GetComponent<CardHover>();
            
            if (missingCardHover != null)
            {
                missingCardHover.SetInteractable(false);
                missingCardHover.SetMissingCard(true);
            }
            
            sourceCard.gameObject.SetActive(sourceWasActive);

            instance.name = sourceCard.name + "_MissingCombo";

            DisableCloneEffects(instance);

            missingCardInstance =
                instance.GetComponent<RectTransform>();

            if (missingCardInstance == null)
            {
                Destroy(instance);
                return;
            }

            missingCardInstance.anchoredPosition =
                carousel.GetComboPosition(false);

            missingCardInstance.localScale =
                carousel.GetComboScale(sourceCard);

            CanvasGroup canvasGroup =
                instance.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup =
                    instance.AddComponent<CanvasGroup>();

            canvasGroup.alpha = missingAlpha;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;

            instance.SetActive(true);

            CardTint sourceVisual =
                sourceCard.GetComponent<CardTint>();

            CardTint visual =
                instance.GetComponent<CardTint>();

            if (visual != null)
            {
                visual.CopyBaseColorsFrom(sourceVisual);
                visual.SetTint(missingColor);
            }

            missingCardInstance.SetAsLastSibling();
        }

        private void DisableCloneEffects(GameObject instance)
        {
            CardHover[] cardHovers =
                instance.GetComponentsInChildren<CardHover>(true);

            ParticleSystem[] particles =
                instance.GetComponentsInChildren<ParticleSystem>(true);

            foreach (ParticleSystem particle in particles)
            {
                particle.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );

                particle.gameObject.SetActive(false);
            }

            Animator[] animators =
                instance.GetComponentsInChildren<Animator>(true);

            foreach (Animator animator in animators)
                animator.enabled = false;

            TrailRenderer[] trails =
                instance.GetComponentsInChildren<TrailRenderer>(true);

            foreach (TrailRenderer trail in trails)
            {
                trail.Clear();
                trail.enabled = false;
            }

            AudioSource[] audioSources =
                instance.GetComponentsInChildren<AudioSource>(true);

            foreach (AudioSource audioSource in audioSources)
            {
                audioSource.Stop();
                audioSource.enabled = false;
            }
        }

        private void ClearMissingCard()
        {
            if (missingCardInstance == null)
                return;

            missingCardInstance.gameObject.SetActive(false);

            Destroy(missingCardInstance.gameObject);

            missingCardInstance = null;
        }

        private int FindCardIndex(
            string cardID,
            int excludedIndex)
        {
            IReadOnlyList<RectTransform> cards =
                carousel.Cards;

            for (int i = 0; i < cards.Count; i++)
            {
                if (i == excludedIndex)
                    continue;

                CardComboId data =
                    cards[i].GetComponent<CardComboId>();

                if (data != null &&
                    data.CardID == cardID)
                {
                    return i;
                }
            }

            return -1;
        }

        private int GetPreviousIndex(int index)
        {
            index--;

            if (index < 0)
                index = carousel.Cards.Count - 1;

            return index;
        }

        private int GetNextIndex(int index)
        {
            index++;

            if (index >= carousel.Cards.Count)
                index = 0;

            return index;
        }

        private void ClearComboIcons()
        {
            for (int i = 0; i < comboIconInstances.Count; i++)
            {
                ComboIconInstance combo =
                    comboIconInstances[i];

                if (combo.icon == null)
                    continue;

                combo.icon.gameObject.SetActive(false);
                Destroy(combo.icon.gameObject);
            }

            comboIconInstances.Clear();
        }

        public bool TryGetCombo(
            CardComboId first,
            CardComboId second,
            out GameObject resultCardPrefab,
            out GameObject craftVFXPrefab)
        {
            resultCardPrefab = null;
            craftVFXPrefab = null;

            if (first == null || second == null)
                return false;

            foreach (CardCombo combo in combinations)
            {
                bool direct =
                    combo.firstCardID == first.CardID &&
                    combo.secondCardID == second.CardID;

                bool reverse =
                    combo.firstCardID == second.CardID &&
                    combo.secondCardID == first.CardID;

                if (!direct && !reverse)
                    continue;

                resultCardPrefab = combo.resultCardPrefab;
                craftVFXPrefab = combo.craftVFXPrefab;

                return true;
            }

            return false;
        }
    }
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardCraftController : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private CardComboController comboController;

    [Header("Craft Layer")]
    [SerializeField] private RectTransform craftRoot;
    [SerializeField] private CanvasGroup backgroundGroup;

    [Header("Result Card")]
    [SerializeField] private Vector3 resultOffset;
    [SerializeField] private Vector3 resultScale = Vector3.one;

    [Header("Craft VFX")]
    [SerializeField] private Vector3 vfxOffset;
    [SerializeField] private Vector3 vfxScale = Vector3.one;
    
    [Header("BG Cards Visual")]
    [SerializeField] private Image backgroundTint;
    [SerializeField] private Color backgroundTintColor = Color.black;
    [SerializeField, Range(0f, 1f)] private float backgroundAlpha = 0.25f;
    
    private GameObject resultCardInstance;
    private GameObject craftVFXInstance;

    public void TryCraft(
        RectTransform firstCard,
        RectTransform secondCard,
        bool firstMissing,
        bool secondMissing)
    {

        if (firstMissing || secondMissing)
        {
            return;
        }

        CardComboId firstData =
            firstCard.GetComponent<CardComboId>();

        CardComboId secondData =
            secondCard.GetComponent<CardComboId>();

        if (firstData == null || secondData == null) return;

        if (!comboController.TryGetCombo(
                firstData,
                secondData,
                out GameObject resultCardPrefab,
                out GameObject craftVFXPrefab)) return;

        ShowCraftResult(
            resultCardPrefab,
            craftVFXPrefab
        );
    }

    private void ShowCraftResult(
        GameObject resultCardPrefab,
        GameObject craftVFXPrefab)
    {
        ClearCraftResult();

        if (backgroundGroup != null)
            backgroundGroup.alpha = backgroundAlpha;

        if (backgroundTint != null)
        {
            Color color = backgroundTintColor;
            color.a = backgroundTintColor.a;

            backgroundTint.color = color;
            backgroundTint.gameObject.SetActive(true);
        }

        if (resultCardPrefab != null)
        {
            resultCardInstance = Instantiate(
                resultCardPrefab,
                craftRoot
            );

            resultCardInstance.SetActive(true);

            RectTransform resultRect =
                resultCardInstance.GetComponent<RectTransform>();

            if (resultRect != null)
            {
                resultRect.anchoredPosition3D = resultOffset;
                resultRect.localScale = resultScale;
                resultRect.localRotation = Quaternion.identity;
            }

            CardHover cardHover =
                resultCardInstance.GetComponent<CardHover>();

            if (cardHover != null)
                cardHover.enabled = false;

            CanvasGroup resultGroup =
                resultCardInstance.GetComponent<CanvasGroup>();

            if (resultGroup != null)
                resultGroup.alpha = 1f;

            resultCardInstance.transform.SetAsLastSibling();
            
            EventTrigger trigger =
                resultCardInstance.GetComponent<EventTrigger>();

            if (trigger == null)
                trigger = resultCardInstance.AddComponent<EventTrigger>();

            EventTrigger.Entry clickEntry =
                new EventTrigger.Entry
                {
                    eventID = EventTriggerType.PointerClick
                };

            clickEntry.callback.AddListener(
                _ => CloseCraftResult()
            );

            trigger.triggers.Add(clickEntry);
        }

        if (craftVFXPrefab != null)
        {
            craftVFXInstance = Instantiate(
                craftVFXPrefab,
                craftRoot
            );
        
            craftVFXInstance.SetActive(true);
        
            RectTransform vfxRect =
                craftVFXInstance.GetComponent<RectTransform>();
        
            if (vfxRect != null)
            {
                vfxRect.anchoredPosition3D =
                    resultOffset + vfxOffset;
        
                Vector3 baseVFXScale = vfxRect.localScale;
                
                vfxRect.localScale =
                    Vector3.Scale(
                        baseVFXScale,
                        resultScale
                    );
        
                vfxRect.localRotation =
                    Quaternion.identity;
            }
        
            if (resultCardInstance != null)
            {
                craftVFXInstance.transform.SetSiblingIndex(
                    resultCardInstance.transform.GetSiblingIndex()
                );
            }
        }
    }

    private void ClearCraftResult()
    {
        if (resultCardInstance != null)
            Destroy(resultCardInstance);

        if (craftVFXInstance != null)
            Destroy(craftVFXInstance);

        resultCardInstance = null;
        craftVFXInstance = null;
    }
    
    public void CloseCraftResult()
    {
        ClearCraftResult();

        if (backgroundGroup != null)
            backgroundGroup.alpha = 1f;

        if (backgroundTint != null)
            backgroundTint.gameObject.SetActive(false);
    }
}
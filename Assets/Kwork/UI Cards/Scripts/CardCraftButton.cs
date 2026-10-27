using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardCraftButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Button button;
    [SerializeField] private string emissionProperty = "_HoverEmission";

    private CardCraftController craftController;

    private RectTransform firstCard;
    private RectTransform secondCard;

    private bool firstMissing;
    private bool secondMissing;
    private bool hasMissingCard;

    private Material[] materialsList;

    private void Awake()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        
        materialsList = new Material[graphics.Length];

        for (int i = 0; i < graphics.Length; i++)
        {
            if(graphics[i] == null) continue;
            
            Material instance = new Material(graphics[i].material);
            
            graphics[i].material = instance;
            materialsList[i] = instance;
            
            instance.SetFloat(emissionProperty, 0f);
        }
        
    }

    public void Setup(
        CardCraftController controller,
        RectTransform first,
        RectTransform second,
        bool firstIsMissing,
        bool secondIsMissing)
    {
        craftController = controller;
        firstCard = first;
        secondCard = second;
        firstMissing = firstIsMissing;
        secondMissing = secondIsMissing;

        hasMissingCard = firstMissing || secondMissing;

        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    private void SetEmission(float value)
    {
        if (materialsList == null) return;

        foreach (Material material in materialsList)
        {
            if (material == null) continue;
            material.SetFloat(emissionProperty, value);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hasMissingCard) return;

        SetEmission(1f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (materialsList == null) return;

        SetEmission(0f);
    }

    private void OnClick()
    {
        craftController.TryCraft(
            firstCard,
            secondCard,
            firstMissing,
            secondMissing
        );
    }
}
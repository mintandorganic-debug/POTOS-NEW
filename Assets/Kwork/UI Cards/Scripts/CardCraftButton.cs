using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardCarouselUI
{

    public class CardCraftButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private string emissionProperty = "_HoverEmission";
        [SerializeField] private float litEmission = 1f;
        [SerializeField] private float selectedEmission = 2f;

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

            SetEmission(hasMissingCard ? 0f : litEmission);

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

        public void SetHighlighted(bool highlighted)
        {
            SetEmission(hasMissingCard ? 0f : (highlighted ? selectedEmission : litEmission));
        }

        public void Activate()
        {
            OnClick();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (hasMissingCard) return;

            SetEmission(selectedEmission);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (materialsList == null) return;

            SetEmission(hasMissingCard ? 0f : litEmission);
        }

        private void OnClick()
        {
            if (craftController == null)
                return;

            craftController.TryCraft(
                firstCard,
                secondCard,
                firstMissing,
                secondMissing
            );
        }
    }
}

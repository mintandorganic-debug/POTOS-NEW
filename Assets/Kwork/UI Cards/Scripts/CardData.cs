using UnityEngine;

namespace CardCarouselUI
{

    public class CardData : MonoBehaviour
    {
        [SerializeField] private string cardID;

        public string CardID => cardID;
    }
}

using System.Collections.Generic;
using UnityEngine;
using static Card;

[CreateAssetMenu(
    fileName = "CardData",
    menuName = "Game/Card Data")]
public class CardData : ScriptableObject
{
    [Header("Card")]
    [SerializeField] private string cardName;

    [SerializeField]
    public CardClass CardClass =
        CardClass.AttackLight;

    [TextArea(3, 8)]
    [SerializeField] private string description;

    [SerializeField]
    public CE_Base cardEffect;

    [SerializeField]
    private List<CardData> interactableCards;

    [Header("Action")]
    [SerializeField] private string actionId;

    [Header("Visual")]
    [SerializeField] public Color emissiveColor;

    [SerializeField] private Sprite sprite;

    [SerializeField] public Texture2D texture;

    public string CardName =>
        cardName;

    public string Description =>
        description;

    public string ActionId =>
        actionId;

    public Sprite Sprite =>
        sprite;
}
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

    [Header("Sound")]
    [Tooltip("ATK_*_VIC_HIT: plays on the victim when this card hits.")]
    [SerializeField] private AudioClip attackHitSound;

    [Range(0f, 1f)]
    [SerializeField] private float attackHitVolume = 1f;

    [Tooltip("AFTERMATH_*: plays in the audience when the attack with " +
             "this card is cancelled and the item is thrown away.")]
    [SerializeField] private AudioClip aftermathSound;

    [Range(0f, 1f)]
    [SerializeField] private float aftermathVolume = 1f;

    [Tooltip("Seconds from the cancel until the item reaches the audience " +
             "(tune to the throw animation).")]
    [SerializeField] private float aftermathDelay = 0.6f;

    public AudioClip AttackHitSound =>
        attackHitSound;

    public float AttackHitVolume =>
        attackHitVolume;

    public AudioClip AftermathSound =>
        aftermathSound;

    public float AftermathVolume =>
        aftermathVolume;

    public float AftermathDelay =>
        aftermathDelay;

    public string CardName =>
        cardName;

    public string Description =>
        description;

    public string ActionId =>
        actionId;

    public Sprite Sprite =>
        sprite;
}
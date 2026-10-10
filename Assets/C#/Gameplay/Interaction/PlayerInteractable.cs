using UnityEngine;

public class PlayerInteractable : InteractableObject
{
    [Header("Visual")]
    [SerializeField] private SkinnedMeshRenderer targetRenderer;
    [SerializeField] private GameObject enemyCanvas;

    [SerializeField] private Transform enemyCardMarker;

    private static readonly int FresnelEnabled =
        Shader.PropertyToID("_IsFresnelEnabled");

    private MaterialPropertyBlock mpb;

    public Transform EnemyCardMarker =>
        enemyCardMarker;

    private void Awake()
    {
        mpb = new MaterialPropertyBlock();
    }

    public override void OnStartAuthority()
    {
        enabled = false;

        gameObject.layer =
            LayerMask.NameToLayer("Default");
    }

    public override void Interact()
    {
        if (GameMaster.Instance.CurrentPhase != GameMaster.GamePhase.SelectTarget)
            return;
        SelectAsTarget();
    }

    // =========================================================
    // TARGET SELECTION
    // =========================================================

    public void SelectAsTarget()
    {
        if (AttackSequence.Instance == null)
            return;

        AttackSequence.Instance.CmdSetTarget(
            netIdentity);
    }

    // =========================================================
    // ATTACK
    // =========================================================

    public void Attack(
        Card card,
        Player attacker)
    {
        if (card == null ||
            attacker == null)
        {
            return;
        }

        Debug.Log(
            $"{attacker.name} attacks {name} with {card.name}");

        // Actual attack logic goes here.
    }

    // =========================================================
    // VISUAL
    // =========================================================

    public override void OnLookEnter()
    {
        if (targetRenderer == null)
            return;
        enemyCanvas?.SetActive(true);


        if (GameMaster.Instance.CurrentPhase != GameMaster.GamePhase.SelectTarget)
            return;


        targetRenderer.GetPropertyBlock(mpb);

        mpb.SetFloat(
            FresnelEnabled,
            1f);

        targetRenderer.SetPropertyBlock(mpb);
    }

    public override void OnLookExit()
    {
        if (targetRenderer == null)
            return;
        enemyCanvas?.SetActive(false);


        targetRenderer.GetPropertyBlock(mpb);

        mpb.SetFloat(
            FresnelEnabled,
            0f);

        targetRenderer.SetPropertyBlock(mpb);
    }
}
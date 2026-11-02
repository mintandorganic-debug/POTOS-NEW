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
        GameLog.Write(
            "PlayerInteractable",
            $"Awake object={name} renderer={(targetRenderer != null ? "ok" : "NULL")} canvas={(enemyCanvas != null ? "ok" : "NULL")} cardMarker={(enemyCardMarker != null ? enemyCardMarker.name : "NULL")}");

        mpb = new MaterialPropertyBlock();
    }

    public override void OnStartAuthority()
    {
        GameLog.Write(
            "PlayerInteractable",
            $"OnStartAuthority object={name} netId={netId}: component disabled, layer set to Default");

        enabled = false;

        gameObject.layer =
            LayerMask.NameToLayer("Default");
    }

    public override void Interact()
    {
        GameLog.Write(
            "PlayerInteractable",
            $"Interact object={name} netId={netId} phase={(GameMaster.Instance != null ? GameMaster.Instance.CurrentPhase.ToString() : "NULL")}");

        if (GameMaster.Instance == null ||
            GameMaster.Instance.CurrentPhase !=
                GameMaster.GamePhase.SelectTarget)
        {
            GameLog.Write(
                "PlayerInteractable",
                $"Interact ignored object={name}: phase is not SelectTarget");

            return;
        }

        SelectAsTarget();
    }

    // =========================================================
    // TARGET SELECTION
    // =========================================================

    public void SelectAsTarget()
    {
        GameLog.Write(
            "PlayerInteractable",
            $"SelectAsTarget object={name} netId={netId}");

        if (AttackSequence.Instance == null)
        {
            GameLog.Write(
                "PlayerInteractable",
                $"SelectAsTarget skipped object={name}: AttackSequence.Instance is NULL");

            return;
        }

        GameLog.Write(
            "PlayerInteractable",
            $"SelectAsTarget object={name}: sending CmdSetTarget netId={netId}");

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
        GameLog.Write(
            "PlayerInteractable",
            $"Attack object={name} card={(card != null ? card.netId.ToString() : "NULL")} attacker={(attacker != null ? attacker.name : "NULL")}");

        if (card == null ||
            attacker == null)
        {
            GameLog.Write(
                "PlayerInteractable",
                $"Attack skipped object={name}: card or attacker is NULL");

            return;
        }
    }

    // =========================================================
    // VISUAL
    // =========================================================

    public override void OnLookEnter()
    {
        GameLog.Write(
            "PlayerInteractable",
            $"OnLookEnter object={name} phase={(GameMaster.Instance != null ? GameMaster.Instance.CurrentPhase.ToString() : "NULL")}");

        if (targetRenderer == null)
        {
            GameLog.Write(
                "PlayerInteractable",
                $"OnLookEnter ignored object={name}: targetRenderer is NULL");

            return;
        }

        GameLog.Write(
            "PlayerInteractable",
            $"OnLookEnter object={name}: enemy canvas shown");

        enemyCanvas?.SetActive(true);

        if (GameMaster.Instance == null ||
            GameMaster.Instance.CurrentPhase !=
                GameMaster.GamePhase.SelectTarget)
        {
            GameLog.Write(
                "PlayerInteractable",
                $"OnLookEnter object={name}: phase is not SelectTarget, fresnel not enabled");

            return;
        }

        GameLog.Write(
            "PlayerInteractable",
            $"OnLookEnter object={name}: fresnel on");

        targetRenderer.GetPropertyBlock(mpb);

        mpb.SetFloat(
            FresnelEnabled,
            1f);

        targetRenderer.SetPropertyBlock(mpb);
    }

    public override void OnLookExit()
    {
        GameLog.Write(
            "PlayerInteractable",
            $"OnLookExit object={name}");

        if (targetRenderer == null)
        {
            GameLog.Write(
                "PlayerInteractable",
                $"OnLookExit ignored object={name}: targetRenderer is NULL");

            return;
        }

        GameLog.Write(
            "PlayerInteractable",
            $"OnLookExit object={name}: enemy canvas hidden, fresnel off");

        enemyCanvas?.SetActive(false);

        targetRenderer.GetPropertyBlock(mpb);

        mpb.SetFloat(
            FresnelEnabled,
            0f);

        targetRenderer.SetPropertyBlock(mpb);
    }
}
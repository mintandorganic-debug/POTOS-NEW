using Mirror;
using UnityEngine;

public class StealableObject : InteractableObject
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private int accesoryIndex;
    [SerializeField] Player owner;

    private static readonly int FresnelEnabled =
        Shader.PropertyToID("_IsFresnelEnabled");

    private MaterialPropertyBlock mpb;

    private void Awake()
    {
        GameLog.Write(
            "StealableObject",
            $"Awake object={name} owner={(owner != null ? owner.name : "NULL")} renderer={(targetRenderer != null ? "ok" : "NULL")} accessoryIndex={accesoryIndex}");

        mpb = new MaterialPropertyBlock();
    }

    public override void OnStartAuthority()
    {
        GameLog.Write(
            "StealableObject",
            $"OnStartAuthority object={name} netId={netId}: component disabled");

        enabled = false;
    }

    public override void Interact()
    {
        GameLog.Write(
            "StealableObject",
            $"Interact object={name} owner={(owner != null ? owner.name : "NULL")} accessoryIndex={accesoryIndex} phase={(GameMaster.Instance != null ? GameMaster.Instance.CurrentPhase.ToString() : "NULL")}");

        if (GameMaster.Instance.CurrentPhase != GameMaster.GamePhase.Steal)
        {
            GameLog.Write(
                "StealableObject",
                $"Interact ignored object={name}: phase is not Steal");

            return;
        }

        GameLog.Write(
            "StealableObject",
            $"Interact object={name}: stealing accessoryIndex={accesoryIndex} from owner={(owner != null ? owner.name : "NULL")}");

        PlayerActions.Local.TryStealAccessory(owner, accesoryIndex);
    }

    // =========================================================
    // VISUAL
    // =========================================================

    public override void OnLookEnter()
    {
        GameLog.Write(
            "StealableObject",
            $"OnLookEnter object={name} phase={(GameMaster.Instance != null ? GameMaster.Instance.CurrentPhase.ToString() : "NULL")}");

        if (targetRenderer == null)
        {
            GameLog.Write(
                "StealableObject",
                $"OnLookEnter ignored object={name}: targetRenderer is NULL");

            return;
        }
        if (GameMaster.Instance.CurrentPhase != GameMaster.GamePhase.Steal)
        {
            GameLog.Write(
                "StealableObject",
                $"OnLookEnter ignored object={name}: phase is not Steal");

            return;
        }

        GameLog.Write(
            "StealableObject",
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
            "StealableObject",
            $"OnLookExit object={name}");

        if (targetRenderer == null)
        {
            GameLog.Write(
                "StealableObject",
                $"OnLookExit ignored object={name}: targetRenderer is NULL");

            return;
        }

        GameLog.Write(
            "StealableObject",
            $"OnLookExit object={name}: fresnel off");

        targetRenderer.GetPropertyBlock(mpb);

        mpb.SetFloat(
            FresnelEnabled,
            0f);

        targetRenderer.SetPropertyBlock(mpb);
    }
}
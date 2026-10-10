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
        mpb = new MaterialPropertyBlock();
    }

    public override void OnStartAuthority()
    {
        enabled = false;
    }

    public override void Interact()
    {
        if (GameMaster.Instance.CurrentPhase != GameMaster.GamePhase.Steal)
            return;

        PlayerActions.Local.TryStealAccessory(owner, accesoryIndex);
    }

    // =========================================================
    // VISUAL
    // =========================================================

    public override void OnLookEnter()
    {
        if (targetRenderer == null)
            return;
        if (GameMaster.Instance.CurrentPhase != GameMaster.GamePhase.Steal)
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

        targetRenderer.GetPropertyBlock(mpb);

        mpb.SetFloat(
            FresnelEnabled,
            0f);

        targetRenderer.SetPropertyBlock(mpb);
    }
}
using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCosmetics : NetworkBehaviour
{
    [SerializeField] private GameObject torso;
    [SerializeField] private List<GameObject> cosmetics;

    private static readonly Color32[] AvailableColors =
    {
        new(122, 30, 44, 255),
        new(242, 233, 216, 255),
        new(14, 122, 75, 255),
        new(30, 58, 110, 255),
        new(215, 193, 138, 255),
    };

    private static Queue<byte> freeColorIndices;

    [SyncVar(hook = nameof(OnColorChanged))]
    private byte colorIndex;

    private readonly SyncList<bool> cosmeticStates = new();

    private Renderer meshRenderer;
    private MaterialPropertyBlock mpb;

    private void Awake()
    {
        GameLog.Write(
            "PlayerCosmetics",
            $"Awake netId={netId} torso={(torso != null ? torso.name : "NULL")} cosmetics={(cosmetics != null ? cosmetics.Count.ToString() : "NULL")}");

        meshRenderer = torso.GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();

        cosmeticStates.Callback += OnCosmeticStatesChanged;

        GameLog.Write(
            "PlayerCosmetics",
            $"Awake done netId={netId} renderer={(meshRenderer != null ? meshRenderer.name : "NULL")}");
    }

    [Server]
    private static void InitializeColors()
    {
        GameLog.Write(
            "PlayerCosmetics",
            $"InitializeColors alreadyInitialized={freeColorIndices != null} availableColors={AvailableColors.Length}");

        if (freeColorIndices != null)
        {
            GameLog.Write(
                "PlayerCosmetics",
                $"InitializeColors skipped: already initialized free={freeColorIndices.Count}");
            return;
        }

        List<byte> indices = new();

        for (byte i = 0; i < AvailableColors.Length; i++)
            indices.Add(i);

        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) =
                (indices[j], indices[i]);
        }

        freeColorIndices =
            new Queue<byte>(indices);

        GameLog.Write(
            "PlayerCosmetics",
            $"InitializeColors done free={freeColorIndices.Count}");
    }

    public override void OnStartServer()
    {
        GameLog.Write(
            "PlayerCosmetics",
            $"OnStartServer netId={netId} cosmetics={cosmetics.Count}");

        base.OnStartServer();

        InitializeColors();

        if (freeColorIndices.Count == 0)
        {
            GameLog.Write(
                "PlayerCosmetics",
                $"OnStartServer no free player colors netId={netId}");

            Debug.LogError("No free player colors!");
            return;
        }

        colorIndex =
            freeColorIndices.Dequeue();

        GameLog.Write(
            "PlayerCosmetics",
            $"OnStartServer color assigned netId={netId} colorIndex={colorIndex} freeLeft={freeColorIndices.Count}");

        cosmeticStates.Clear();

        for (int i = 0; i < cosmetics.Count; i++)
            cosmeticStates.Add(false);

        GameLog.Write(
            "PlayerCosmetics",
            $"OnStartServer cosmetic states initialized netId={netId} count={cosmeticStates.Count}");
    }

    public override void OnStopServer()
    {
        GameLog.Write(
            "PlayerCosmetics",
            $"OnStopServer netId={netId} colorIndex={colorIndex} queue={(freeColorIndices != null ? "set" : "NULL")}");

        base.OnStopServer();

        if (freeColorIndices != null)
        {
            GameLog.Write(
                "PlayerCosmetics",
                $"OnStopServer color returned to pool netId={netId} colorIndex={colorIndex}");

            freeColorIndices.Enqueue(colorIndex);
        }
    }

    public override void OnStartClient()
    {
        GameLog.Write(
            "PlayerCosmetics",
            $"OnStartClient netId={netId} colorIndex={colorIndex} cosmeticStates={cosmeticStates.Count}");

        base.OnStartClient();

        ApplyColor(
            AvailableColors[colorIndex]);

        ApplyAllCosmetics();
    }

    public override void OnStartLocalPlayer()
    {
        GameLog.Write(
            "PlayerCosmetics",
            $"OnStartLocalPlayer netId={netId}");

        base.OnStartLocalPlayer();

        int ignoreClientLayer =
            LayerMask.NameToLayer("IgnoreClient");

        if (ignoreClientLayer == -1)
        {
            GameLog.Write(
                "PlayerCosmetics",
                $"OnStartLocalPlayer aborted: layer IgnoreClient was not found netId={netId}");

            Debug.LogWarning(
                "PlayerCosmetics: Layer 'IgnoreClient' was not found.",
                this);

            return;
        }

        SetLayerRecursively(
            torso,
            ignoreClientLayer);

        foreach (GameObject cosmetic in cosmetics)
        {
            SetLayerRecursively(
                cosmetic,
                ignoreClientLayer);
        }

        GameLog.Write(
            "PlayerCosmetics",
            $"OnStartLocalPlayer layers applied netId={netId} layer={ignoreClientLayer} cosmetics={cosmetics.Count}");
    }

    private static void SetLayerRecursively(
        GameObject target,
        int layer)
    {
        GameLog.Write(
            "PlayerCosmetics",
            $"SetLayerRecursively target={(target != null ? target.name : "NULL")} layer={layer}");

        if (target == null)
        {
            GameLog.Write(
                "PlayerCosmetics",
                $"SetLayerRecursively skipped: target is NULL layer={layer}");
            return;
        }

        target.layer = layer;

        foreach (Transform child in target.transform)
        {
            SetLayerRecursively(
                child.gameObject,
                layer);
        }
    }

    private void OnColorChanged(
        byte oldIndex,
        byte newIndex)
    {
        GameLog.Write(
            "PlayerCosmetics",
            $"OnColorChanged netId={netId} old={oldIndex} new={newIndex}");

        if (newIndex >= AvailableColors.Length)
        {
            GameLog.Write(
                "PlayerCosmetics",
                $"OnColorChanged ignored: index out of range netId={netId} new={newIndex} colors={AvailableColors.Length}");
            return;
        }

        ApplyColor(
            AvailableColors[newIndex]);
    }

    private void OnCosmeticStatesChanged(
        SyncList<bool>.Operation operation,
        int index,
        bool oldValue,
        bool newValue)
    {
        GameLog.Write(
            "PlayerCosmetics",
            $"OnCosmeticStatesChanged netId={netId} operation={operation} index={index} old={oldValue} new={newValue}");

        if (operation == SyncList<bool>.Operation.OP_CLEAR)
        {
            GameLog.Write(
                "PlayerCosmetics",
                $"OnCosmeticStatesChanged OP_CLEAR: reapplying all cosmetics netId={netId}");

            ApplyAllCosmetics();
            return;
        }

        if (operation == SyncList<bool>.Operation.OP_ADD)
        {
            if (index >= 0 &&
                index < cosmetics.Count &&
                cosmetics[index] != null)
            {
                GameLog.Write(
                    "PlayerCosmetics",
                    $"OnCosmeticStatesChanged OP_ADD set active netId={netId} index={index} cosmetic={cosmetics[index].name} active={newValue}");

                cosmetics[index].SetActive(newValue);
            }
            else
            {
                GameLog.Write(
                    "PlayerCosmetics",
                    $"OnCosmeticStatesChanged OP_ADD ignored: invalid index or NULL cosmetic netId={netId} index={index} cosmetics={cosmetics.Count}");
            }

            return;
        }

        if (operation == SyncList<bool>.Operation.OP_SET)
        {
            if (index >= 0 &&
                index < cosmetics.Count &&
                cosmetics[index] != null)
            {
                GameLog.Write(
                    "PlayerCosmetics",
                    $"OnCosmeticStatesChanged OP_SET set active netId={netId} index={index} cosmetic={cosmetics[index].name} old={oldValue} new={newValue}");

                cosmetics[index].SetActive(newValue);
            }
            else
            {
                GameLog.Write(
                    "PlayerCosmetics",
                    $"OnCosmeticStatesChanged OP_SET ignored: invalid index or NULL cosmetic netId={netId} index={index} cosmetics={cosmetics.Count}");
            }

            return;
        }

        GameLog.Write(
            "PlayerCosmetics",
            $"OnCosmeticStatesChanged other operation: reapplying all cosmetics netId={netId} operation={operation}");

        ApplyAllCosmetics();
    }

    private void ApplyAllCosmetics()
    {
        GameLog.Write(
            "PlayerCosmetics",
            $"ApplyAllCosmetics netId={netId} cosmetics={cosmetics.Count} states={cosmeticStates.Count}");

        int count = Mathf.Min(
            cosmetics.Count,
            cosmeticStates.Count);

        for (int i = 0; i < count; i++)
        {
            if (cosmetics[i] != null)
            {
                GameLog.Write(
                    "PlayerCosmetics",
                    $"ApplyAllCosmetics set active netId={netId} index={i} cosmetic={cosmetics[i].name} active={cosmeticStates[i]}");

                cosmetics[i].SetActive(
                    cosmeticStates[i]);
            }
        }
    }

    private void ApplyColor(Color32 color)
    {
        GameLog.Write(
            "PlayerCosmetics",
            $"ApplyColor netId={netId} color=({color.r},{color.g},{color.b},{color.a}) renderer={(meshRenderer != null ? meshRenderer.name : "NULL")}");

        for (int i = 1; i <= 2; i++)
        {
            meshRenderer.GetPropertyBlock(
                mpb,
                i);

            mpb.SetColor(
                "_BaseColor",
                color);

            mpb.SetColor(
                "_EmissiveColor",
                (Color)color * 3f);

            meshRenderer.SetPropertyBlock(
                mpb,
                i);
        }
    }

    [Server]
    private void ToggleCosmeticOnServer(int index)
    {
        GameLog.Write(
            "PlayerCosmetics",
            $"ToggleCosmeticOnServer netId={netId} index={index} states={cosmeticStates.Count}");

        if (index < 0 ||
            index >= cosmeticStates.Count)
        {
            GameLog.Write(
                "PlayerCosmetics",
                $"ToggleCosmeticOnServer rejected: invalid index netId={netId} index={index} states={cosmeticStates.Count}");

            Debug.LogError(
                $"Invalid cosmetic index: {index}",
                this);

            return;
        }

        GameLog.Write(
            "PlayerCosmetics",
            $"ToggleCosmeticOnServer toggled netId={netId} index={index} old={cosmeticStates[index]} new={!cosmeticStates[index]}");

        cosmeticStates[index] =
            !cosmeticStates[index];
    }

    [Command]
    private void CmdToggleCosmetic(int index)
    {
        GameLog.Write(
            "PlayerCosmetics",
            $"CmdToggleCosmetic netId={netId} index={index}");

        ToggleCosmeticOnServer(index);
    }

    private void ToggleCosmetics(int index)
    {
        GameLog.Write(
            "PlayerCosmetics",
            $"ToggleCosmetics netId={netId} index={index} isServer={isServer} isLocalPlayer={isLocalPlayer}");

        if (isServer)
        {
            GameLog.Write(
                "PlayerCosmetics",
                $"ToggleCosmetics toggling on server netId={netId} index={index}");

            ToggleCosmeticOnServer(index);
            return;
        }

        if (!isLocalPlayer)
        {
            GameLog.Write(
                "PlayerCosmetics",
                $"ToggleCosmetics ignored: not local player netId={netId} index={index}");
            return;
        }

        GameLog.Write(
            "PlayerCosmetics",
            $"ToggleCosmetics sending command to server netId={netId} index={index}");

        CmdToggleCosmetic(index);
    }

    [ContextMenu("Toggle clown")]
    public void ToggleClown()
    {
        GameLog.Write(
            "PlayerCosmetics",
            $"ToggleClown netId={netId}");

        ToggleCosmetics(0);
    }

    [ContextMenu("Toggle cosmetic 0")]
    public void ToggleCosmeticsEditor()
    {
        GameLog.Write(
            "PlayerCosmetics",
            $"ToggleCosmeticsEditor netId={netId}");

        ToggleClown();
    }
}
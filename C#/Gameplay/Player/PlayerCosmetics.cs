using System.Collections.Generic;
using Mirror;
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
        meshRenderer = torso.GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();

        cosmeticStates.Callback += OnCosmeticStatesChanged;
    }

    [Server]
    private static void InitializeColors()
    {
        if (freeColorIndices != null)
            return;

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
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        InitializeColors();

        if (freeColorIndices.Count == 0)
        {
            Debug.LogError("No free player colors!");
            return;
        }

        colorIndex =
            freeColorIndices.Dequeue();

        cosmeticStates.Clear();

        for (int i = 0; i < cosmetics.Count; i++)
            cosmeticStates.Add(false);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        if (freeColorIndices != null)
            freeColorIndices.Enqueue(colorIndex);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        ApplyColor(
            AvailableColors[colorIndex]);

        ApplyAllCosmetics();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        torso.layer = LayerMask.NameToLayer("IgnoreClient");
    }

    private void OnColorChanged(
        byte oldIndex,
        byte newIndex)
    {
        if (newIndex >= AvailableColors.Length)
            return;

        ApplyColor(
            AvailableColors[newIndex]);
    }

    private void OnCosmeticStatesChanged(
        SyncList<bool>.Operation operation,
        int index,
        bool oldValue,
        bool newValue)
    {
        if (operation == SyncList<bool>.Operation.OP_CLEAR)
        {
            ApplyAllCosmetics();
            return;
        }

        if (operation == SyncList<bool>.Operation.OP_ADD)
        {
            if (index >= 0 &&
                index < cosmetics.Count &&
                cosmetics[index] != null)
            {
                cosmetics[index].SetActive(newValue);
            }

            return;
        }

        if (operation == SyncList<bool>.Operation.OP_SET)
        {
            if (index >= 0 &&
                index < cosmetics.Count &&
                cosmetics[index] != null)
            {
                cosmetics[index].SetActive(newValue);
            }

            return;
        }

        ApplyAllCosmetics();
    }

    private void ApplyAllCosmetics()
    {
        int count = Mathf.Min(
            cosmetics.Count,
            cosmeticStates.Count);

        for (int i = 0; i < count; i++)
        {
            if (cosmetics[i] != null)
            {
                cosmetics[i].SetActive(
                    cosmeticStates[i]);
            }
        }
    }

    private void ApplyColor(Color32 color)
    {
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

    [Command]
    private void CmdToggleCosmetic(int index)
    {
        if (index < 0 ||
            index >= cosmeticStates.Count)
        {
            Debug.LogError(
                $"Invalid cosmetic index: {index}");

            return;
        }

        cosmeticStates[index] =
            !cosmeticStates[index];
    }

    private void ToggleCosmetics(int index)
    {
        if (!isLocalPlayer)
            return;

        CmdToggleCosmetic(index);
    }

    [ContextMenu("Toggle clown")]
    public void ToggleCosmeticsEditor()
    {
        ToggleCosmetics(0);
    }
}
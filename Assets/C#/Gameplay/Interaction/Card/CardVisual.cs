using System.Collections;
using UnityEngine;

public class CardVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MeshRenderer cardMesh;
    [SerializeField] private MeshRenderer highlight;
    [SerializeField] private GameObject unavailableOverlay;
    [SerializeField] private GameObject descriptionMesh;
    [SerializeField] private Collider cardCollider;

    [Header("Description")]
    [SerializeField] private float descriptionDelay = 1f;

    private Coroutine descriptionRoutine;

    public void ApplyCardData(
        CardData data)
    {
        GameLog.Write(
            "CardVisual",
            $"ApplyCardData begin card={name} data={(data == null ? "NULL" : data.name)}");

        if (data == null ||
            cardMesh == null ||
            data.texture == null)
        {
            GameLog.Write(
                "CardVisual",
                $"ApplyCardData skipped data={(data == null ? "NULL" : data.name)} mesh={(cardMesh == null ? "NULL" : "ok")}");

            return;
        }

        Material[] materials =
            cardMesh.materials;

        if (materials == null ||
            materials.Length <= 1)
        {
            GameLog.Write(
                "CardVisual",
                $"ApplyCardData skipped: materials={(materials == null ? -1 : materials.Length)}");

            return;
        }

        GameLog.Write(
            "CardVisual",
            $"ApplyCardData texture={data.texture.name}");

        Material material =
            materials[1];

        material.SetTexture(
            "_BaseColorMap",
            data.texture);

        material.SetTexture(
            "_EmissiveColorMap",
            data.texture);
    }

    public void ApplyAvailability(
        bool available)
    {
        GameLog.Write(
            "CardVisual",
            $"ApplyAvailability card={name} available={available} overlay={(unavailableOverlay != null ? "ok" : "NULL")}");

        if (unavailableOverlay != null)
        {
            unavailableOverlay.SetActive(
                !available);
        }
    }

    public void HideUnavailableOverlay()
    {
        GameLog.Write(
            "CardVisual",
            $"HideUnavailableOverlay card={name} overlay={(unavailableOverlay != null ? "ok" : "NULL")}");

        if (unavailableOverlay != null)
        {
            unavailableOverlay.SetActive(false);
        }
    }

    public void SetHover(
        bool value)
    {
        GameLog.Write(
            "CardVisual",
            $"SetHover card={name} value={value} highlight={(highlight != null ? "ok" : "NULL")}");

        if (highlight != null)
        {
            highlight.enabled = value;
        }
    }

    public void StartDescriptionSwap()
    {
        GameLog.Write(
            "CardVisual",
            $"StartDescriptionSwap card={name} delay={descriptionDelay}");

        StopDescriptionSwap();

        if (cardMesh != null)
        {
            cardMesh.enabled = true;
        }

        if (descriptionMesh != null)
        {
            descriptionMesh.SetActive(false);
        }

        descriptionRoutine =
            StartCoroutine(
                SwapDescription());
    }

    private IEnumerator SwapDescription()
    {
        GameLog.Write(
            "CardVisual",
            $"SwapDescription start card={name} delay={descriptionDelay}");

        yield return new WaitForSeconds(
            descriptionDelay);

        if (cardMesh != null)
        {
            cardMesh.enabled = false;
        }

        if (descriptionMesh != null)
        {
            descriptionMesh.SetActive(true);
        }

        descriptionRoutine = null;

        GameLog.Write(
            "CardVisual",
            $"SwapDescription end card={name}: description shown, card mesh hidden");
    }

    public void StopDescriptionSwap()
    {
        if (descriptionRoutine == null)
        {
            GameLog.Write(
                "CardVisual",
                $"StopDescriptionSwap card={name}: no routine running");

            return;
        }

        GameLog.Write(
            "CardVisual",
            $"StopDescriptionSwap card={name}: routine stopped");

        StopCoroutine(
            descriptionRoutine);

        descriptionRoutine = null;
    }

    public void ShowCardMesh()
    {
        GameLog.Write(
            "CardVisual",
            $"ShowCardMesh card={name}");

        StopDescriptionSwap();

        if (descriptionMesh != null)
        {
            descriptionMesh.SetActive(false);
        }

        if (cardMesh != null)
        {
            cardMesh.enabled = true;
        }
    }

    public void DisableHighlight()
    {
        GameLog.Write(
            "CardVisual",
            $"DisableHighlight card={name} highlight={(highlight != null ? "ok" : "NULL")}");

        if (highlight != null)
        {
            highlight.enabled = false;
        }
    }

    public void SetColliderEnabled(
        bool value)
    {
        GameLog.Write(
            "CardVisual",
            $"SetColliderEnabled card={name} value={value} collider={(cardCollider != null ? "ok" : "NULL")}");

        if (cardCollider != null)
        {
            cardCollider.enabled = value;
        }
    }

    public void DisableInteraction()
    {
        GameLog.Write(
            "CardVisual",
            $"DisableInteraction card={name}");

        StopDescriptionSwap();
        ShowCardMesh();

        DisableHighlight();

        SetColliderEnabled(false);
    }

    public void ResetState()
    {
        GameLog.Write(
            "CardVisual",
            $"ResetState card={name}");

        StopDescriptionSwap();
        ShowCardMesh();
        DisableHighlight();
    }
}

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
        if (data == null ||
            cardMesh == null ||
            data.texture == null)
        {
            return;
        }

        Material[] materials =
            cardMesh.materials;

        if (materials == null ||
            materials.Length <= 1)
        {
            return;
        }

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
        if (unavailableOverlay != null)
        {
            unavailableOverlay.SetActive(
                !available);
        }

        SetColliderEnabled(true);
    }

    public void SetHover(
        bool value)
    {
        if (highlight != null)
            highlight.enabled = value;
    }

    public void StartDescriptionSwap()
    {
        StopDescriptionSwap();

        if (cardMesh != null)
            cardMesh.enabled = true;

        if (descriptionMesh != null)
            descriptionMesh.SetActive(false);

        descriptionRoutine =
            StartCoroutine(
                SwapDescription());
    }

    private IEnumerator SwapDescription()
    {
        yield return new WaitForSeconds(
            descriptionDelay);

        if (cardMesh != null)
            cardMesh.enabled = false;

        if (descriptionMesh != null)
            descriptionMesh.SetActive(true);

        descriptionRoutine = null;
    }

    public void StopDescriptionSwap()
    {
        if (descriptionRoutine == null)
            return;

        StopCoroutine(
            descriptionRoutine);

        descriptionRoutine = null;
    }

    public void ShowCardMesh()
    {
        StopDescriptionSwap();

        if (descriptionMesh != null)
            descriptionMesh.SetActive(false);

        if (cardMesh != null)
            cardMesh.enabled = true;
    }

    public void DisableHighlight()
    {
        if (highlight != null)
            highlight.enabled = false;
    }

    public void SetColliderEnabled(
        bool value)
    {
        if (cardCollider != null)
            cardCollider.enabled = value;
    }

    public void DisableInteraction()
    {
        StopDescriptionSwap();
        ShowCardMesh();

        DisableHighlight();

        if (unavailableOverlay != null)
            unavailableOverlay.SetActive(false);

        SetColliderEnabled(false);
    }

    public void ResetState()
    {
        StopDescriptionSwap();
        ShowCardMesh();
        DisableHighlight();

        if (unavailableOverlay != null)
            unavailableOverlay.SetActive(false);
    }
}
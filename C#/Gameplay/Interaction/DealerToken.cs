using Mirror;
using UnityEngine;

public class DealerToken : InteractableObject
{
    [SerializeField] private SkinnedMeshRenderer highlight;


    public override void Interact()
    {
        Debug.Log("Interacted with token");
        GameMaster.Instance.EndTurn();
    }

    public override void OnLookEnter()
    {
        highlight.enabled = true;
    }

    public override void OnLookExit()
    {
        highlight.enabled = false;
    }
}
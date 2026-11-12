using UnityEditor;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class BankInteractable : InteractableObject
{
    [SerializeField] GameObject hudObject;

    public override void Interact()
    {
        GameLog.Write(
            "BankInteractable",
            $"Interact netId={netId}: nothing to do");

        //Do nothing
    }

    public override void OnLookEnter()
    {
        GameLog.Write(
            "BankInteractable",
            $"OnLookEnter netId={netId}: showing hud={(hudObject != null ? hudObject.name : "NULL")}");

        hudObject.SetActive(true);
    }

    public override void OnLookExit()
    {
        GameLog.Write(
            "BankInteractable",
            $"OnLookExit netId={netId}: hiding hud={(hudObject != null ? hudObject.name : "NULL")}");

        hudObject.SetActive(false);
    }
}

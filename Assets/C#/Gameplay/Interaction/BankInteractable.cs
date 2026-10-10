using UnityEditor;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class BankInteractable : InteractableObject
{
    [SerializeField] GameObject hudObject;

    public override void Interact()
    {
        //Do nothing
    }

    public override void OnLookEnter()
    {
        hudObject.SetActive(true);
    }

    public override void OnLookExit()
    {
        hudObject.SetActive(false);
    }
}

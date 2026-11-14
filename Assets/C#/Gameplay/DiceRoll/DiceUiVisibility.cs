using System.Collections.Generic;
using UnityEngine;

// Appear and disappear effect of the UI dice (original: UIDiceVisibility).
public class DiceUiVisibility
{
    private readonly DiceRollSettings settings;
    private readonly IList<GameObject> uiDice;

    private float diceTimer;

    public DiceUiVisibility(
        DiceRollSettings settings,
        IList<GameObject> uiDice)
    {
        this.settings = settings;
        this.uiDice = uiDice;
    }

    public void Update(
        bool diceVisible)
    {
        if (diceVisible == true)
        {
            diceTimer += Time.fixedDeltaTime * settings.DiceAppearSpeed;
        }

        if (diceVisible == false)
        {
            diceTimer -= Time.fixedDeltaTime * settings.DiceDisappearSpeed;
        }

        diceTimer = Mathf.Clamp(diceTimer, 0f, 1f);

        foreach (GameObject d in uiDice)
        {
            d.GetComponent<MeshRenderer>().materials[0].SetFloat("_ScaleMult", diceTimer);
            d.GetComponent<ParticleSystemRenderer>().material.SetMatrix("_CubeMatrix", d.transform.worldToLocalMatrix);
            d.GetComponent<ParticleSystemRenderer>().material.SetFloat("_ScaleMult", diceTimer);
        }
    }
}

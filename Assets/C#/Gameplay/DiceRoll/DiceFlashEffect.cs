using System.Collections.Generic;
using UnityEngine;

// Flash of the UI dice one after another when everything has stopped (original: DoThingsWhenStopped).
public class DiceFlashEffect
{
    private readonly DiceRollSettings settings;
    private readonly IReadOnlyList<GameObject> dice;
    private readonly IList<GameObject> uiDice;
    private readonly IDiceStopSource stopDetector;
    private readonly float[] flashTimers = { 0f, 0f, 0f };

    private bool allStopped;

    public DiceFlashEffect(
        DiceRollSettings settings,
        IReadOnlyList<GameObject> dice,
        IList<GameObject> uiDice,
        IDiceStopSource stopDetector)
    {
        this.settings = settings;
        this.dice = dice;
        this.uiDice = uiDice;
        this.stopDetector = stopDetector;
    }

    public void Update()
    {
        if (allStopped != stopDetector.AllStopped() && stopDetector.AllStopped() == true)
        {
            int _count = 0;

            foreach (GameObject d in dice)
            {
                flashTimers[_count] = settings.FlashDuration * _count + settings.FlashDuration;
                _count += 1;
            }
        }

        int count = 0;

        foreach (GameObject d in dice)
        {
            if (stopDetector.AllStopped() == true)
            {
                flashTimers[count] = Mathf.Clamp(flashTimers[count] - Time.fixedDeltaTime, 0f, (settings.FlashDuration * count + settings.FlashDuration));
                uiDice[count].GetComponent<MeshRenderer>().materials[0].SetFloat("_BrightnessMult", Mathf.Lerp(0.3f, 1f, 1f - Mathf.Clamp(flashTimers[count] / settings.FlashDuration, 0f, 1f)));
                uiDice[count].GetComponent<ParticleSystemRenderer>().material.SetFloat("_BrightnessMult", (1f - Mathf.Clamp(flashTimers[count] / settings.FlashDuration, 0f, 1f)) * (1f - settings.FollowWeight));
                uiDice[count].GetComponent<MeshRenderer>().materials[0].SetFloat("_ScalePulse", settings.ScalePulse * (1f - Mathf.Clamp(flashTimers[count] / settings.FlashDuration, 0f, 1f)));
                uiDice[count].GetComponent<ParticleSystemRenderer>().material.SetFloat("_ScalePulse", settings.ScalePulse * (1f - Mathf.Clamp(flashTimers[count] / settings.FlashDuration, 0f, 1f)));
            }
            else
            {
                uiDice[count].GetComponent<MeshRenderer>().materials[0].SetFloat("_BrightnessMult", 0.3f);
                uiDice[count].GetComponent<ParticleSystemRenderer>().material.SetFloat("_BrightnessMult", 0f);
                uiDice[count].GetComponent<MeshRenderer>().materials[0].SetFloat("_ScalePulse", 0f);
                uiDice[count].GetComponent<ParticleSystemRenderer>().material.SetFloat("_ScalePulse", 0f);
            }

            count += 1;
        }

        allStopped = stopDetector.AllStopped();
    }
}

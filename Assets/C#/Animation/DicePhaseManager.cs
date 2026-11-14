#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Phase manager of the key-3 dice throw: only the order of the phases lives here.
public class DicePhaseManager : MonoBehaviour
{
    private const float ReadyTime = 1.4f;
    private const float ShakeEnd = 2.2f;
    private const float ThrowStart = 2.3f;
    private const float ReleaseAt = 2.5f;
    private const float FullPowerTime = 2f;
    private const float ThrowPower = 0.95f;
    private const float ResetDelay = 4f;
    private const float StopWaitLimit = 4f;
    private const float LiftStateWaitLimit = 1f;
    private const bool ShowAimLine = false;

    private LiftAnimation lift;
    private DiceResultDisplay resultDisplay;
    private DiceChargeBar chargeBar;
    private PlateSpawner plate;
    private DiceRollRig dice;
    private CardHandVisibility cards;
    private DiceAimLine aimLine;
    private HandsRollAnimation hands;
    private bool busy;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        GameObject go =
            new GameObject("DicePhaseManager");

        DontDestroyOnLoad(go);

        go.AddComponent<LiftAnimation>();
        go.AddComponent<DiceResultDisplay>();
        go.AddComponent<DiceChargeBar>();
        go.AddComponent<DicePhaseManager>();
    }

    private void Awake()
    {
        lift =
            GetComponent<LiftAnimation>();

        resultDisplay =
            GetComponent<DiceResultDisplay>();

        chargeBar =
            GetComponent<DiceChargeBar>();

        plate =
            new PlateSpawner(
                lift);

        dice =
            new DiceRollRig();

        cards =
            new CardHandVisibility();

        aimLine =
            new DiceAimLine();
    }

    private void Update()
    {
        Keyboard keyboard =
            Keyboard.current;

        if (keyboard == null ||
            !keyboard.digit3Key.wasPressedThisFrame)
        {
            return;
        }

        if (busy)
        {
            CancelRun();

            return;
        }

        StartCoroutine(
            Run());
    }

    private void CancelRun()
    {
        GameLog.Write(
            "DicePhaseManager",
            "3 pressed again: everything is disposed");

        StopAllCoroutines();

        aimLine.Hide();
        chargeBar.Hide();
        resultDisplay.Hide();
        dice.Dispose();
        cards.Show();

        ResetAll();

        busy = false;
    }

    private IEnumerator Run()
    {
        busy = true;

        resultDisplay.Hide();
        dice.Dispose();

        hands =
            HandsRollAnimation.Find();

        if (hands == null)
        {
            busy = false;

            yield break;
        }

        GameLog.Write(
            "DicePhaseManager",
            "3 step 1: pedestal and plate up");

        DiceCollisionLayers.Apply();

        cards.Hide();

        lift.Raise();

        float waited = 0f;

        while (!lift.IsInState("DiceAppear") &&
               waited < LiftStateWaitLimit)
        {
            waited += Time.deltaTime;

            yield return null;
        }

        plate.Spawn();

        yield return new WaitForSeconds(
            lift.RiseDuration);

        GameLog.Write(
            "DicePhaseManager",
            "3 step 2: hands take the dice (dice appear only at the throw)");

        hands.SetWeight(
            1f);

        float time = 0f;

        while (time < ReadyTime)
        {
            time += Time.deltaTime;

            hands.Scrub(
                time);

            yield return null;
        }

        GameLog.Write(
            "DicePhaseManager",
            "Ready: hold the left mouse button to shake");

        while (!Mouse.current.leftButton.isPressed)
        {
            hands.Scrub(
                ReadyTime);

            yield return null;
        }

        GameLog.Write(
            "DicePhaseManager",
            "LMB held: shaking in a loop");

        float phase = 0f;

        chargeBar.Show();

        while (Mouse.current.leftButton.isPressed)
        {
            phase += Time.deltaTime;

            time =
                ReadyTime +
                Mathf.PingPong(
                    phase,
                    ShakeEnd - ReadyTime);

            hands.Scrub(
                time);

            float charge =
                Mathf.Clamp01(
                    phase / FullPowerTime);

            chargeBar.SetValue(
                charge);

            if (ShowAimLine)
            {
                aimLine.Draw(
                    hands.Wrist,
                    DiceRollRig.PlateCenter,
                    charge);
            }

            yield return null;
        }

        aimLine.Hide();
        chargeBar.Hide();

        GameLog.Write(
            "DicePhaseManager",
            "LMB released: throw, dice appear in the hand");

        dice.Build(
            RandomScore());

        dice.State.IsCharging = true;
        dice.State.IsAttackOrReady = false;

        yield return null;

        time = ThrowStart;

        while (time < ReleaseAt)
        {
            time += Time.deltaTime;

            hands.Scrub(
                time);

            dice.HoldBones(
                hands.Wrist);

            yield return null;
        }

        float power =
            Mathf.Clamp01(
                phase / FullPowerTime);

        GameLog.Write(
            "DicePhaseManager",
            $"Throw power={power:0.00} (LMB held {phase:0.00}s, full power at {FullPowerTime}s)");

        StartCoroutine(
            dice.FlyBones(
                DiceRollRig.PlateCenter,
                ThrowPower));

        StartCoroutine(
            ShowResultWhenStopped());

        while (time < hands.Length)
        {
            time += Time.deltaTime;

            hands.Scrub(
                time);

            yield return null;
        }

        hands.Scrub(
            hands.Length - 0.01f);

        yield return new WaitForSeconds(
            ResetDelay);

        // The lift, the plate, the dice and the result stay until 3 is pressed again.
        hands.SetWeight(
            0f);
    }

    private IEnumerator ShowResultWhenStopped()
    {
        yield return new WaitForSeconds(
            DiceThrower.FlightTime);

        float waited = 0f;

        while (waited < StopWaitLimit &&
               !dice.AllStopped())
        {
            waited += Time.deltaTime;

            yield return null;
        }

        resultDisplay.Show(
            dice.Controller.Score);
    }

    private static int[] RandomScore()
    {
        return new[]
        {
            Random.Range(1, 7),
            Random.Range(1, 7),
            Random.Range(1, 7)
        };
    }

    private void ResetAll()
    {
        GameLog.Write(
            "DicePhaseManager",
            "Reset: hands, dice, plate and pedestal back");

        if (hands != null)
        {
            hands.SetWeight(
                0f);
        }

        plate.Remove();

        lift.Lower();
    }
}
#endif

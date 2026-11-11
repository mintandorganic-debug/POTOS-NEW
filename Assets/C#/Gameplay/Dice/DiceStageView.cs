using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class DiceStageView : MonoBehaviour
{
    private const float ChargeSpeed = 0.5f;
    private const float ResultShowTime = 3f;

    private bool active;
    private bool isThrower;
    private bool holding;
    private bool sent;
    private float power;
    private float endTime;
    private float resultUntil;
    private string resultText = string.Empty;
    private GUIStyle titleStyle;
    private GUIStyle textStyle;

    private void OnEnable()
    {
        GameMaster.DiceStarted += OnDiceStarted;
        GameMaster.DiceRolled += OnDiceRolled;
    }

    private void OnDisable()
    {
        GameMaster.DiceStarted -= OnDiceStarted;
        GameMaster.DiceRolled -= OnDiceRolled;
    }

    private void OnDiceStarted(
        uint playerNetId,
        float duration)
    {
        active = true;
        sent = false;
        holding = false;
        power = 0f;
        endTime = Time.time + duration;

        isThrower =
            NetworkClient.localPlayer != null &&
            NetworkClient.localPlayer.netId == playerNetId;

        GameLog.Write(
            "DiceStageView",
            $"OnDiceStarted player={playerNetId} duration={duration} isThrower={isThrower}");
    }

    private void OnDiceRolled(
        uint playerNetId,
        int first,
        int second,
        int third,
        string combination,
        int chips,
        int coins,
        float rolledPower)
    {
        resultText =
            $"DICE  {first}  {second}  {third}   SUM {first + second + third}   +{chips} chips";

        if (coins > 0)
        {
            resultText +=
                $"\n{combination}  +{coins} coins";
        }

        resultUntil =
            Time.time + ResultShowTime;

        active = false;
        holding = false;

        GameLog.Write(
            "DiceStageView",
            $"OnDiceRolled player={playerNetId} values={first},{second},{third} combination={combination} chips={chips} coins={coins} power={rolledPower}");
    }

    private void Update()
    {
        if (!active ||
            !isThrower ||
            sent)
        {
            return;
        }

        Mouse mouse =
            Mouse.current;

        if (mouse == null)
        {
            return;
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            holding = true;
            power = 0f;
        }

        if (holding &&
            mouse.leftButton.isPressed)
        {
            power =
                Mathf.Min(
                    1f,
                    power + ChargeSpeed * Time.deltaTime);
        }

        if (holding &&
            mouse.leftButton.wasReleasedThisFrame)
        {
            holding = false;
            sent = true;

            GameLog.Write(
                "DiceStageView",
                $"Update throw released power={power}");

            if (GameMaster.Instance != null)
            {
                GameMaster.Instance.CmdThrowDice(
                    power);
            }
        }
    }

    private void OnGUI()
    {
        bool showResult =
            Time.time < resultUntil;

        if (!active &&
            !showResult)
        {
            return;
        }

        if (titleStyle == null)
        {
            titleStyle =
                new GUIStyle(GUI.skin.label)
                {
                    fontSize = 28,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };

            textStyle =
                new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    alignment = TextAnchor.MiddleCenter
                };
        }

        float width = Screen.width;
        float centerY = Screen.height * 0.25f;

        if (active)
        {
            float remaining =
                Mathf.Max(
                    0f,
                    endTime - Time.time);

            GUI.Label(
                new Rect(0f, centerY, width, 40f),
                $"STAGE 2: DICE  {remaining:0.0}",
                titleStyle);

            if (isThrower &&
                !sent)
            {
                GUI.Label(
                    new Rect(0f, centerY + 40f, width, 30f),
                    "Hold LMB to set power, release to throw",
                    textStyle);

                Rect bar =
                    new Rect(
                        width * 0.5f - 150f,
                        centerY + 80f,
                        300f,
                        18f);

                GUI.Box(
                    bar,
                    GUIContent.none);

                Color previous =
                    GUI.color;

                GUI.color =
                    Color.Lerp(
                        Color.green,
                        Color.red,
                        power);

                GUI.DrawTexture(
                    new Rect(
                        bar.x,
                        bar.y,
                        bar.width * power,
                        bar.height),
                    Texture2D.whiteTexture);

                GUI.color =
                    previous;
            }
        }

        if (showResult)
        {
            GUI.Label(
                new Rect(0f, centerY + 120f, width, 80f),
                resultText,
                textStyle);
        }
    }
}

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;

public class DiceDebugKeys : MonoBehaviour
{
    private void Update()
    {
        Keyboard keyboard =
            Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard.f9Key.wasPressedThisFrame)
        {
            GameLog.Write(
                "DiceDebugKeys",
                "F9: local dice start (lift up, hands to DicePrep)");

            DiceLift lift =
                GetComponent<DiceLift>();

            if (lift != null)
            {
                lift.DebugRaise();
            }

            DiceHands handsView =
                GetComponent<DiceHands>();

            if (handsView != null)
            {
                handsView.BeginLocal();
            }
        }

        if (keyboard.f10Key.wasPressedThisFrame)
        {
            GameLog.Write(
                "DiceDebugKeys",
                "F10: local dice finish (hands idle, lift down)");

            DiceHands handsView =
                GetComponent<DiceHands>();

            if (handsView != null)
            {
                handsView.FinishLocal();
            }

            DiceLift lift =
                GetComponent<DiceLift>();

            if (lift != null)
            {
                lift.DebugLower();
            }
        }
    }
}
#endif

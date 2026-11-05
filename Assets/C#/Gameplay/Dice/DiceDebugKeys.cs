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

        DiceLift lift =
            GetComponent<DiceLift>();

        DiceHands handsView =
            GetComponent<DiceHands>();

        if (keyboard.f7Key.wasPressedThisFrame)
        {
            GameLog.Write(
                "DiceDebugKeys",
                "F7: lift and plate up");

            if (lift != null)
            {
                lift.DebugRaise();
            }
        }

        if (keyboard.f8Key.wasPressedThisFrame)
        {
            GameLog.Write(
                "DiceDebugKeys",
                "F8: hands take dice and shake (charge)");

            if (handsView != null)
            {
                handsView.DebugCharge();
            }
        }

        if (keyboard.f9Key.wasPressedThisFrame)
        {
            GameLog.Write(
                "DiceDebugKeys",
                "F9: throw");

            if (handsView != null)
            {
                handsView.DebugThrow();
            }
        }

        if (keyboard.f10Key.wasPressedThisFrame)
        {
            GameLog.Write(
                "DiceDebugKeys",
                "F10: reset hands and lift");

            if (handsView != null)
            {
                handsView.FinishLocal();
            }

            if (lift != null)
            {
                lift.DebugLower();
            }
        }
    }
}
#endif

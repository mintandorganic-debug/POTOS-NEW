using System.Collections.Generic;
using UnityEngine;

// Rotates the UI copies of the dice after the physical ones (original: RotateUIDice and TwoAxisCompoundRotation).
public class DiceUiRotator
{
    private readonly DiceRollSettings settings;
    private readonly IReadOnlyList<GameObject> dice;
    private readonly IList<GameObject> uiDice;
    private readonly Transform mainCamera;
    private readonly Quaternion[] rotFrames = { Quaternion.identity, Quaternion.identity, Quaternion.identity };
    private readonly float[] rotFrameTimers = { 0.1f, 0.1f, 0.1f };

    public DiceUiRotator(
        DiceRollSettings settings,
        IReadOnlyList<GameObject> dice,
        IList<GameObject> uiDice,
        Transform mainCamera)
    {
        this.settings = settings;
        this.dice = dice;
        this.uiDice = uiDice;
        this.mainCamera = mainCamera;
    }

    public void Rotate()
    {
        if (dice.Count != 0 && uiDice.Count != 0)
        {
            int count = 0;
            Vector4 CamRight = new Vector4(mainCamera.right.x, mainCamera.right.y, mainCamera.right.z, 0f);
            Vector4 CamUp = new Vector4(mainCamera.forward.x, mainCamera.forward.y, mainCamera.forward.z, 0f) * -1f;
            Vector4 CamForward = new Vector4(mainCamera.up.x, mainCamera.up.y, mainCamera.up.z, 0f);
            Matrix4x4 CameraAlign = new Matrix4x4();
            CameraAlign.SetColumn(0, CamRight);
            CameraAlign.SetColumn(1, CamUp);
            CameraAlign.SetColumn(2, CamForward);
            CameraAlign.SetColumn(3, new Vector4(0f, 0f, 0f, 1f));

            foreach (GameObject d in dice)
            {
                bool SetKeyframe = false;
                rotFrameTimers[count] -= Time.fixedDeltaTime;

                if (rotFrameTimers[count] <= 0f)
                {
                    SetKeyframe = true;
                    rotFrameTimers[count] = settings.RotFrameRate;
                }

                // Rotation of the empty child, not of the main node (new score tweaking algorithm).
                Vector3 _x = d.transform.GetChild(0).right;
                Vector3 _y = d.transform.GetChild(0).up;
                Vector3 _z = d.transform.GetChild(0).forward;
                float axis = 0f;

                // Which axis is the most parallel to the world Y axis, and its direction.
                if (Mathf.Abs(_x.y) > Mathf.Abs(_y.y))
                {
                    if (Mathf.Abs(_x.y) > Mathf.Abs(_z.y))
                    {
                        axis = 1f * Mathf.Sign(_x.y);
                    }
                    else
                    {
                        axis = 3f * Mathf.Sign(_z.y);
                    }
                }
                else
                {
                    if (Mathf.Abs(_y.y) > Mathf.Abs(_z.y))
                    {
                        axis = 2f * Mathf.Sign(_y.y);
                    }
                    else
                    {
                        axis = 3f * Mathf.Sign(_z.y);
                    }
                }

                Vector3 ChooseSide = Vector3.zero;

                switch (axis)
                {
                    case -3f:
                        ChooseSide = new Vector3(90f, 0f, 0f);
                        break;
                    case -2f:
                        ChooseSide = new Vector3(180f, 0f, 0f);
                        break;
                    case -1f:
                        ChooseSide = new Vector3(0f, 0f, -90f);
                        break;
                    case 1f:
                        ChooseSide = new Vector3(0f, 90f, 90f);
                        break;
                    case 2f:
                        ChooseSide = new Vector3(0f, -90f, 0f);
                        break;
                    case 3f:
                        ChooseSide = new Vector3(-90f, 0f, 0f);
                        break;
                }

                Quaternion SnapRot = Quaternion.Euler(ChooseSide);

                if (SetKeyframe == true)
                {
                    rotFrames[count] = SnapRot;
                }

                Quaternion _SnapRot = CameraAlign.rotation * rotFrames[count];
                Debug.DrawRay(uiDice[count].transform.position, _SnapRot * new Vector3(0f, 0f, 1f), Color.red);
                Debug.DrawRay(uiDice[count].transform.position, uiDice[count].transform.rotation * new Vector3(0f, 0f, 1f), Color.green);
                Debug.DrawRay(uiDice[count].transform.position, _SnapRot * new Vector3(0f, 1f, 0f), Color.blue);
                Debug.DrawRay(uiDice[count].transform.position, uiDice[count].transform.rotation * new Vector3(0f, 1f, 0f), Color.blue);
                uiDice[count].transform.rotation = TwoAxisCompoundRotation(uiDice[count].transform.rotation, _SnapRot, 1f - rotFrameTimers[count] / settings.RotFrameRate);
                SetKeyframe = false;
                count += 1;
            }
        }
    }

    private Quaternion TwoAxisCompoundRotation(
        Quaternion _from,
        Quaternion rot,
        float t)
    {
        Vector3 TargetForward = rot * new Vector3(0f, 0f, 1f);
        Vector3 TargetUp = rot * new Vector3(0f, 1f, 0f);

        Quaternion halfway = Quaternion.FromToRotation(_from * new Vector3(0f, 0f, 1f), TargetForward);

        Vector3 halfUp = Quaternion.Slerp(_from, halfway * _from, Mathf.Clamp(t / 0.5f, 0f, 1f)) * new Vector3(0f, 1f, 0f);
        float targetAngle = Vector3.Angle(halfUp, TargetUp);
        targetAngle *= Mathf.Sign(Vector3.Dot(halfUp, TargetUp));
        Quaternion AxisRot = Quaternion.AngleAxis(targetAngle, TargetForward);
        Quaternion final = halfway * AxisRot;

        return final * _from;
    }
}

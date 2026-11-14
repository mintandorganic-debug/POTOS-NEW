#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Builds the objects the artist's dice logic needs, moves the control bones and removes everything again.
public class DiceRollRig
{
    public static readonly Vector3 PlateCenter =
        new Vector3(-0.013f, 0.15f, -0.001f);

    private const float FloorSize = 0.7f;
    private const float FollowReleaseTime = 0.3f;
    private const float DiceSpacing = 0.03f;
    private const float StopSpeed = 0.05f;

    private GameObject root;
    private GameObject canvasObject;
    private GameObject floor;

    public DiceRollController Controller { get; private set; }

    public Transform[] Bones { get; private set; }

    public ScriptedDiceState State { get; } = new ScriptedDiceState();

    public bool IsBuilt =>
        root != null;

    public void Build(
        int[] score)
    {
        Dispose();

        DiceCollisionLayers.Apply();

        GameObject diePrefab =
            Resources.Load<GameObject>(
                "Dice/SingleDie");

        GameObject uiBase =
            Resources.Load<GameObject>(
                "Dice/DiceUIGlow");

        if (uiBase == null)
        {
            GameLog.Write(
                "DiceRollRig",
                "Dice/DiceUIGlow prefab is missing: run Potos/Extract Dice UI Glow Prefab");

            uiBase =
                Resources.Load<GameObject>(
                    "Dice/DiceUIBase");
        }

        root =
            new GameObject("DiceRollRig");

        root.AddComponent<Animator>();

        Transform plate =
            new GameObject("Plate").transform;

        plate.SetParent(
            root.transform,
            false);

        plate.position = PlateCenter;

        Transform origin =
            new GameObject("DiceOrigin").transform;

        origin.SetParent(
            root.transform,
            false);

        origin.position = PlateCenter;

        Bones = new Transform[3];

        List<GameObject> bones =
            new List<GameObject>();

        for (int i = 0;
             i < 3;
             i++)
        {
            GameObject bone =
                new GameObject("bone" + (i + 1));

            bone.transform.SetParent(
                root.transform,
                false);

            Bones[i] = bone.transform;
            bones.Add(bone);
        }

        BuildFloor();

        List<GameObject> uiDice =
            BuildUiDice(
                uiBase);

        DiceRollController controller =
            root.AddComponent<DiceRollController>();

        controller.DiePrefab = diePrefab;
        controller.MainCamera = Camera.main.transform;
        controller.DiceControlBones = bones;
        controller.UIDice = uiDice;
        controller.Plate = plate;
        controller.DiceOrigin = origin;
        controller.DiceVisible = true;

        controller.Settings.score = score;
        controller.Settings.DieRadius = 0.04f;
        controller.Settings.FollowWeight = 1f;
        controller.Settings.PlateRadius = 0.336f;
        controller.Settings.ChargeMin = 1f;
        controller.Settings.ChargeMax = 1.5f;
        controller.Settings.ForceVariation = 0.1f;
        controller.Settings.VerticalLimit = 0.5f;
        controller.Settings.DiceAppearSpeed = 2f;
        controller.Settings.DiceDisappearSpeed = 2f;

        controller.Configure(
            new FixedDiceCharge(
                0.5f),
            State);

        Controller = controller;
    }

    public static Vector3 HoldPosition(
        Transform wrist,
        int index)
    {
        Transform cam =
            Camera.main != null
                ? Camera.main.transform
                : wrist;

        return
            wrist.position +
            Vector3.up * 0.03f +
            cam.right * (index - 1) * DiceSpacing +
            cam.forward * 0.04f;
    }

    public void HoldBones(
        Transform wrist)
    {
        for (int i = 0;
             i < Bones.Length;
             i++)
        {
            Bones[i].position =
                HoldPosition(
                    wrist,
                    i);
        }
    }

    // The bones fly along the parabola, the dice follow them and are released to free physics.
    public IEnumerator FlyBones(
        Vector3 landing,
        float power)
    {
        Vector3[] origins =
            new Vector3[Bones.Length];

        Vector3[] velocities =
            new Vector3[Bones.Length];

        for (int i = 0;
             i < Bones.Length;
             i++)
        {
            origins[i] = Bones[i].position;

            velocities[i] =
                DiceThrower.LaunchVelocity(
                    origins[i],
                    landing,
                    power,
                    i - 1,
                    out Vector3 spot);
        }

        State.IsCharging = false;

        float time = 0f;

        while (time < DiceThrower.FlightTime)
        {
            time += Time.deltaTime;

            for (int i = 0;
                 i < Bones.Length;
                 i++)
            {
                Bones[i].position =
                    DiceThrower.PointAt(
                        origins[i],
                        velocities[i],
                        time);
            }

            if (time >= FollowReleaseTime)
            {
                Controller.Settings.FollowWeight = 0f;
            }

            yield return null;
        }

        State.IsAttackOrReady = true;
    }

    public bool AllStopped()
    {
        if (Controller == null)
        {
            return true;
        }

        foreach (GameObject die in Controller.PhysicalDice)
        {
            if (die != null &&
                die.GetComponent<Rigidbody>().linearVelocity.magnitude > StopSpeed)
            {
                return false;
            }
        }

        return true;
    }

    public void Dispose()
    {
        if (root != null)
        {
            Object.Destroy(
                root);
        }

        if (canvasObject != null)
        {
            Object.Destroy(
                canvasObject);
        }

        if (floor != null)
        {
            Object.Destroy(
                floor);
        }

        root = null;
        canvasObject = null;
        floor = null;
        Controller = null;

        foreach (GameObject die in
                 GameObject.FindObjectsByType<Rigidbody>(
                         FindObjectsSortMode.None)
                     .Where(b => b.name.StartsWith("SingleDie"))
                     .Select(b => b.gameObject)
                     .ToList())
        {
            Object.Destroy(
                die);
        }
    }

    private void BuildFloor()
    {
        floor =
            new GameObject("DiceTestFloor");

        floor.layer = DiceCollisionLayers.LayerId;

        floor.transform.position =
            PlateCenter + Vector3.down * 0.01f;

        floor.AddComponent<BoxCollider>().size =
            new Vector3(FloorSize, 0.02f, FloorSize);
    }

    private List<GameObject> BuildUiDice(
        GameObject uiBase)
    {
        canvasObject =
            new GameObject("DiceTestCanvas");

        Canvas canvas =
            canvasObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 0.5f;

        GameObject uiRoot =
            Object.Instantiate(
                uiBase,
                canvasObject.transform,
                false);

        List<GameObject> uiDice =
            uiRoot.GetComponentsInChildren<MeshRenderer>(true)
                .Select(r => r.gameObject)
                .OrderBy(g => g.transform.localPosition.x)
                .ToList();

        return uiDice;
    }
}
#endif

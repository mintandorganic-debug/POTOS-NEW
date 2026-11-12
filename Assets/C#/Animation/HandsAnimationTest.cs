#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class HandsAnimationTest : MonoBehaviour
{
    private const string HandsControllerName = "Player_AC";
    private const string DiceLayer = "DiceHands";
    private const string RollState = "GameDiceRoll";
    private const string WristName = "Wrist.R";
    private const float ReadyTime = 1.4f;
    private const float ShakeEnd = 2.2f;
    private const float ThrowStart = 2.3f;
    private const float FullPowerTime = 2f;
    private const float ThrowPower = 0.95f;
    private const float NearDistance = 0.35f;
    private const float LateralSpread = 0.02f;
    private const int LineSegments = 24;
    private const bool ShowAimLine = false;
    private const float DiceScale = 1f / 3f;
    private const float DiceSpacing = 0.03f;
    private const float ReleaseAt = 2.5f;
    private const float FlightTime = 0.7f;
    private const float ResetDelay = 4f;

    private static readonly Vector3 PlateTarget =
        new Vector3(-0.013f, 0.15f, -0.001f);

    private DiceLiftTest lift;
    private GameObject plateVisual;
    private LineRenderer aimLine;
    private DiceResultDisplay resultDisplay;
    private DiceChargeBar chargeBar;
    private readonly List<Rigidbody> dice = new List<Rigidbody>();
    private bool busy;
    private Animator activeHands;
    private int activeLayer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        GameObject go =
            new GameObject("HandsAnimationTest");

        DontDestroyOnLoad(go);

        go.AddComponent<DiceLiftTest>();
        go.AddComponent<DiceResultDisplay>();
        go.AddComponent<DiceChargeBar>();
        go.AddComponent<HandsAnimationTest>();
    }

    private void Awake()
    {
        lift =
            GetComponent<DiceLiftTest>();

        resultDisplay =
            GetComponent<DiceResultDisplay>();

        chargeBar =
            GetComponent<DiceChargeBar>();
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
            "HandsAnimationTest",
            "3 pressed again: everything is disposed");

        StopAllCoroutines();

        HideAimLine();
        chargeBar.Hide();
        resultDisplay.Hide();

        if (activeHands != null)
        {
            ResetAll(
                activeHands,
                activeLayer);
        }

        busy = false;
    }

    private IEnumerator Run()
    {
        busy = true;

        resultDisplay.Hide();

        Animator hands =
            FindHands();

        int layer =
            hands != null
                ? hands.GetLayerIndex(
                    DiceLayer)
                : -1;

        float length =
            hands != null
                ? ClipLength(
                    hands)
                : 0f;

        Transform wrist =
            hands != null
                ? FindDeep(
                    hands.transform,
                    WristName)
                : null;

        if (hands == null ||
            layer < 0 ||
            length <= 0f ||
            wrist == null)
        {
            GameLog.Write(
                "HandsAnimationTest",
                $"3: cannot start hands={(hands != null)} layer={layer} clipLength={length} wrist={(wrist != null)}");

            busy = false;

            yield break;
        }

        activeHands = hands;
        activeLayer = layer;

        GameLog.Write(
            "HandsAnimationTest",
            "3 step 1: pedestal and plate up");

        lift.Raise();

        SpawnPlateVisual();

        yield return new WaitForSeconds(
            lift.RiseDuration);

        Transform plate =
            lift.Plate;

        SetupDiceCollisions();

        GameLog.Write(
            "HandsAnimationTest",
            "3 step 2: hands take the dice (dice appear only at the throw)");

        hands.SetLayerWeight(
            layer,
            1f);

        float time = 0f;

        while (time < ReadyTime)
        {
            time += Time.deltaTime;

            Scrub(hands, layer, time, length);

            yield return null;
        }

        GameLog.Write(
            "HandsAnimationTest",
            "Ready: hold the left mouse button to shake");

        while (!Mouse.current.leftButton.isPressed)
        {
            Scrub(hands, layer, ReadyTime, length);

            yield return null;
        }

        GameLog.Write(
            "HandsAnimationTest",
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

            Scrub(hands, layer, time, length);

            chargeBar.SetValue(
                Mathf.Clamp01(
                    phase / FullPowerTime));

            if (ShowAimLine)
            {
                DrawAimLine(
                    wrist,
                    Mathf.Clamp01(
                        phase / FullPowerTime));
            }

            yield return null;
        }

        HideAimLine();
        chargeBar.Hide();

        GameLog.Write(
            "HandsAnimationTest",
            "LMB released: throw, dice appear in the hand");

        SpawnDice(
            hands);

        time = ThrowStart;

        while (time < ReleaseAt)
        {
            time += Time.deltaTime;

            Scrub(hands, layer, time, length);
            HoldDice(wrist);

            yield return null;
        }

        float power =
            Mathf.Clamp01(
                phase / FullPowerTime);

        GameLog.Write(
            "HandsAnimationTest",
            $"Throw power={power:0.00} (LMB held {phase:0.00}s, full power at {FullPowerTime}s)");

        ReleaseDice(
            plate,
            ThrowPower);

        StartCoroutine(
            LogFlight(
                plate));

        StartCoroutine(
            ShowResultWhenStopped(
                power));

        while (time < length)
        {
            time += Time.deltaTime;

            Scrub(hands, layer, time, length);

            yield return null;
        }

        Scrub(hands, layer, length - 0.01f, length);

        yield return new WaitForSeconds(
            ResetDelay);

        ResetAll(
            hands,
            layer);

        busy = false;
    }

    private static void Scrub(
        Animator hands,
        int layer,
        float time,
        float length)
    {
        hands.Play(
            RollState,
            layer,
            Mathf.Clamp01(time / length));
    }

    private void SpawnDice(
        Animator hands)
    {
        DestroyDice();

        GameObject diePrefab =
            Resources.Load<GameObject>(
                "Dice/SingleDie");

        if (diePrefab == null)
        {
            GameLog.Write(
                "HandsAnimationTest",
                "SpawnDice failed: SingleDie prefab not found");

            return;
        }

        int layerId =
            LayerMask.NameToLayer(
                "DiceSimulation");

        for (int i = 0;
             i < 3;
             i++)
        {
            GameObject die =
                Instantiate(
                    diePrefab);

            if (layerId >= 0)
            {
                die.layer = layerId;
            }

            die.transform.localScale *= DiceScale;

            Rigidbody body =
                die.GetComponent<Rigidbody>();

            body.isKinematic = true;

            SetDiceColliders(
                body,
                false);

            dice.Add(
                body);
        }
    }

    private void HoldDice(
        Transform wrist)
    {
        Transform cam =
            Camera.main != null
                ? Camera.main.transform
                : wrist;

        for (int i = 0;
             i < dice.Count;
             i++)
        {
            Vector3 position =
                wrist.position +
                Vector3.up * 0.03f +
                cam.right * (i - 1) * DiceSpacing +
                cam.forward * 0.04f;

            dice[i].position = position;
            dice[i].transform.position = position;
        }
    }

    private Vector3 PlateLanding
    {
        get
        {
            if (plateVisual == null)
            {
                return PlateTarget;
            }

            Renderer[] renderers =
                plateVisual.GetComponentsInChildren<Renderer>(true);

            if (renderers.Length == 0)
            {
                return PlateTarget;
            }

            Bounds visual =
                renderers[0].bounds;

            foreach (Renderer renderer in renderers)
            {
                visual.Encapsulate(
                    renderer.bounds);
            }

            return
                new Vector3(
                    visual.center.x,
                    visual.min.y,
                    visual.center.z);
        }
    }

    private Vector3 LaunchVelocity(
        Vector3 origin,
        float power,
        float side,
        out Vector3 spot)
    {
        Vector3 center =
            PlateLanding;

        Vector3 toCenter =
            center - origin;

        toCenter.y = 0f;

        Vector3 flightDirection =
            toCenter.sqrMagnitude > 0.0001f
                ? toCenter.normalized
                : Vector3.forward;

        Vector3 near =
            origin +
            flightDirection * NearDistance;

        near.y = center.y;

        Vector3 target =
            Vector3.Lerp(
                near,
                center,
                power);

        Vector3 sideways =
            Vector3.Cross(
                Vector3.up,
                flightDirection).normalized;

        spot =
            target +
            Vector3.up * 0.05f +
            sideways * side * LateralSpread;

        return
            (spot - origin -
             0.5f * Physics.gravity * FlightTime * FlightTime) /
            FlightTime;
    }

    private void ReleaseDice(
        Transform plate,
        float power)
    {
        Vector3 origin =
            dice.Count > 0
                ? dice[0].position
                : PlateTarget;

        for (int i = 0;
             i < dice.Count;
             i++)
        {
            Rigidbody body =
                dice[i];

            Vector3 velocity =
                LaunchVelocity(
                    body.position,
                    power,
                    i - 1,
                    out Vector3 spot);

            SetDiceColliders(
                body,
                true);

            body.isKinematic = false;
            body.linearVelocity = velocity;
            body.angularVelocity = Vector3.zero;

            GameLog.Write(
                "HandsAnimationTest",
                $"Release die from={body.position.ToString("F2")} to={spot.ToString("F2")} velocity={velocity.ToString("F2")}");
        }
    }

    private void DrawAimLine(
        Transform wrist,
        float power)
    {
        if (aimLine == null)
        {
            GameObject lineObject =
                new GameObject("DiceAimLine");

            aimLine =
                lineObject.AddComponent<LineRenderer>();

            Shader shader =
                Shader.Find("Sprites/Default");

            if (shader == null)
            {
                shader =
                    Shader.Find("Hidden/Internal-Colored");
            }

            aimLine.material =
                new Material(
                    shader);

            aimLine.startColor = Color.red;
            aimLine.endColor = Color.yellow;
            aimLine.startWidth = 0.01f;
            aimLine.endWidth = 0.01f;
            aimLine.positionCount = LineSegments + 1;
        }

        Transform cam =
            Camera.main != null
                ? Camera.main.transform
                : wrist;

        Vector3 origin =
            wrist.position +
            Vector3.up * 0.03f +
            cam.forward * 0.04f;

        Vector3 velocity =
            LaunchVelocity(
                origin,
                power,
                0f,
                out Vector3 spot);

        for (int i = 0;
             i <= LineSegments;
             i++)
        {
            float t =
                FlightTime * i / LineSegments;

            aimLine.SetPosition(
                i,
                origin +
                velocity * t +
                0.5f * Physics.gravity * t * t);
        }

        aimLine.enabled = true;
    }

    private void HideAimLine()
    {
        if (aimLine != null)
        {
            aimLine.enabled = false;
        }
    }

    private static void SetDiceColliders(
        Rigidbody body,
        bool value)
    {
        foreach (Collider collider in
                 body.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = value;
        }
    }

    private IEnumerator ShowResultWhenStopped(
        float chargePower)
    {
        yield return new WaitForSeconds(
            FlightTime);

        float waited = 0f;

        while (waited < 4f &&
               !AllDiceStopped())
        {
            waited += Time.deltaTime;

            yield return null;
        }

        resultDisplay.Show(
            dice,
            chargePower);
    }

    private bool AllDiceStopped()
    {
        foreach (Rigidbody body in dice)
        {
            if (body != null &&
                body.linearVelocity.magnitude > 0.05f)
            {
                return false;
            }
        }

        return true;
    }

    private void DestroyDice()
    {
        foreach (Rigidbody body in dice)
        {
            if (body != null)
            {
                Destroy(
                    body.gameObject);
            }
        }

        dice.Clear();
    }

    private void ResetAll(
        Animator hands,
        int layer)
    {
        GameLog.Write(
            "HandsAnimationTest",
            "Reset: hands, dice, plate and pedestal back");

        hands.SetLayerWeight(
            layer,
            0f);

        DestroyDice();

        if (plateVisual != null)
        {
            Destroy(
                plateVisual);

            plateVisual = null;
        }

        lift.Lower();
    }

    private void SpawnPlateVisual()
    {
        if (plateVisual != null)
        {
            Destroy(
                plateVisual);
        }

        GameObject plateAsset =
            Resources.Load<GameObject>(
                "Dice/Plate");

        if (plateAsset == null)
        {
            GameLog.Write(
                "HandsAnimationTest",
                "SpawnPlateVisual skipped: Plate prefab not found");

            return;
        }

        plateVisual =
            Instantiate(
                plateAsset,
                PlateTarget,
                plateAsset.transform.rotation);

        Transform platform =
            lift.PlatformPart;

        if (platform == null ||
            !TryGetPlatformTop(
                platform,
                out Vector3 top))
        {
            GameLog.Write(
                "HandsAnimationTest",
                "SpawnPlateVisual: lift platform not found, plate stays at the fixed target");

            top = PlateTarget;
        }

        Renderer[] renderers =
            plateVisual.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length > 0)
        {
            Bounds visual =
                renderers[0].bounds;

            foreach (Renderer renderer in renderers)
            {
                visual.Encapsulate(
                    renderer.bounds);
            }

            plateVisual.transform.position +=
                new Vector3(
                    top.x - visual.center.x,
                    top.y - visual.min.y,
                    top.z - visual.center.z);
        }

        AddDiceCollider();

        if (platform != null)
        {
            plateVisual.transform.SetParent(
                platform,
                true);
        }

        GameLog.Write(
            "HandsAnimationTest",
            $"Plate prefab put on the lift platform, top {top.ToString("F3")}");
    }

    private static bool TryGetPlatformTop(
        Transform platform,
        out Vector3 top)
    {
        bool found = false;
        Bounds bounds = default;

        foreach (Renderer renderer in
                 platform.GetComponentsInChildren<Renderer>(true))
        {
            Mesh mesh = null;

            MeshFilter filter =
                renderer.GetComponent<MeshFilter>();

            if (filter != null)
            {
                mesh = filter.sharedMesh;
            }
            else if (renderer is SkinnedMeshRenderer skinned)
            {
                mesh = skinned.sharedMesh;
            }

            if (mesh == null)
            {
                continue;
            }

            Bounds local =
                mesh.bounds;

            for (int i = 0;
                 i < 8;
                 i++)
            {
                Vector3 corner =
                    local.center +
                    Vector3.Scale(
                        local.extents,
                        new Vector3(
                            (i & 1) == 0 ? -1f : 1f,
                            (i & 2) == 0 ? -1f : 1f,
                            (i & 4) == 0 ? -1f : 1f));

                Vector3 world =
                    renderer.transform.TransformPoint(
                        corner);

                if (!found)
                {
                    bounds =
                        new Bounds(
                            world,
                            Vector3.zero);

                    found = true;
                }
                else
                {
                    bounds.Encapsulate(
                        world);
                }
            }
        }

        top =
            new Vector3(
                bounds.center.x,
                bounds.max.y,
                bounds.center.z);

        return found;
    }

    private void AddDiceCollider()
    {
        int diceLayer =
            LayerMask.NameToLayer(
                "DiceSimulation");

        MeshFilter filter =
            plateVisual.GetComponentInChildren<MeshFilter>(true);

        if (diceLayer < 0 ||
            filter == null)
        {
            return;
        }

        GameObject colliderObject =
            new GameObject("DiceCollider");

        colliderObject.layer = diceLayer;

        colliderObject.transform.SetParent(
            filter.transform,
            false);

        MeshCollider collider =
            colliderObject.AddComponent<MeshCollider>();

        collider.sharedMesh =
            filter.sharedMesh;
    }

    private void SetupDiceCollisions()
    {
        int diceLayer =
            LayerMask.NameToLayer(
                "DiceSimulation");

        if (diceLayer < 0)
        {
            return;
        }

        for (int i = 0;
             i < 32;
             i++)
        {
            if (i != diceLayer)
            {
                Physics.IgnoreLayerCollision(
                    diceLayer,
                    i,
                    true);
            }
        }

        GameLog.Write(
            "HandsAnimationTest",
            "SetupDiceCollisions: dice collide only with the plate and each other");
    }

    private IEnumerator LogFlight(
        Transform plate)
    {
        for (int i = 0;
             i < 8;
             i++)
        {
            yield return new WaitForSeconds(
                0.25f);

            string positions = string.Empty;

            foreach (Rigidbody body in dice)
            {
                if (body != null)
                {
                    positions += $" die(pos={body.position.ToString("F2")} speed={body.linearVelocity.magnitude:0.00})";
                }
            }

            GameLog.Write(
                "HandsAnimationTest",
                $"Flight t={(i + 1) * 0.25f} plate={plate.position.ToString("F2")} dice={positions}");
        }
    }

    private static float ClipLength(
        Animator hands)
    {
        foreach (AnimationClip clip in
                 hands.runtimeAnimatorController.animationClips)
        {
            if (clip.name.EndsWith("DICE_ROLL"))
            {
                return clip.length;
            }
        }

        return 0f;
    }

    private static Animator FindHands()
    {
        if (NetworkClient.localPlayer == null)
        {
            return null;
        }

        foreach (Animator candidate in
                 NetworkClient.localPlayer.GetComponentsInChildren<Animator>(true))
        {
            if (candidate.runtimeAnimatorController != null &&
                candidate.runtimeAnimatorController.name == HandsControllerName)
            {
                return candidate;
            }
        }

        return null;
    }

    private static Transform FindDeep(
        Transform root,
        string name)
    {
        foreach (Transform child in
                 root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
            {
                return child;
            }
        }

        return null;
    }
}
#endif

using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DiceHands : MonoBehaviour
{
    private const string HandsControllerName = "Player_AC";
    private const string LiftControllerName = "Lift_AC";
    private const string IdleState = "hands|hands_idle";
    private const float AutoThrowChargeTime = 0.4f;

    private Animator hands;
    private TorchDemo torch;
    private LoadedDice loaded;
    private GameObject plateBowl;
    private GameObject uiCanvas;
    private bool active;
    private Coroutine autoThrow;

    private void OnEnable()
    {
        GameMaster.DicePhaseStarted += OnDicePhaseStarted;
        GameMaster.DiceRolled += OnDiceRolled;
    }

    private void OnDisable()
    {
        GameMaster.DicePhaseStarted -= OnDicePhaseStarted;
        GameMaster.DiceRolled -= OnDiceRolled;
    }

    private void Start()
    {
        if (GameMaster.Instance != null)
        {
            GameMaster.Instance.GamePhaseChanged += OnPhaseChanged;
        }
    }

    private void OnDestroy()
    {
        if (GameMaster.Instance != null)
        {
            GameMaster.Instance.GamePhaseChanged -= OnPhaseChanged;
        }
    }

    private void OnDicePhaseStarted(
        uint playerNetId,
        float duration)
    {
        bool isThrower =
            NetworkClient.localPlayer != null &&
            NetworkClient.localPlayer.netId == playerNetId;

        GameLog.Write(
            "DiceHands",
            $"OnDicePhaseStarted player={playerNetId} isThrower={isThrower}");

        if (!isThrower)
        {
            return;
        }

        if (!Prepare())
        {
            return;
        }

        active = true;

        SetDiceObjectsActive(
            true);

        torch.enabled = true;
        torch.CurrentWeap = TorchDemo.WeaponType.Dice;

        loaded.enabled = true;
        loaded.InitializeDice();

        hands.CrossFade(
            "DicePrep",
            0.1f,
            0);

        if (hands.layerCount > 1)
        {
            hands.CrossFade(
                "DicePrepFX",
                0.1f,
                1);
        }

        GameLog.Write(
            "DiceHands",
            "Begin: hands switched to DicePrep");
    }

    private void OnDiceRolled(
        uint playerNetId,
        int first,
        int second,
        int third,
        float power)
    {
        if (!active ||
            loaded == null ||
            NetworkClient.localPlayer == null ||
            NetworkClient.localPlayer.netId != playerNetId)
        {
            return;
        }

        loaded.score =
            new[]
            {
                first,
                second,
                third
            };

        string state = CurrentStateName();

        GameLog.Write(
            "DiceHands",
            $"OnDiceRolled score={first},{second},{third} power={power} state={state}");

        bool thrownByHand =
            state == "DiceCharge" ||
            state == "DiceAttack" ||
            loaded.DiceVisible;

        if (!thrownByHand)
        {
            if (autoThrow != null)
            {
                StopCoroutine(
                    autoThrow);
            }

            autoThrow =
                StartCoroutine(
                    AutoThrow(
                        power));
        }
    }

    private void OnPhaseChanged(
        GameMaster.GamePhase phase)
    {
        if (phase == GameMaster.GamePhase.Dice ||
            !active)
        {
            return;
        }

        Finish();
    }

    private IEnumerator AutoThrow(
        float power)
    {
        GameLog.Write(
            "DiceHands",
            "AutoThrow start");

        torch.Charge = power;

        hands.SetBool(
            "IsCharging",
            true);

        yield return new WaitForSeconds(
            AutoThrowChargeTime);

        hands.SetBool(
            "IsCharging",
            false);

        loaded.DiceVisible = true;

        GameLog.Write(
            "DiceHands",
            "AutoThrow done");
    }

    private void Finish()
    {
        GameLog.Write(
            "DiceHands",
            "Finish: hands back to idle");

        active = false;

        if (autoThrow != null)
        {
            StopCoroutine(
                autoThrow);

            autoThrow = null;
        }

        if (torch != null)
        {
            torch.enabled = false;
        }

        if (loaded != null)
        {
            loaded.DiceVisible = false;
            loaded.enabled = false;
        }

        SetDiceObjectsActive(
            false);

        if (hands != null)
        {
            hands.SetBool(
                "IsCharging",
                false);

            hands.CrossFade(
                IdleState,
                0.2f,
                0);
        }
    }

    private string CurrentStateName()
    {
        AnimatorStateInfo info =
            hands.GetCurrentAnimatorStateInfo(0);

        foreach (string name in new[]
                 {
                     "DicePrep",
                     "DiceReady",
                     "DiceCharge",
                     "DiceAttack",
                     "DiceIdle"
                 })
        {
            if (info.IsName(name))
            {
                return name;
            }
        }

        return "other";
    }

    private bool Prepare()
    {
        if (hands == null)
        {
            hands = FindHands();
        }

        if (hands == null)
        {
            GameLog.Write(
                "DiceHands",
                $"Prepare failed: no local Animator with controller {HandsControllerName}");

            return false;
        }

        if (torch != null &&
            loaded != null)
        {
            return true;
        }

        torch =
            hands.GetComponent<TorchDemo>();

        if (torch == null)
        {
            torch =
                hands.gameObject.AddComponent<TorchDemo>();

            torch.enabled = false;
            torch.hands = hands.gameObject;
            torch.ChargeSpeed = 0.5f;
            torch.CurrentWeap = TorchDemo.WeaponType.Dice;
            torch.fire1 = new InputAction("fire1", InputActionType.Button, "<Mouse>/leftButton");
            torch.fire2 = new InputAction("fire2");
            torch.fire3 = new InputAction("fire3");
            torch.slot1 = new InputAction("slot1");
            torch.slot2 = new InputAction("slot2");
            torch.slot3 = new InputAction("slot3");
            torch.slot4 = new InputAction("slot4");
            torch.ChargeSlider = CreateHiddenSlider();
        }

        loaded =
            hands.GetComponent<LoadedDice>();

        if (loaded == null)
        {
            loaded =
                hands.gameObject.AddComponent<LoadedDice>();

            loaded.enabled = false;

            if (!ConfigureLoadedDice())
            {
                Destroy(
                    loaded);

                loaded = null;

                return false;
            }
        }

        return true;
    }

    private bool ConfigureLoadedDice()
    {
        GameObject diePrefab =
            Resources.Load<GameObject>(
                "Dice/SingleDie");

        Transform first = FindDeep(hands.transform, "dice1");
        Transform second = FindDeep(hands.transform, "dice2");
        Transform third = FindDeep(hands.transform, "dice3");

        if (diePrefab == null ||
            first == null ||
            second == null ||
            third == null)
        {
            GameLog.Write(
                "DiceHands",
                $"ConfigureLoadedDice failed: diePrefab={(diePrefab != null)} dice1={(first != null)} dice2={(second != null)} dice3={(third != null)}");

            return false;
        }

        loaded.DiePrefab = diePrefab;

        loaded.DiceControlBones =
            new List<GameObject>
            {
                first.gameObject,
                second.gameObject,
                third.gameObject
            };

        Transform cameraTransform =
            Camera.main != null
                ? Camera.main.transform
                : hands.transform;

        loaded.MainCamera = cameraTransform;

        GameObject origin =
            new GameObject("DiceOrigin");

        origin.transform.SetParent(
            cameraTransform,
            false);

        origin.transform.position =
            first.position;

        origin.transform.rotation =
            cameraTransform.rotation;

        loaded.DiceOrigin = origin.transform;

        Transform plate =
            FindPlate();

        if (plate == null)
        {
            GameLog.Write(
                "DiceHands",
                "ConfigureLoadedDice failed: plate transform not found in the lift");

            return false;
        }

        loaded.Plate = plate;

        AttachPlateBowl(
            plate);

        loaded.UIDice =
            CreateUiDice(
                cameraTransform);

        GameLog.Write(
            "DiceHands",
            $"ConfigureLoadedDice done plate={plate.name} uiDice={loaded.UIDice.Count}");

        return loaded.UIDice.Count == 3;
    }

    private void AttachPlateBowl(
        Transform plate)
    {
        if (plateBowl != null)
        {
            return;
        }

        GameObject prefab =
            Resources.Load<GameObject>(
                "Dice/DicePlate");

        if (prefab == null)
        {
            GameLog.Write(
                "DiceHands",
                "AttachPlateBowl skipped: DicePlate prefab not found");

            return;
        }

        plateBowl =
            Instantiate(
                prefab,
                plate,
                false);
    }

    private List<GameObject> CreateUiDice(
        Transform cameraTransform)
    {
        List<GameObject> result =
            new List<GameObject>();

        GameObject prefab =
            Resources.Load<GameObject>(
                "Dice/DiceUIBase");

        if (prefab == null)
        {
            GameLog.Write(
                "DiceHands",
                "CreateUiDice skipped: DiceUIBase prefab not found");

            return result;
        }

        uiCanvas =
            new GameObject("DiceUICanvas");

        Canvas canvas =
            uiCanvas.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 1f;
        canvas.sortingOrder = 10;

        CanvasScaler scaler =
            uiCanvas.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject root =
            Instantiate(
                prefab,
                uiCanvas.transform,
                false);

        RectTransform rect =
            root.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.anchoredPosition = new Vector2(0f, -250f);
        }

        foreach (string name in new[]
                 {
                     "UIDie1",
                     "UIDie2",
                     "UIDie3"
                 })
        {
            Transform die =
                FindDeep(root.transform, name);

            if (die != null)
            {
                result.Add(
                    die.gameObject);
            }
        }

        return result;
    }

    private Slider CreateHiddenSlider()
    {
        GameObject go =
            new GameObject("DiceChargeSlider");

        go.transform.SetParent(
            transform,
            false);

        Slider slider =
            go.AddComponent<Slider>();

        slider.minValue = 0f;
        slider.maxValue = 1f;

        return slider;
    }

    private void SetDiceObjectsActive(
        bool value)
    {
        foreach (Rigidbody body in
                 FindObjectsByType<Rigidbody>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (body.name.StartsWith("SingleDie"))
            {
                body.gameObject.SetActive(
                    value);
            }
        }

        if (uiCanvas != null)
        {
            uiCanvas.SetActive(
                value);
        }
    }

    private Animator FindHands()
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

    private Transform FindPlate()
    {
        foreach (Animator candidate in
                 FindObjectsByType<Animator>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (candidate.runtimeAnimatorController != null &&
                candidate.runtimeAnimatorController.name == LiftControllerName)
            {
                return FindDeep(
                    candidate.transform,
                    "plate");
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

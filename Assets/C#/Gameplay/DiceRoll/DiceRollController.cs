using System.Collections.Generic;
using UnityEngine;

// Wires the dice roll parts together and calls them in the order of the original LoadedDice.FixedUpdate.
public class DiceRollController : MonoBehaviour
{
    public GameObject DiePrefab;
    public Transform MainCamera;
    public List<GameObject> DiceControlBones = new List<GameObject>();
    public List<GameObject> UIDice = new List<GameObject>();
    public Transform Plate;
    public Transform DiceOrigin;
    public bool DiceVisible = false;
    public DiceRollSettings Settings = new DiceRollSettings();

    private DiceBodies bodies;
    private DiceAnimationFollower follower;
    private DiceScoreTweaker tweaker;
    private DiceBounceLimiter bounceLimiter;
    private DiceFlashEffect flashEffect;
    private DiceUiRotator uiRotator;
    private DiceUiVisibility uiVisibility;
    private IDiceCharge charge;
    private IDiceAnimationState animationState;

    public int[] Score =>
        Settings.score;

    public IReadOnlyList<GameObject> PhysicalDice =>
        bodies != null
            ? bodies.Dice
            : new List<GameObject>();

    // Optional: replace the default sources (TorchDemo and the Animator).
    public void Configure(
        IDiceCharge charge,
        IDiceAnimationState animationState)
    {
        this.charge = charge;
        this.animationState = animationState;
    }

    public void InitializeDice()
    {
        bodies.Initialize(
            DiePrefab,
            DiceControlBones,
            Settings.ForceVariation);
    }

    private void Start()
    {
        Animator anim =
            gameObject.GetComponent<Animator>();

        anim.updateMode = AnimatorUpdateMode.Fixed;

        if (animationState == null)
        {
            animationState =
                new AnimatorDiceState(
                    anim);
        }

        if (charge == null)
        {
            charge =
                new TorchDemoCharge(
                    GetComponent<TorchDemo>());
        }

        bodies =
            new DiceBodies();

        InitializeDice();

        follower =
            new DiceAnimationFollower(
                Settings,
                bodies.Dice,
                DiceControlBones,
                bodies.ForceMultipliers,
                DiceOrigin,
                Plate,
                animationState,
                charge);

        tweaker =
            new DiceScoreTweaker(
                Settings,
                bodies.Dice);

        bounceLimiter =
            new DiceBounceLimiter(
                Settings,
                bodies.Dice);

        flashEffect =
            new DiceFlashEffect(
                Settings,
                bodies.Dice,
                UIDice,
                new DiceStopDetector(
                    Settings,
                    bodies.Dice,
                    animationState));

        uiRotator =
            new DiceUiRotator(
                Settings,
                bodies.Dice,
                UIDice,
                MainCamera);

        uiVisibility =
            new DiceUiVisibility(
                Settings,
                UIDice);
    }

    private void FixedUpdate()
    {
        uiVisibility.Update(
            DiceVisible);

        follower.Follow();
        tweaker.Tweak();
        bounceLimiter.Limit();
        flashEffect.Update();
        uiRotator.Rotate();
    }
}

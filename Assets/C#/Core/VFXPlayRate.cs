using UnityEngine;
using UnityEngine.VFX;

[ExecuteAlways]
[RequireComponent(typeof(VisualEffect))]
public class VFXFixedPlayRate : MonoBehaviour {
    [SerializeField, Min(0f)]
    private float playRate = 2f;

    private VisualEffect vfx;

    private void OnEnable() {
        vfx = GetComponent<VisualEffect>();
        Apply();
    }

    private void OnValidate() {
        vfx = GetComponent<VisualEffect>();
        Apply();
    }

    private void Update() {
        Apply();
    }

    private void Apply() {
        if (vfx == null)
            vfx = GetComponent<VisualEffect>();

        if (vfx != null && !Mathf.Approximately(vfx.playRate, playRate))
            vfx.playRate = playRate;
    }
}
using UnityEngine;

public class UIWindow : MonoBehaviour
{
    [SerializeField] protected bool isShown = true;
    public virtual void Toggle()
    {
        GameLog.Write(
            "UIWindow",
            $"Toggle window={name} wasShown={isShown}");

        if (isShown)
        {
            isShown = false;
            gameObject.SetActive(false);
        }
        else
        {
            isShown = true;
            gameObject.SetActive(true);
        }
    }
    public virtual void Hide()
    {
        GameLog.Write(
            "UIWindow",
            $"Hide window={name} wasShown={isShown}");

        isShown = false;
        gameObject.SetActive(false);
    }
    public virtual void Show()
    {
        GameLog.Write(
            "UIWindow",
            $"Show window={name} wasShown={isShown}");

        isShown = true;
        gameObject.SetActive(true);
    }
}

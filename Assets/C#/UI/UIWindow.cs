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
            GameAudioManager.Instance?.PlayMenuClose();
            gameObject.SetActive(false);
        }
        else
        {
            isShown = true;
            GameAudioManager.Instance?.PlayMenuOpen();
            gameObject.SetActive(true);
        }
    }
    public virtual void Hide()
    {
        GameLog.Write(
            "UIWindow",
            $"Hide window={name} wasShown={isShown}");

        isShown = false;
        GameAudioManager.Instance?.PlayMenuClose();
        gameObject.SetActive(false);
    }
    public virtual void Show()
    {
        GameLog.Write(
            "UIWindow",
            $"Show window={name} wasShown={isShown}");

        isShown = true;
        GameAudioManager.Instance?.PlayMenuOpen();
        gameObject.SetActive(true);
    }
}

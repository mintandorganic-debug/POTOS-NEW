using UnityEngine;

public class UIWindow : MonoBehaviour
{
    [SerializeField] protected bool isShown = true;
    public virtual void Toggle()
    {
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
        isShown = false;
        GameAudioManager.Instance?.PlayMenuClose();
        gameObject.SetActive(false);
    }
    public virtual void Show()
    {
        isShown = true;
        GameAudioManager.Instance?.PlayMenuOpen();
        gameObject.SetActive(true);
    }
}

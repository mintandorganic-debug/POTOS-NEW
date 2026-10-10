using UnityEngine;

public class UIWindow : MonoBehaviour
{
    [SerializeField] protected bool isShown = true;
    public virtual void Toggle()
    {
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
        isShown = false;
        gameObject.SetActive(false);
    }
    public virtual void Show()
    {
        isShown = true;
        gameObject.SetActive(true);
    }
}

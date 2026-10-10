using UnityEngine;

public class WS_BankHUD : MonoBehaviour
{
    void Update()
    {
         transform.LookAt(Camera.main.transform.position);
    }
}

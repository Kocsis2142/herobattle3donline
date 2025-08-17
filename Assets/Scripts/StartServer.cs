using Mirror;
using UnityEngine;

public class StartServer : MonoBehaviour
{
    void Start()
    {
#if UNITY_SERVER
        Debug.Log("Starting dedicated server...");
        NetworkManager.singleton.StartServer();
#endif
    }
}

using Mirror;
using UnityEngine;

public class StartClient : MonoBehaviour
{
    public string serverAddress = "127.0.0.1"; // LAN teszt esetén
    public int port = 7777;

    void Start()
    {
#if !UNITY_SERVER
        var nm = NetworkManager.singleton;
        nm.networkAddress = serverAddress;
        Debug.Log($"Connecting to {serverAddress}:{port}...");
        nm.StartClient();
#endif
    }
}

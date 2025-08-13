using Mirror;
using UnityEngine;

public class NetworkManagerCustom : NetworkManager
{
    public GridManager gridManagerPrefab;

    private GridManager gridManagerInstance;

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Spawnoljuk a GridManagert szerver oldalon
        gridManagerInstance = Instantiate(gridManagerPrefab);
        NetworkServer.Spawn(gridManagerInstance.gameObject);
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        PlayerController player = conn.identity.GetComponent<PlayerController>();
        player.team = numPlayers == 1 ? Team.Friendly : Team.Enemy;

        Debug.Log($"[Server] Player {conn.connectionId} team = {player.team}");
    }
}

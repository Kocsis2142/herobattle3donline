using Mirror;
using UnityEngine;
using System.Collections;

public class NetworkManagerCustom : NetworkManager
{
    public GridManager gridManagerPrefab;

    private GridManager gridManagerInstance;

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Spawnoljuk a GridManagert szerver oldalon
       // gridManagerInstance = Instantiate(gridManagerPrefab);
       // NetworkServer.Spawn(gridManagerInstance.gameObject);
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        /*   var player = conn.identity.GetComponent<Player>();
           player.myTeam = (numPlayers == 1) ? Team.Friendly : Team.Enemy;
           Debug.Log($"[Server] New Player joined to the lobby.");
           Debug.Log($"[Server] Player {numPlayers - 1} team = {player.myTeam}");*/

        /*  if (NetworkServer.connections.Count == 2)
          {
              Debug.Log($"[Server] 2 player joined game starting.");
              GameManager.Instance.StartGame();
          }*/

    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);

        Debug.Log("Player connected: " + conn.connectionId);

        // Ha 2 játékos van, indítsuk a játékot
        if (NetworkServer.connections.Count == 2)
        {
            Debug.Log("2 player joined starting game...");
            GameManager.Instance.StartGame();
        }
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
        Debug.Log($"Player left. Current: ");
    }

  /*  [Server]
    void PlacePlayer(Player player)
    {
        var gm = GridManager.Instance;
        player.transform.position = gm.GetHeroSpawnPosition(player.myTeam);
        player.transform.forward = (gm.BoardCenter - player.transform.position).normalized;
    }

    [Server]
    IEnumerator PlaceWhenReady(Player player)
    {
        while (GridManager.Instance == null) yield return null;
        PlacePlayer(player);
    }*/

}


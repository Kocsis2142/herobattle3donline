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
        gridManagerInstance = Instantiate(gridManagerPrefab);
        NetworkServer.Spawn(gridManagerInstance.gameObject);
    }

public override void OnServerAddPlayer(NetworkConnectionToClient conn)
{
    base.OnServerAddPlayer(conn);

    var player = conn.identity.GetComponent<Player>();
    player.myTeam = (numPlayers == 1) ? Team.Friendly : Team.Enemy;
    Debug.Log($"[Server] Player {numPlayers - 1} team = {player.myTeam}");

    // Ha a GridManager már létezik → azonnal spawn pozíció
    if (GridManager.Instance != null)
    {
        PlacePlayer(player);
    }
    else
    {
        // Ha még nincs grid, várjunk rá
        StartCoroutine(PlaceWhenReady(player));
    }
}

[Server]
void PlacePlayer(Player player)
{
    var gm = GridManager.Instance;
    player.transform.position = gm.GetHeroSpawnPosition(player.myTeam);
    player.transform.forward  = (gm.BoardCenter - player.transform.position).normalized;
}

[Server]
IEnumerator PlaceWhenReady(Player player)
{
    while (GridManager.Instance == null) yield return null;
    PlacePlayer(player);
}

}


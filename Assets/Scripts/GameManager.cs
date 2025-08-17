using Mirror;
using UnityEngine;
using System.Collections;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    public GridManager gridManagerPrefab;

    private GridManager gridManagerInstance;


    void Awake()
    {
        Instance = this;
    }

    [Server]
    public void StartGame()
    {
        gridManagerInstance = Instantiate(gridManagerPrefab);
        NetworkServer.Spawn(gridManagerInstance.gameObject);

        int i = 0;
        foreach (var conn in NetworkServer.connections.Values)
        {
            GameObject heroPrefab = Resources.Load<GameObject>("NetworkPrefabs/Player");
            GameObject playerObj = Instantiate(heroPrefab);
            NetworkServer.AddPlayerForConnection(conn, playerObj);

            var player = playerObj.GetComponent<Player>();

            player.myTeam = (i == 1) ? Team.Friendly : Team.Enemy;

            if (i == 0)
            {
                // első player – spawn első hero
                TargetStartGame(player);
            }
            else if (i == 1)
            {
                // második player – spawn második hero
                TargetStartGame(player);
            }

            i++;
        }
    }

    [TargetRpc]
    private void TargetStartGame(Player player)
    {
        Debug.Log("Game started on client!");


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
        player.transform.forward = (gm.BoardCenter - player.transform.position).normalized;
    }

    [Server]
    IEnumerator PlaceWhenReady(Player player)
    {
        while (GridManager.Instance == null) yield return null;
        PlacePlayer(player);
    }
}

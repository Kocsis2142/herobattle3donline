using Mirror;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SyncVar] public Team team;
   // private GridManager grid;

    public override void OnStartClient()
    {
      //  grid = FindObjectOfType<GridManager>();
    }

    [Command]
    public void CmdRequestSpawnSoldier(Vector3 position, int team, string unitName)
    {
        if (!isServer) return;

          GameObject prefab = Resources.Load<GameObject>($"Units/SoldierPrefab");
        if (prefab == null)
        {
            Debug.LogWarning($"Prefab nem tal�lhat�: {prefab}");
            return;
        }

        GameObject soldier = Instantiate(prefab, position, Quaternion.identity);

        // Network spawn
        NetworkServer.Spawn(soldier);

        Vector3 targetPos = position + new Vector3(5f, 0f, 0f);
        soldier.GetComponent<SoldierController>().SetTargetPosition(targetPos);

        // Ha szeretn�l csapatot be�ll�tani:
        /*   var unit = soldier.GetComponent<Unit>();
           if (unit != null)
           {
               unit.team = team;
           }*/
    }
}

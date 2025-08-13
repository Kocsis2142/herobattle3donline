using Mirror;
using UnityEngine;

public class GridManager : NetworkBehaviour
{
    public static GridManager Instance;

    public GameObject tilePrefab;
    public int width = 8;
    public int height = 5;
    public float tileSize = 1f;

    public override void OnStartServer()
    {
        base.OnStartServer();
        GenerateGrid();
    }

    private void Awake()
    {
        Instance = this;
    }

    [Server]
    private void GenerateGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 pos = new Vector3(x * tileSize, 0, z * tileSize);
                var tile = Instantiate(tilePrefab, pos, Quaternion.identity);
                NetworkServer.Spawn(tile);
            }
        }
    }

    public Vector3 SnapToNearestTile(Vector3 worldPos)
    {
        int gx = Mathf.RoundToInt(worldPos.x / tileSize);
        int gz = Mathf.RoundToInt(worldPos.z / tileSize);

        gx = Mathf.Clamp(gx, 0, width - 1);
        gz = Mathf.Clamp(gz, 0, height - 1);

        return new Vector3(gx * tileSize, 0, gz * tileSize);
    }
}

using Mirror;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Szerver-oldali grid generálás.
/// - Minden tile kap TileVisual-t, amiben SyncVar-ben tároljuk a (col,row)-t.
/// - A kliens raycast-tel eltalálja a tile-t, és KÖZVETLENÜL onnan olvassa a cellát.
/// - Így nincs több kliens-szerver kerekítés / offset desync.
/// </summary>
public class GridManager : NetworkBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid beállítások")]
    public int width = 9;
    public int height = 5;
    public float tileSize = 1f;
    [Tooltip("Vizuális rés a quadok között (csak a skálát csökkentjük).")]
    [Range(0f, 0.4f)] public float visualGap = 0.04f;
    public GameObject tilePrefab;

    [Header("Spawn zónák (oszlopszám)")]
    public int friendlySpawnCols = 3;
    public int enemySpawnCols = 3;

    [Header("Raycast a csempékre")]
    public LayerMask tilesLayerMask;   // Inspectorban pipáld be a 'Tiles' layert

    [Header("Highlight színek (ha a DragDrop nem TileVisualt használna)")]
    public Color tileNormalColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    public Color tileAllowedColor = Color.white;
    public Color tileDeniedColor = new Color(1f, 0.4f, 0.4f, 1f);

    // --- állapot/segéd ---
    Transform tilesRoot;

    // szerver: foglalt cellák (egyszerű ütközés elkerüléshez)
    readonly HashSet<int> _occupied = new HashSet<int>();
    int IndexFromCell(Vector2Int c) => c.y * width + c.x;

    // grid-közép a kamerához/herohoz
    public Vector3 BoardCenter
    {
        get
        {
            int mx = Mathf.FloorToInt((width - 1) * 0.5f);
            int mz = Mathf.FloorToInt((height - 1) * 0.5f);
            return CellToWorldCenter(new Vector2Int(mx, mz));
        }
    }

    void Awake() => Instance = this;

    public override void OnStartServer()
    {
        base.OnStartServer();
        GenerateGrid();
    }

    // --- Grid építés szerveren ---
    [Server]
    void GenerateGrid()
    {
        if (!tilePrefab)
        {
            Debug.LogError("[Grid] tilePrefab nincs beállítva!");
            return;
        }

        EnsureTilesRoot();

        float scaleXY = Mathf.Clamp01(1f - visualGap);
        Vector3 off = new((width - 1) * tileSize / 2f, 0f, (height - 1) * tileSize / 2f);
        int tilesLayer = LayerMask.NameToLayer("Tiles");

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 pos = new Vector3(x * tileSize, 0f, z * tileSize) - off;

                var go = Instantiate(tilePrefab, pos, Quaternion.identity, tilesRoot);

                // vizuális rés: csak XZ skála csökkentés (a rácstáv marad tileSize)
                var s = go.transform.localScale;
                go.transform.localScale = new Vector3(scaleXY * s.x, s.y, scaleXY * s.z);

                // biztos ami biztos: collider + Tiles layer
                if (!go.TryGetComponent<Collider>(out _)) go.AddComponent<BoxCollider>();
                if (tilesLayer >= 0) go.layer = tilesLayer;

                // TileVisual felvétele és (col,row) beégetése SyncVar-be
                var tv = go.GetComponent<TileVisual>() ?? go.AddComponent<TileVisual>();
                tv.ServerInit(x, z);

                NetworkServer.Spawn(go);
            }
        }
    }

    void EnsureTilesRoot()
    {
        if (tilesRoot) return;
        var t = transform.Find("TilesRoot");
        tilesRoot = t ? t : new GameObject("TilesRoot").transform;
        tilesRoot.SetParent(transform, false);
    }

    // --- Cell ↔ World: szerver és kliens is ugyanezt használja ---
    public Vector2Int WorldToCell(Vector3 world, out Vector3 center)
    {
        Vector3 off = new((width - 1) * tileSize / 2f, 0f, (height - 1) * tileSize / 2f);
        float gx = (world.x + off.x) / tileSize;
        float gz = (world.z + off.z) / tileSize;

        // stabil „round”: floor(x + 0.5)
        int cx = Mathf.Clamp(Mathf.FloorToInt(gx + 0.5f), 0, width - 1);
        int cz = Mathf.Clamp(Mathf.FloorToInt(gz + 0.5f), 0, height - 1);

        center = new Vector3(cx * tileSize, 0f, cz * tileSize) - off;
        return new Vector2Int(cx, cz);
    }

    public Vector3 CellToWorldCenter(Vector2Int cell)
    {
        Vector3 off = new((width - 1) * tileSize / 2f, 0f, (height - 1) * tileSize / 2f);
        return new Vector3(cell.x * tileSize, 0f, cell.y * tileSize) - off;
    }

    // pontos Y a tile tetején (Tiles layeren lévő colliderre raycast)
    public float GetTileTopYAtWorldXZ(Vector3 worldXZ, float castHeight = 50f)
    {
        Vector3 origin = new(worldXZ.x, castHeight, worldXZ.z);
        if (Physics.Raycast(origin, Vector3.down, out var hit, castHeight * 2f, tilesLayerMask))
            return hit.point.y;
        return 0f;
    }

    // kliens: képernyő → TileVisual + cella + találati pont
    public bool TryRayToGrid(Camera cam, Vector2 screenPos, out TileVisual tile, out Vector2Int cell, out Vector3 hitPoint)
    {
        var ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out var hit, 500f, tilesLayerMask))
        {
            tile = hit.collider.GetComponentInParent<TileVisual>();
            if (tile != null)
            {
                cell = new Vector2Int(tile.col, tile.row);
                hitPoint = hit.point;
                return true;
            }
        }
        tile = null; cell = default; hitPoint = default;
        return false;
    }

    // --- Hero spawn: bal/jobb SZÉL ELÉ tesszük (fél tile + padding) ---
    public Vector3 GetHeroSpawnPosition(Team team, float xPaddingTiles = 1.0f)
    {
        int midRow = Mathf.FloorToInt((height - 1) * 0.5f);
        var leftC = CellToWorldCenter(new Vector2Int(0, midRow));
        var rightC = CellToWorldCenter(new Vector2Int(width - 1, midRow));

        float pad = xPaddingTiles * tileSize;
        if (team == Team.Friendly)
            return new Vector3(leftC.x - (tileSize * 0.5f) - pad, GetTileTopYAtWorldXZ(leftC), leftC.z);
        else
            return new Vector3(rightC.x + (tileSize * 0.5f) + pad, GetTileTopYAtWorldXZ(rightC), rightC.z);
    }

    // --- Foglaltság nyilvántartás (egyszerű) ---
    [Server] public void ServerMarkOccupied(Vector2Int cell, uint _netId) { if (InBoundsCell(cell)) _occupied.Add(IndexFromCell(cell)); }
    [Server] public void ServerClearOccupied(Vector2Int cell) { _occupied.Remove(IndexFromCell(cell)); }
    [Server] public bool ServerIsOccupied(Vector2Int cell) => !InBoundsCell(cell) || _occupied.Contains(IndexFromCell(cell));
    public bool InBoundsCell(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < width && c.y < height;

#if UNITY_EDITOR
    [ContextMenu("Rebuild Grid (Server)")]
#endif
    [Server]
    public void RebuildGrid()
    {
        EnsureTilesRoot();

        for (int i = tilesRoot.childCount - 1; i >= 0; i--)
        {
            var child = tilesRoot.GetChild(i);
            var ni = child.GetComponent<NetworkIdentity>();
            if (ni && ni.isServer) NetworkServer.Destroy(child.gameObject);
            else Destroy(child.gameObject);
        }

        GenerateGrid();
        Debug.Log($"[Grid] Rebuilt: {width}x{height} tileSize={tileSize} gap={visualGap}");
    }


}

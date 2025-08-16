using Mirror;
using UnityEngine;
using System.Collections;

public partial class Player : NetworkBehaviour
{
    [Header("Alap soldier prefab (ha a UnitCardon nincs override)")]
    [SerializeField] private GameObject defaultSoldierPrefab;

    [Header("Skin / modell a Hero-hoz (NEM network prefab!)")]
    [Tooltip("Opcionális: egy sima modell/mesh prefab, NetworkIdentity NÉLKÜL. Gyerekként rátesszük a Playerre.")]
    [SerializeField] private GameObject heroModelPrefab;

    [SyncVar(hook = nameof(OnTeamChanged))]
    public Team myTeam;

    // belső flag csak a szervernek: egyszer állítsuk be a pozíciót és a modellt
    bool serverHeroPlaced;
    bool serverModelAttached;

    public override void OnStartServer()
    {
        base.OnStartServer();
        // szerver-oldalon a Player = Hero → a pálya megjelenése után a helyére tesszük
        StartCoroutine(Server_PlacePlayerAsHeroWhenGridReady());
    }

    /// <summary>
    /// Megvárjuk, míg a GridManager biztosan él (szerver generálja a rácsot),
    /// aztán a Player-t a hero helyére tesszük és opcionálisan ráakasztjuk a modellt.
    /// </summary>
    private IEnumerator Server_PlacePlayerAsHeroWhenGridReady()
    {
        yield return null; // 1 frame biztos, mire OnStartServer-ek lefutnak
        var gm = GridManager.Instance;
        int tries = 12;
        while (!gm && tries-- > 0)
        {
            yield return null;
            gm = GridManager.Instance;
        }
        if (!gm) yield break;

        if (!serverHeroPlaced)
        {
            // 1) pontos spawn a grid BAL/JOBB SZÉLE ELÉ (fél tile + padding)
            Vector3 pos = gm.GetHeroSpawnPosition(myTeam, 1.2f);
            transform.position = pos;

            // 2) nézzen a pálya közepe felé (opcionális, jól néz ki)
            var look = gm.BoardCenter; look.y = transform.position.y;
            transform.rotation = Quaternion.LookRotation((look - transform.position).normalized, Vector3.up);

            // 3) fizikák: ne essen át / ne boruljon fel
            if (TryGetComponent<Rigidbody>(out var rb))
            {
                rb.useGravity = false;
                rb.isKinematic = false;
                rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            }

            // 4) opcionális vizuális modell gyerekként (NEM network prefab!)
            if (!serverModelAttached && heroModelPrefab != null && transform.Find("Model") == null)
            {
                var m = Instantiate(heroModelPrefab, transform);
                m.name = "Model";

                // ha véletlen lenne rajta NetworkIdentity, leszedjük (a Player már hálózati objektum)
                var ni = m.GetComponent<NetworkIdentity>();
                if (ni) Destroy(ni);
                serverModelAttached = true;
            }

            serverHeroPlaced = true;
        }
    }

    // ======== KÖVETKEZŐK: egység spawn továbbra is SZERVEREN történik ========

    // közös segéd egységekhez (Defender/Attacker)
    [Server]
    private GameObject ServerSpawnSoldierAt(UnitCard def, Vector2Int cell, CardRole role)
    {
        var gm = GridManager.Instance;
        if (!gm) { Debug.LogWarning("[Player] Nincs GridManager."); return null; }

        // 1) cella → világ közép + tile teteje (Y)
        var center = gm.CellToWorldCenter(cell);
        float topY = gm.GetTileTopYAtWorldXZ(center);
        center.y = topY;

        // 2) PREFAB kiválasztása (ELŐBB deklarálunk!)
        var prefab = (def && def.prefabOverride) ? def.prefabOverride : defaultSoldierPrefab;
        if (!prefab) { Debug.LogWarning("[Player] Nincs soldier prefab."); return null; }

        // 3) Példányosítás (ideiglenesen a tile tetejére)
        var go = Instantiate(prefab, center, Quaternion.identity);

        // 4) Fizika beállítás (ne essen át)
        EnsureUnitPhysics(go);

        // 5) Fél magasság a PÉLDÁNYRÓL → Y emelés
        float halfH = GetHalfHeight(go);
        if (halfH < 0.25f) halfH = 0.25f;
        float spawnY = topY + halfH + 0.01f;
        go.transform.position = new Vector3(center.x, spawnY, center.z);

        // ÚJ: számoljuk ki a grid origó X-ét (világban) a cella és center alapján
        float originXWorld = center.x - cell.x * gm.tileSize;
        // A sor világ Z-je a center.z
        float rowWorldZ = center.z;

        // !! Adjunk át mindent világtérben (spawnY, originX, rowWorldZ)
        if (go.TryGetComponent<SoldierController>(out var sc))
        {
            sc.ServerInit(
                myTeam,
                role,
                cell.y,           // sor index információ kedvéért megtarthatod
                gm.tileSize,
                spawnY,           // NEM tileTopY, hanem az emelt Y!
                gm.width,
                center.x,         // startX világban
                originXWorld,          // ÚJ: grid origó X világban
                rowWorldZ         // ÚJ: sor Z világban
            );
        }

        // 7) Network spawn (játékoshoz authority-vel)
        NetworkServer.Spawn(go, connectionToClient);
        return go;
    }


    [Server]
    private void EnsureUnitPhysics(GameObject go)
    {
        if (go.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        }
        if (!go.TryGetComponent<Collider>(out _))
            go.AddComponent<CapsuleCollider>();
    }

    // === PARANCSOK: a kliens CSAK (col,row)+unitName-et küld ===

    [Command]
    public void CmdSpawnDefender(int col, int row, string unitName)
    {
        var gm = GridManager.Instance;
        var db = UnitDatabase.Instance;
        if (!gm || !db) return;

        var def = db.GetByName(unitName);
        if (!def) { Debug.LogWarning($"[Player] UnitCard nem található: {unitName}"); return; }

        // baráti sáv ellenőrzése
        if (myTeam == Team.Friendly && col >= gm.friendlySpawnCols) return;
        if (myTeam == Team.Enemy && col < gm.width - gm.enemySpawnCols) return;

        var cell = new Vector2Int(
            Mathf.Clamp(col, 0, gm.width - 1),
            Mathf.Clamp(row, 0, gm.height - 1)
        );

        ServerSpawnSoldierAt(def, cell, CardRole.Defender);
    }

[Command]
public void CmdSpawnAttacker(int row, string unitName)
{
    var gm = GridManager.Instance;
    var db = UnitDatabase.Instance;
    if (!gm || !db) return;

    var def = db.GetByName(unitName);
    if (!def) { Debug.LogWarning($"[Player] UnitCard nem található: {unitName}"); return; }

    int r = Mathf.Clamp(row, 0, gm.height - 1);

    // Friendly balról (0), Enemy jobbról (width-1) indul
    int startCol = (myTeam == Team.Friendly) ? 0 : gm.width - 1;
    var startCell = new Vector2Int(startCol, r);

    ServerSpawnSoldierAt(def, startCell, CardRole.Attacker);
}


    static float GetHalfHeight(GameObject go, float fallback = 0.005f)
    {
        float h = 0f;
        var col = go.GetComponentInChildren<Collider>(true);
        if (col != null) h = Mathf.Max(h, col.bounds.extents.y);

        var rend = go.GetComponentInChildren<Renderer>(true);
        if (rend != null) h = Mathf.Max(h, rend.bounds.extents.y);

        return h > 0 ? h : fallback;
    }

    void OnTeamChanged(Team oldV, Team newV)
{
    if (!isLocalPlayer) return;

    var rig = FindFirstObjectByType<CameraRigController>();
    if (rig) rig.SetTeam(newV);

    var dd = FindFirstObjectByType<DragDropManager>();
    if (dd) dd.SetLocalTeam(newV);
}

}
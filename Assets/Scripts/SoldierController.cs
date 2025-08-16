using Mirror;
using UnityEngine;

/// <summary>
/// Szerver-vezérelt unit:
///  - Defender: helyben áll (rácsközép, sor fix, tile teteje)
///  - Attacker: X irányban halad előre (Friend → +X, Enemy → -X),
///              célpont-alapú mozgással, és nem hagyhatja el a pályát.
///  - Mindig Z (sor) és Y (tile teteje) fix.
///  - Meghagyott SetTargetPosition kompatibilitás.
/// </summary>
public class SoldierController : NetworkBehaviour
{
    [Header("Mozgás")]
    [SerializeField] private float _moveSpeed = 2f;   // szerkeszthető a prefabon
    public float MoveSpeed { get => _moveSpeed; set => _moveSpeed = Mathf.Max(0.01f, value); }

    // --- SZERVER SZINKRON ÁLLAPOTOK ---
    [SyncVar] public Team team;                  // Friendly / Enemy
    [SyncVar] public CardRole role = CardRole.Defender;  // Defender / Attacker

    // Grid-paraméterek (szerver tölti initkor)
    [SyncVar] private int fixedRowIndex = -1;  // mindig ezen a Z-soron marad
    [SyncVar] private float tileSize = 1.0f;
    [SyncVar] private float groundY = 0.0f;
    [SyncVar] private int gridWidth = 9;   // X oszlopok száma
    [SyncVar] private float originX = 0f;   // grid világ origója X-ben
    [SyncVar] private float rowZ = 0f;   // sor világ Z-je (NEM index*tileSize!)

    // Belső állapot (szerver használja)
    private Vector3 targetPosition;       // aktuális cél (X változik, Y/Z fix)
    private bool hasTarget = false;

    /// <summary>
    /// Ezt a GridManager hívja SPawn után, SZERVEREN.
    /// startX: a kezdő X (rács közép), sorIndex: Z sor index, groundY: tile teteje.
    /// </summary>
    // ÚJ mezők:
    [Server]
    public void ServerInit(Team t, CardRole r, int rowIndex, float gridTileSize,
                           float spawnY, int gridW, float startXWorld,
                           float originXWorld, float rowWorldZ)
    {
        team = t;
        role = r;
        fixedRowIndex = Mathf.Max(0, rowIndex);
        tileSize = Mathf.Max(0.01f, gridTileSize);
        groundY = spawnY;        // emelt Y-t tároljuk
        gridWidth = Mathf.Max(1, gridW);

        originX = originXWorld;        // <— ÚJ
        rowZ = rowWorldZ;           // <— ÚJ

        // Kezdeti pozíció: pontosan arra a cellára, amit a Player kiszámolt
        transform.position = new Vector3(SnapX(startXWorld), groundY, rowZ);
        transform.forward = (team == Team.Friendly) ? Vector3.right : Vector3.left;

        // cél beállítás mint eddig, csak origóval számolunk
        int curCol = GetColX();
        if (role == CardRole.Attacker)
        {
            int dir = (team == Team.Friendly) ? +1 : -1;
            int next = Mathf.Clamp(curCol + dir, 0, gridWidth - 1);
            SetTargetXColumn(next);
        }
        else
        {
            SetTargetXColumn(curCol);
        }
    }

    // ---------- MOZGÁS LOGIKA (SZERVER) ----------
    [ServerCallback]
    private void Update()
    {
        // Z és Y lock minden framen (sávban marad és a tile tetején)
        Vector3 p = transform.position;
        p.y = groundY;
        p.z = rowZ;
        transform.position = p;

        if (role != CardRole.Attacker || !hasTarget)
            return; // Defender vagy nincs cél → nem mozog

        float step = MoveSpeed * Time.deltaTime;

        // csak X-ben mozgunk a target felé, Y/Z fix
        Vector3 tgt = new Vector3(targetPosition.x, groundY, rowZ);
        transform.position = Vector3.MoveTowards(transform.position, tgt, step);

        // ha elértük a target X-et → számoljuk ki a következő rácsot
        if (Mathf.Abs(transform.position.x - tgt.x) <= 0.0001f)
        {
            int curCol = GetColX();
            int dir = (team == Team.Friendly) ? +1 : -1;
            int next = Mathf.Clamp(curCol + dir, 0, gridWidth - 1);

            if (next == curCol)
            {
                // szélen vagyunk → megállunk (tovább már nem mehet ki a pályáról)
                hasTarget = false;
                return;
            }

            SetTargetXColumn(next);
        }
    }

    // ---------- KOMPAT: célpont beállítás kívülről ----------
    /// <summary>
    /// Ezt hívhatja a Player/DragDrop régi kódja. Csak Attackerre érvényes.
    /// A cél X koordinátát a pálya határai közé clampeljük, Y/Z fix marad.
    /// </summary>
    [Server]
    public void SetTargetPosition(Vector3 newTarget)
    {
        if (role != CardRole.Attacker)
            return; // Defender nem kap célpontot

        float clampedX = ClampXToGrid(newTarget.x);
        targetPosition = new Vector3(clampedX, groundY, rowZ);
        hasTarget = true;
    }

    // ---------- SEGÉDEK (SZERVER) ----------
    [Server] private int GetColX() => Mathf.Clamp(Mathf.RoundToInt((transform.position.x - originX) / tileSize), 0, gridWidth - 1);
    [Server] private float SnapX(float x) => originX + Mathf.Round((x - originX) / tileSize) * tileSize;

    [Server]
    private float ClampXToGrid(float x)
    {
        float minX = originX;
        float maxX = originX + (gridWidth - 1) * tileSize;
        return Mathf.Clamp(x, minX, maxX);
    }

    [Server]
    private void SetTargetXColumn(int col)
    {
        col = Mathf.Clamp(col, 0, gridWidth - 1);
        float x = originX + col * tileSize;
        targetPosition = new Vector3(x, groundY, rowZ);
        hasTarget = true;
    }

    [Server]
    public void ForceRowAndGround(float rowZ, float groundY)
    {
        this.rowZ = rowZ;
        this.groundY = groundY;

        var p = transform.position;
        p.z = rowZ;
        p.y = groundY;
        transform.position = p;
    }

}

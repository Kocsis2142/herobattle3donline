using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Kliens oldali drag/drop:
/// - Drag közben NEM snapelünk, csak a tile tetején "úszik" a ghost.
/// - Defender: csak a célzott csempe highlight.
/// - Attacker: az egész sort highlightolja.
/// - Dropkor (col,row) megy fel a szervernek.
/// </summary>
public class DragDropManager : MonoBehaviour
{
    [Header("Világ kamera")]
    [SerializeField] Camera worldCamera;      
    [SerializeField] float groundY = 0f;      

    [Header("Ghost (vizuális)")]
    [SerializeField] GameObject ghostPrefab;  
    [Range(0, 1)] [SerializeField] float ghostAlpha = 0.35f;

    GameObject ghost;
    UnitCard activeCard;

    // highlightolt tile-ok
    List<TileVisual> currentHighlights = new List<TileVisual>();

    GridManager gm;

    Team _localTeam = Team.Friendly;
    public void SetLocalTeam(Team t) => _localTeam = t;

    void EnsureRefs()
    {
        if (!worldCamera) worldCamera = Camera.main;
        if (!gm) gm = GridManager.Instance;
    }

    // ==== UI kártya hívja ezeket (ObjectCard.cs-ből) ====

    public void BeginCardDrag(UnitCard card, RectTransform _rect, PointerEventData e)
    {
        EnsureRefs();
        if (!gm) { Debug.LogWarning("[DragDrop] Nincs GridManager a jelenetben."); return; }
        if (card == null || string.IsNullOrEmpty(card.unitName)) { Debug.LogWarning("[DragDrop] UnitCard hiányzik vagy unitName üres."); return; }
        if (!ghostPrefab) { Debug.LogWarning("[DragDrop] ghostPrefab nincs beállítva."); return; }

        activeCard = card;

        ghost = Instantiate(ghostPrefab);
        PrepareGhostVisual(ghost);
    }

    public void DragCard(UnitCard card, RectTransform _rect, PointerEventData e)
    {
        if (ghost == null) return;
        EnsureRefs();
        if (!gm || !worldCamera) return;

        if (gm.TryRayToGrid(worldCamera, e.position, out var tv, out var cell, out var hitPos))
        {
            // ghost pozícionálása
            float topY = gm.GetTileTopYAtWorldXZ(hitPos);

            float halfH = 0.5f;
            var gcol = ghost.GetComponent<Collider>();
            if (gcol != null) halfH = gcol.bounds.extents.y;
            else
            {
                var gr = ghost.GetComponentInChildren<Renderer>(true);
                if (gr != null) halfH = gr.bounds.extents.y;
            }
            if (halfH < 0.25f) halfH = 0.25f;

            ghost.transform.position = new Vector3(hitPos.x, topY + halfH + 0.001f, hitPos.z);

            // highlight frissítés
            ClearHighlights();

            if (activeCard.role == CardRole.Defender)
            {
                bool isAllowed = IsCellAllowedForLocalDefender(cell, gm);
                tv?.SetHighlight(isAllowed ? gm.tileAllowedColor : gm.tileDeniedColor);
                if (tv != null) currentHighlights.Add(tv);
            }
            else if (activeCard.role == CardRole.Attacker)
            {
                // egész sort highlightoljuk
                if (cell.y >= 0 && cell.y < gm.height)
                {
                    for (int x = 0; x < gm.width; x++)
                    {
                        // összes TileVisual végigjárása, és az adott sor highlight
                        foreach (var tvRow in Object.FindObjectsByType<TileVisual>(FindObjectsSortMode.None))
                        {
                            if (tvRow.row == cell.y)
                            {
                                tvRow.SetHighlight(gm.tileAllowedColor);
                                currentHighlights.Add(tvRow);
                            }
                        }
                    }
                }
            }
            return;
        }

        // fallback síkra
        if (ScreenToWorldOnPlane(e.position, groundY, out var worldPos))
        {
            float topY = gm.GetTileTopYAtWorldXZ(worldPos);

            float halfH = 0.5f;
            var gcol = ghost.GetComponent<Collider>();
            if (gcol != null) halfH = gcol.bounds.extents.y;
            else
            {
                var gr = ghost.GetComponentInChildren<Renderer>(true);
                if (gr != null) halfH = gr.bounds.extents.y;
            }
            if (halfH < 0.25f) halfH = 0.25f;

            ghost.transform.position = new Vector3(worldPos.x, topY + halfH + 0.001f, worldPos.z);
        }
    }

    public void EndCardDrag(UnitCard card, RectTransform _rect, PointerEventData e)
    {
        EnsureRefs();

        ClearHighlights();

        if (ghost == null) { Cleanup(); activeCard = null; return; }
        if (!gm || !worldCamera) { Cleanup(); activeCard = null; return; }

        if (!gm.TryRayToGrid(worldCamera, e.position, out var tv, out var cell, out _))
        {
            Cleanup(); activeCard = null; return;
        }

        var lp = NetworkClient.localPlayer;
        var player = lp ? lp.GetComponent<Player>() : null;
        if (!player) { Cleanup(); activeCard = null; return; }

        if (activeCard.role == CardRole.Defender)
        {
            if (!IsCellAllowedForLocalDefender(cell, gm))
            {
                Cleanup(); activeCard = null; return;
            }
            player.CmdSpawnDefender(cell.x, cell.y, activeCard.unitName);
        }
        else
        {
            // Attacker → csak a sort küldjük fel
            player.CmdSpawnAttacker(Mathf.Clamp(cell.y, 0, gm.height - 1), activeCard.unitName);
        }

        Cleanup();
        activeCard = null;
    }

    // ==== segédek ====

    bool ScreenToWorldOnPlane(Vector2 screenPos, float yLevel, out Vector3 world)
    {
        var cam = worldCamera ? worldCamera : Camera.main;
        var ray = cam.ScreenPointToRay(screenPos);
        var plane = new Plane(Vector3.up, new Vector3(0f, yLevel, 0f));
        if (plane.Raycast(ray, out float enter))
        {
            world = ray.GetPoint(enter);
            return true;
        }
        
        world = default;
        return false;
    }

    void PrepareGhostVisual(GameObject go)
    {
        go.layer = LayerMask.NameToLayer("Ignore Raycast");

        if (go.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        foreach (var c in go.GetComponentsInChildren<Collider>(true))
            c.isTrigger = true;

        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.material || !r.material.HasProperty("_Color")) continue;
            var col = Color.yellow; col.a = ghostAlpha;
            r.material.color = col;
        }
    }

    bool IsCellAllowedForLocalDefender(Vector2Int cell, GridManager gm)
    {
        if (_localTeam == Team.Friendly)
            return cell.x >= 0 && cell.x < gm.friendlySpawnCols;
        else
            return cell.x >= (gm.width - gm.enemySpawnCols) && cell.x < gm.width;
    }

    void ClearHighlights()
    {
        foreach (var th in currentHighlights)
            th?.ClearHighlight();
        currentHighlights.Clear();
    }

    void Cleanup()
    {
        ClearHighlights();
        if (ghost) Destroy(ghost);
        ghost = null;
    }
}

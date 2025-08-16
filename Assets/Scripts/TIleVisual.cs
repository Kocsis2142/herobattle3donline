using Mirror;
using UnityEngine;

/// <summary>
/// Minden tile-on fut. A szerver beállítja a cella indexet,
/// a kliens pedig innen olvassa ki (raycast után), így nincs eltérés a számolásban.
/// </summary>
public class TileVisual : NetworkBehaviour
{
    [SyncVar] public int col;
    [SyncVar] public int row;

    // highlighthoz
    [SerializeField] Renderer _rend;
    Color _base; 

  void Awake()
    {
        if (!_rend) _rend = GetComponentInChildren<Renderer>(true);
        _base = GetCurrentColor();
    }

      Color GetCurrentColor()
    {
        if (_rend == null || _rend.material == null) return Color.white;
        var m = _rend.material;
        if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
        if (m.HasProperty("_Color"))     return m.GetColor("_Color");
        return Color.white;
    }

    [Server]
    public void ServerInit(int c, int r)
    {
        col = c; row = r;
    }

       void SetMatColor(Color c)
    {
        if (!_rend || _rend.material == null) return;

        var mat = _rend.material;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        else if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
    }

    // Kliens-oldali vizuális highlight
    [Client] public void SetHighlight(Color c) => SetMatColor(c);
    [Client] public void ClearHighlight() => SetMatColor(_base);
 
}

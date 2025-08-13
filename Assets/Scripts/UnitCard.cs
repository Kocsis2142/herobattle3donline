using UnityEngine;
using UnityEngine.EventSystems;

public class UnitCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string unitName;    // Az egység prefab neve
    public int team;           // Csapat

    private Vector3 startPos;
    private Canvas canvas;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        startPos = rectTransform.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position += (Vector3)eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(eventData.position);
        worldPos.y = 0; // Grid magasság

        Vector3 snappedPos = GridManager.Instance.SnapToNearestTile(worldPos);

        // Meghívjuk a PlayerController szerver parancsát
        if (Mirror.NetworkClient.localPlayer != null)
        {
            var pc = Mirror.NetworkClient.localPlayer.GetComponent<PlayerController>();
            pc.CmdRequestSpawnSoldier(snappedPos, team, unitName);
        }
        
        rectTransform.position = startPos; // visszaállítjuk a kártyát
    }
}

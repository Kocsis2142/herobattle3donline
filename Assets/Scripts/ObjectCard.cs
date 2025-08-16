using UnityEngine;
using UnityEngine.EventSystems;

public class ObjectCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public DragDropManager manager;  
    public UnitCard card;            // Inspectorban állítod be

    private RectTransform rect;

    void Awake()
    {
        rect = transform as RectTransform;
        if (manager == null) manager = FindFirstObjectByType<DragDropManager>();
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (card == null)
        {
            Debug.LogWarning("[ObjectCard] UnitCard reference is missing on this UI card.");
            return;
        }

        manager?.BeginCardDrag(card, rect, eventData);
    }



    public void OnDrag(PointerEventData eventData) => manager?.DragCard(card, rect, eventData);
    public void OnEndDrag(PointerEventData eventData)   => manager?.EndCardDrag(card, rect, eventData);
}

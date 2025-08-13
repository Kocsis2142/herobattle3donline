using Mirror;
using UnityEngine;

public class SoldierController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    private Vector3 targetPosition;

    public override void OnStartServer()
    {
        // Induláskor a saját pozíciójára állítjuk
        targetPosition = transform.position;
    }

    [Server]
    public void SetTargetPosition(Vector3 newTarget)
    {
        targetPosition = newTarget;
    }

    [ServerCallback]
    private void Update()
    {
        if (Vector3.Distance(transform.position, targetPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

          //  Vector3 direction = (targetPosition - transform.position).normalized;
          //  if (direction != Vector3.zero)
           //     transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}

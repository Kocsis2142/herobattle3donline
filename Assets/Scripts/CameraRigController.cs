using UnityEngine;

public class CameraRigController : MonoBehaviour
{
    [Header("Beállítások")]
    public Transform cam;         // húzd ide a Main Camera-t
    public float pitch = 45f;     // dőlés
    public float yawFriendly = 45f;
    public float yawEnemyOffset = 180f;

    [Header("Távolság (perspective) vagy ortho 'zoom'")]
    public float distance = 10f;  // perspective esetén a cam local Z-je lesz -distance

    void Start()
    {
        // Pálya közepe
        var gm = GridManager.Instance;
        if (gm) transform.position = gm.BoardCenter;

        // A kamera ne forogjon: csak a Pivot
        if (cam)
        {
            cam.localRotation = Quaternion.identity;
            cam.localPosition = new Vector3(0f, 0f, -distance);
        }

        // Alap: friendly nézet
        SetTeam(Team.Friendly);
    }

    public void SetTeam(Team team)
    {
        float yaw = (team == Team.Friendly) ? yawFriendly : yawFriendly + yawEnemyOffset;
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}

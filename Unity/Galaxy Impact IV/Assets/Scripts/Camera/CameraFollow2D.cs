using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [Header("Objetos")]
    public Transform player;
    public SpriteRenderer background;  // SpriteRenderer del fondo

    [Header("Movimiento")]
    public float smoothSpeed = 5f;

    private Camera cam;
    private float halfHeight;
    private float halfWidth;
    private Vector2 minBounds;
    private Vector2 maxBounds;

    void Start()
    {
        cam = GetComponent<Camera>();
        UpdateBounds();
    }

    void LateUpdate()
    {
        if (player == null || background == null)
            return;

        UpdateBounds(); // Recalcula por si el fondo cambia de tamaño

        Vector3 targetPos = new Vector3(player.position.x, player.position.y, transform.position.z);

        float clampX = Mathf.Clamp(targetPos.x, minBounds.x + halfWidth, maxBounds.x - halfWidth);
        float clampY = Mathf.Clamp(targetPos.y, minBounds.y + halfHeight, maxBounds.y - halfHeight);

        Vector3 smoothPos = Vector3.Lerp(transform.position, new Vector3(clampX, clampY, transform.position.z), smoothSpeed * Time.deltaTime);
        transform.position = smoothPos;
    }

    void UpdateBounds()
    {
        halfHeight = cam.orthographicSize;
        halfWidth = halfHeight * cam.aspect;

        Bounds bgBounds = background.bounds; // Usa bounds reales en el mundo
        minBounds = bgBounds.min;
        maxBounds = bgBounds.max;
    }

    void OnDrawGizmosSelected()
    {
        if (background != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(background.bounds.center, background.bounds.size);
        }
    }
}
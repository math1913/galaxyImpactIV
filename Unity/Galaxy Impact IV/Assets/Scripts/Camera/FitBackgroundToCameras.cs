using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FitBackgroundToCamera : MonoBehaviour
{
    [Range(1f, 2f)] public float extraScale = 1.05f;
    public Vector2 offset = Vector2.zero; // nuevo campo para mover el fondo

    void Start()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        float worldScreenHeight = Camera.main.orthographicSize * 2f;
        float worldScreenWidth = worldScreenHeight / Screen.height * Screen.width;

        float scaleX = worldScreenWidth / sr.sprite.bounds.size.x;
        float scaleY = worldScreenHeight / sr.sprite.bounds.size.y;

        float scale = Mathf.Max(scaleX, scaleY) * extraScale;

        transform.localScale = new Vector3(scale, scale, 1);

        // --- Compensar posición ---
        Vector3 camPos = Camera.main.transform.position;
        transform.position = new Vector3(camPos.x + offset.x, camPos.y + offset.y, transform.position.z);
    }
}
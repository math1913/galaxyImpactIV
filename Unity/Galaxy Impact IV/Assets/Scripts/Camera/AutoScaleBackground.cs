using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class AutoScaleBackground : MonoBehaviour
{
    [Header("Escalado respecto a la cámara")]
    [Range(1f, 3f)] public float sizeMultiplier = 1.5f; // qué tan grande será respecto al área visible de la cámara
    public Camera cam; // si no se asigna, usa la principal

    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (cam == null) cam = Camera.main;

        AjustarEscala();
    }

    void AjustarEscala()
    {
        if (sr.sprite == null) return;

        float worldScreenHeight = cam.orthographicSize * 2f;
        float worldScreenWidth = worldScreenHeight / Screen.height * Screen.width;

        float scaleX = worldScreenWidth / sr.sprite.bounds.size.x;
        float scaleY = worldScreenHeight / sr.sprite.bounds.size.y;

        // Escala base para cubrir toda la cámara
        float scale = Mathf.Max(scaleX, scaleY);

        // Aplica el multiplicador adicional (hace el fondo más grande)
        scale *= sizeMultiplier;

        transform.localScale = new Vector3(scale, scale, 1);
    }
}
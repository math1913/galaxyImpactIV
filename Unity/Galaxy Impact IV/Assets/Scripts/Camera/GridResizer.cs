using UnityEngine;
using Pathfinding;

public class GridResizer : MonoBehaviour
{
    public AstarPath astar;
    public SpriteRenderer background; // o el objeto que se escala
    public float nodeSize = 0.5f;

    void Start()
    {
        // Esperamos un frame por si el fondo se ajusta en Awake()
        StartCoroutine(RescanNextFrame());
    }

    System.Collections.IEnumerator RescanNextFrame()
    {
        yield return null;

        var gg = astar.data.gridGraph;
        if (gg != null && background != null)
        {
            // Tomamos el tamaño real del sprite
            float width = background.bounds.size.x;
            float depth = background.bounds.size.y;

            gg.center = background.bounds.center;
            gg.width = Mathf.RoundToInt(width / nodeSize);
            gg.depth = Mathf.RoundToInt(depth / nodeSize);
            gg.nodeSize = nodeSize;

            astar.Scan(); // recalcula el grid
        }
    }
}

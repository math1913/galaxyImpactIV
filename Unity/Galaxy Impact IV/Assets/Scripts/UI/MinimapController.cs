using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] RectTransform minimapRect;
    [SerializeField] RectTransform dotsParent;
    [SerializeField] Image playerDotPrefab;
    [SerializeField] Image enemyDotPrefab;

    [Header("Map Bounds (Background SpriteRenderer)")]
    [SerializeField] SpriteRenderer background; // <-- tu Background

    [Header("Targets")]
    [SerializeField] Transform player;
    [SerializeField, Range(0f, 0.49f)]
    float edgeMarginNormalized = 0.05f; // 5% de margen por cada lado


    Image playerDot;
    readonly Dictionary<Transform, Image> enemyDots = new();

    void Awake()
    {
        if (!dotsParent) dotsParent = minimapRect;
        playerDot = Instantiate(playerDotPrefab, dotsParent);
        playerDot.raycastTarget = false;
    }

    void LateUpdate()
    {
        if (!background) return;

        if (player) UpdateDot(playerDot.rectTransform, player.position);

        foreach (var kv in enemyDots)
        {
            if (!kv.Key) continue;
            UpdateDot(kv.Value.rectTransform, kv.Key.position);
        }
    }

    public void RegisterEnemy(Transform enemy)
    {
        if (!enemy || enemyDots.ContainsKey(enemy)) return;
        var dot = Instantiate(enemyDotPrefab, dotsParent);
        dot.raycastTarget = false;
        enemyDots.Add(enemy, dot);
    }

    public void UnregisterEnemy(Transform enemy)
    {
        if (!enemy) return;
        if (enemyDots.TryGetValue(enemy, out var dot))
        {
            if (dot) Destroy(dot.gameObject);
            enemyDots.Remove(enemy);
        }
    }

    void UpdateDot(RectTransform dot, Vector3 worldPos)
    {
        Bounds b = background.bounds;

        float nx = Mathf.InverseLerp(b.min.x, b.max.x, worldPos.x);
        float ny = Mathf.InverseLerp(b.min.y, b.max.y, worldPos.y);

        float m = edgeMarginNormalized;

        // "Dentro" solo si está lejos del borde por al menos el margen
        bool inside = (nx >= m && nx <= 1f - m && ny >= m && ny <= 1f - m);

        dot.gameObject.SetActive(inside);
        if (!inside) return;

        Vector2 size = minimapRect.rect.size;
        dot.anchoredPosition = new Vector2((nx - 0.5f) * size.x, (ny - 0.5f) * size.y);
    }


}

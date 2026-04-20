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
    [SerializeField] Image allyDotPrefab;

    [Header("Map Bounds (Background SpriteRenderer)")]
    [SerializeField] SpriteRenderer background;

    [Header("Targets")]
    [SerializeField] Transform player;
    [SerializeField, Range(0f, 0.49f)]
    float edgeMarginNormalized = 0.05f;

    Image playerDot;
    readonly Dictionary<Transform, Image> enemyDots = new();
    readonly List<Transform> staleEnemyEntries = new();
    readonly Dictionary<Transform, Image> allyDots = new();
    readonly List<Transform> staleAllyEntries = new();

    void Awake()
    {
        if (!dotsParent)
            dotsParent = minimapRect;

        playerDot = Instantiate(playerDotPrefab, dotsParent);
        playerDot.raycastTarget = false;
    }

    private void OnEnable()
    {
        EnemyController.OnEnemySpawned += HandleEnemySpawned;
        EnemyController.OnEnemyDespawned += HandleEnemyDespawned;
        RefreshEnemiesFromScene();
    }

    private void OnDisable()
    {
        EnemyController.OnEnemySpawned -= HandleEnemySpawned;
        EnemyController.OnEnemyDespawned -= HandleEnemyDespawned;
    }

    void LateUpdate()
    {
        if (!background)
            return;

        if (player)
            UpdateDot(playerDot.rectTransform, player.position);

        staleEnemyEntries.Clear();
        foreach (var kv in enemyDots)
        {
            if (!kv.Key)
            {
                staleEnemyEntries.Add(kv.Key);
                continue;
            }

            UpdateDot(kv.Value.rectTransform, kv.Key.position);
        }

        foreach (var enemy in staleEnemyEntries)
            UnregisterEnemy(enemy);

        UpdateAllies();
    }

    public void RegisterEnemy(Transform enemy)
    {
        if (!enemy || enemyDots.ContainsKey(enemy))
            return;

        var dot = Instantiate(enemyDotPrefab, dotsParent);
        dot.raycastTarget = false;
        enemyDots.Add(enemy, dot);
    }

    public void UnregisterEnemy(Transform enemy)
    {
        if (object.ReferenceEquals(enemy, null))
            return;

        if (!enemyDots.TryGetValue(enemy, out var registeredDot))
            return;

        if (registeredDot)
            Destroy(registeredDot.gameObject);

        enemyDots.Remove(enemy);
    }

    public void RegisterAlly(Transform ally)
    {
        if (!ally || !allyDotPrefab || allyDots.ContainsKey(ally))
            return;

        var dot = Instantiate(allyDotPrefab, dotsParent);
        dot.raycastTarget = false;
        allyDots.Add(ally, dot);
    }

    public void UnregisterAlly(Transform ally)
    {
        if (object.ReferenceEquals(ally, null))
            return;

        if (!allyDots.TryGetValue(ally, out var registeredDot))
            return;

        if (registeredDot)
            Destroy(registeredDot.gameObject);

        allyDots.Remove(ally);
    }

    void UpdateDot(RectTransform dot, Vector3 worldPos)
    {
        Bounds b = background.bounds;

        float nx = Mathf.InverseLerp(b.min.x, b.max.x, worldPos.x);
        float ny = Mathf.InverseLerp(b.min.y, b.max.y, worldPos.y);

        float m = edgeMarginNormalized;
        bool inside = (nx >= m && nx <= 1f - m && ny >= m && ny <= 1f - m);

        dot.gameObject.SetActive(inside);
        if (!inside)
            return;

        Vector2 size = minimapRect.rect.size;
        dot.anchoredPosition = new Vector2((nx - 0.5f) * size.x, (ny - 0.5f) * size.y);
    }

    public void BindPlayer(Transform target)
    {
        player = target;
        RefreshEnemiesFromScene();
    }

    private void HandleEnemySpawned(Transform enemy)
    {
        RegisterEnemy(enemy);
    }

    private void HandleEnemyDespawned(Transform enemy)
    {
        UnregisterEnemy(enemy);
    }

    private void RefreshEnemiesFromScene()
    {
        foreach (var enemy in FindObjectsOfType<EnemyController>(true))
            RegisterEnemy(enemy.transform);
    }

    private void UpdateAllies()
    {
        staleAllyEntries.Clear();
        foreach (var kv in allyDots)
            staleAllyEntries.Add(kv.Key);

        IReadOnlyList<LanPlayerAvatar> activePlayers = LanPlayerAvatar.ActivePlayers;
        for (int i = 0; i < activePlayers.Count; i++)
        {
            LanPlayerAvatar lanPlayer = activePlayers[i];
            if (lanPlayer == null || !lanPlayer.IsAlive)
                continue;

            Transform ally = lanPlayer.transform;
            if (!ally || ally == player)
                continue;

            RegisterAlly(ally);

            if (allyDots.TryGetValue(ally, out var allyDot) && allyDot)
                UpdateDot(allyDot.rectTransform, ally.position);

            staleAllyEntries.Remove(ally);
        }

        foreach (var ally in staleAllyEntries)
            UnregisterAlly(ally);
    }
}

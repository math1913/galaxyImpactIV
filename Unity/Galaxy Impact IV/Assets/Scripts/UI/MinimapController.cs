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
    [SerializeField] Color teammateDotColor = new Color(0.2f, 0.55f, 1f, 0.9f);

    [Header("Map Bounds (Background SpriteRenderer)")]
    [SerializeField] SpriteRenderer background;

    [Header("Targets")]
    [SerializeField] Transform player;
    [SerializeField, Range(0f, 0.49f)]
    float edgeMarginNormalized = 0.05f;

    Image playerDot;
    readonly Dictionary<Transform, Image> enemyDots = new();
    readonly Dictionary<Transform, Image> teammateDots = new();
    readonly List<Transform> staleEnemyEntries = new();
    readonly List<Transform> staleTeammateEntries = new();

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
        ClearTeammateDots();
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

        UpdateTeammateDots();
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
        UpdateTeammateDots();
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

    private void UpdateTeammateDots()
    {
        if (!LanRuntime.IsActive)
        {
            ClearTeammateDots();
            return;
        }

        staleTeammateEntries.Clear();
        foreach (var entry in teammateDots)
            staleTeammateEntries.Add(entry.Key);

        foreach (LanPlayerAvatar avatar in LanPlayerAvatar.ActivePlayers)
        {
            if (avatar == null || !avatar.IsAlive)
                continue;

            Transform teammate = avatar.transform;
            if (teammate == null || teammate == player)
                continue;

            if (!teammateDots.TryGetValue(teammate, out Image dot))
            {
                dot = Instantiate(playerDotPrefab, dotsParent);
                dot.raycastTarget = false;
                dot.color = teammateDotColor;
                teammateDots.Add(teammate, dot);
            }

            staleTeammateEntries.Remove(teammate);
            UpdateDot(dot.rectTransform, teammate.position);
        }

        foreach (Transform stale in staleTeammateEntries)
            UnregisterTeammate(stale);
    }

    private void UnregisterTeammate(Transform teammate)
    {
        if (object.ReferenceEquals(teammate, null))
            return;

        if (!teammateDots.TryGetValue(teammate, out Image dot))
            return;

        if (dot)
            Destroy(dot.gameObject);

        teammateDots.Remove(teammate);
    }

    private void ClearTeammateDots()
    {
        foreach (Image dot in teammateDots.Values)
        {
            if (dot)
                Destroy(dot.gameObject);
        }

        teammateDots.Clear();
        staleTeammateEntries.Clear();
    }
}

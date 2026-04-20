using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapController : MonoBehaviour
{
    private sealed class AllyMarker
    {
        public readonly Image aliveDot;
        public readonly Text deadCross;

        public AllyMarker(Image aliveDot, Text deadCross)
        {
            this.aliveDot = aliveDot;
            this.deadCross = deadCross;
        }
    }

    [Header("UI")]
    [SerializeField] RectTransform minimapRect;
    [SerializeField] RectTransform dotsParent;
    [SerializeField] Image playerDotPrefab;
    [SerializeField] Image enemyDotPrefab;
    [SerializeField] Image allyDotPrefab;

    [Header("Allies")]
    [SerializeField] Color allyDotColor = new Color(0.25f, 0.82f, 1f, 0.95f);
    [SerializeField] Color deadAllyCrossColor = new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField, Min(8)] int deadAllyCrossFontSize = 16;
    [SerializeField] string deadAllyCrossText = "X";

    [Header("Map Bounds (Background SpriteRenderer)")]
    [SerializeField] SpriteRenderer background;

    [Header("Targets")]
    [SerializeField] Transform player;
    [SerializeField, Range(0f, 0.49f)]
    float edgeMarginNormalized = 0.05f;

    Image playerDot;
    readonly Dictionary<Transform, Image> enemyDots = new();
    readonly List<Transform> staleEnemyEntries = new();
    readonly Dictionary<Transform, AllyMarker> allyDots = new();
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
        RefreshAlliesFromLan();
    }

    private void OnDisable()
    {
        EnemyController.OnEnemySpawned -= HandleEnemySpawned;
        EnemyController.OnEnemyDespawned -= HandleEnemyDespawned;
        ClearAllies();
    }

    void LateUpdate()
    {
        if (!background)
            return;

        if (player)
            UpdateMarker(playerDot.rectTransform, playerDot.gameObject, player.position);

        staleEnemyEntries.Clear();
        foreach (var kv in enemyDots)
        {
            if (!kv.Key)
            {
                staleEnemyEntries.Add(kv.Key);
                continue;
            }

            UpdateMarker(kv.Value.rectTransform, kv.Value.gameObject, kv.Key.position);
        }

        foreach (var enemy in staleEnemyEntries)
            UnregisterEnemy(enemy);

        RefreshAlliesFromLan();
        staleAllyEntries.Clear();
        foreach (var kv in allyDots)
        {
            Transform allyTransform = kv.Key;
            if (!allyTransform)
            {
                staleAllyEntries.Add(kv.Key);
                continue;
            }

            LanPlayerAvatar avatar = allyTransform.GetComponent<LanPlayerAvatar>();
            if (avatar != null && (avatar.IsOwner || allyTransform == player))
            {
                staleAllyEntries.Add(kv.Key);
                continue;
            }

            bool alive = avatar == null || avatar.IsAlive;
            if (alive)
            {
                kv.Value.deadCross.gameObject.SetActive(false);
                UpdateMarker(kv.Value.aliveDot.rectTransform, kv.Value.aliveDot.gameObject, allyTransform.position);
            }
            else
            {
                kv.Value.aliveDot.gameObject.SetActive(false);
                UpdateMarker(kv.Value.deadCross.rectTransform, kv.Value.deadCross.gameObject, allyTransform.position);
            }
        }

        foreach (var ally in staleAllyEntries)
            UnregisterAlly(ally);
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

    private void RegisterAlly(Transform ally)
    {
        if (!ally || allyDots.ContainsKey(ally))
            return;

        Image sourceDotPrefab = allyDotPrefab ? allyDotPrefab : playerDotPrefab;
        if (!sourceDotPrefab)
            return;

        Image aliveDot = Instantiate(sourceDotPrefab, dotsParent);
        aliveDot.raycastTarget = false;
        if (!allyDotPrefab)
            aliveDot.color = allyDotColor;

        Text deadCross = CreateDeadAllyCross();
        deadCross.gameObject.SetActive(false);

        allyDots.Add(ally, new AllyMarker(aliveDot, deadCross));
    }

    private void UnregisterAlly(Transform ally)
    {
        if (object.ReferenceEquals(ally, null))
            return;

        if (!allyDots.TryGetValue(ally, out AllyMarker marker))
            return;

        if (marker.aliveDot)
            Destroy(marker.aliveDot.gameObject);
        if (marker.deadCross)
            Destroy(marker.deadCross.gameObject);

        allyDots.Remove(ally);
    }

    private void RefreshAlliesFromLan()
    {
        if (!LanRuntime.IsActive)
        {
            if (allyDots.Count > 0)
                ClearAllies();
            return;
        }

        foreach (LanPlayerAvatar avatar in LanPlayerAvatar.ActivePlayers)
        {
            if (avatar == null)
                continue;
            if (avatar.IsOwner || avatar.transform == player)
                continue;

            RegisterAlly(avatar.transform);
        }
    }

    private void ClearAllies()
    {
        foreach (var kv in allyDots)
        {
            AllyMarker marker = kv.Value;
            if (marker.aliveDot)
                Destroy(marker.aliveDot.gameObject);
            if (marker.deadCross)
                Destroy(marker.deadCross.gameObject);
        }

        allyDots.Clear();
    }

    private Text CreateDeadAllyCross()
    {
        var markerObject = new GameObject("AllyDeadCross", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        markerObject.transform.SetParent(dotsParent, false);

        var deadCross = markerObject.GetComponent<Text>();
        deadCross.raycastTarget = false;
        deadCross.alignment = TextAnchor.MiddleCenter;
        deadCross.fontSize = deadAllyCrossFontSize;
        deadCross.fontStyle = FontStyle.Bold;
        deadCross.color = deadAllyCrossColor;
        deadCross.horizontalOverflow = HorizontalWrapMode.Overflow;
        deadCross.verticalOverflow = VerticalWrapMode.Overflow;
        deadCross.text = string.IsNullOrWhiteSpace(deadAllyCrossText) ? "X" : deadAllyCrossText;
        deadCross.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        RectTransform markerRect = markerObject.GetComponent<RectTransform>();
        markerRect.sizeDelta = new Vector2(16f, 16f);
        markerRect.anchorMin = new Vector2(0.5f, 0.5f);
        markerRect.anchorMax = new Vector2(0.5f, 0.5f);
        markerRect.pivot = new Vector2(0.5f, 0.5f);

        return deadCross;
    }

    private void UpdateMarker(RectTransform marker, GameObject markerObject, Vector3 worldPos)
    {
        Bounds b = background.bounds;

        float nx = Mathf.InverseLerp(b.min.x, b.max.x, worldPos.x);
        float ny = Mathf.InverseLerp(b.min.y, b.max.y, worldPos.y);

        float m = edgeMarginNormalized;
        bool inside = (nx >= m && nx <= 1f - m && ny >= m && ny <= 1f - m);

        markerObject.SetActive(inside);
        if (!inside)
            return;

        Vector2 size = minimapRect.rect.size;
        marker.anchoredPosition = new Vector2((nx - 0.5f) * size.x, (ny - 0.5f) * size.y);
    }

    public void BindPlayer(Transform target)
    {
        player = target;
        RefreshEnemiesFromScene();
        RefreshAlliesFromLan();
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
}

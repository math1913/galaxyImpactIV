using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LanKillsTableHUD : MonoBehaviour
{
    private const string GameplaySceneName = "GameScene";
    private const string StatsRootName = "Stats";
    private const string RuntimeRootName = "StatsRuntime";
    private const string RowsRootName = "StatsRows";
    private const string TitleName = "StatsTitle";
    private const string TitleValue = "STATS";
    private const string LegacyGeneratedRootName = "GeneratedStatsContent";
    private const string LegacyGeneratedRootName2 = "LanStatsGeneratedContent";

    private const bool AutoResizeStatsRoot = true;
    private const bool ForceStatsRootScale = true;
    private const float StatsRootScale = 1f;

    private const float RootPadding = 8f;
    private const float RootSpacing = 2f;
    private const float TitleHeight = 30f;
    private const float TitleFontSize = 26f;
    private const float RowsSpacing = 2f;
    private const float RowHeight = 30f;
    private const float RowFontSize = 22f;
    private const int RowPaddingHorizontal = 10;
    private const int RowPaddingVertical = 2;
    private const float KillsColumnWidth = 72f;

    private static bool sceneHookInstalled;
    private static LanKillsTableHUD instance;

    private readonly List<LanLiveStatsEntry> sortedEntries = new List<LanLiveStatsEntry>();
    private readonly List<ulong> staleClientIds = new List<ulong>();
    private readonly Dictionary<ulong, RowView> rowsByClientId = new Dictionary<ulong, RowView>();

    private RectTransform statsRoot;
    private RectTransform runtimeRoot;
    private RectTransform rowsRoot;
    private VerticalLayoutGroup rowsLayout;
    private TextMeshProUGUI titleLabel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        sceneHookInstalled = false;
        instance = null;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallSceneHook()
    {
        if (!sceneHookInstalled)
        {
            sceneHookInstalled = true;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        TryAttachForScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryAttachForScene(scene);
    }

    private static void TryAttachForScene(Scene scene)
    {
        if (scene.name != GameplaySceneName)
            return;

        RectTransform root = FindStatsRoot(scene);
        if (root == null)
            return;

        if (!LanRuntime.IsActive)
        {
            root.gameObject.SetActive(false);
            return;
        }

        root.gameObject.SetActive(true);
        LanKillsTableHUD hud = root.GetComponent<LanKillsTableHUD>();
        if (hud == null)
            root.gameObject.AddComponent<LanKillsTableHUD>();
        else
            hud.enabled = true;
    }

    private static RectTransform FindStatsRoot(Scene scene)
    {
        foreach (RectTransform rect in Resources.FindObjectsOfTypeAll<RectTransform>())
        {
            if (rect == null || rect.gameObject.scene != scene)
                continue;

            if (string.Equals(rect.gameObject.name, StatsRootName, StringComparison.OrdinalIgnoreCase))
                return rect;
        }

        return null;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
        statsRoot = GetComponent<RectTransform>();
        BuildFromScratch();
    }

    private void OnEnable()
    {
        LanPlayerAvatar.OnLiveStatsChanged += HandleLiveStatsChanged;
        HandleLiveStatsChanged(LanPlayerAvatar.LiveStats);
    }

    private void OnDisable()
    {
        LanPlayerAvatar.OnLiveStatsChanged -= HandleLiveStatsChanged;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void BuildFromScratch()
    {
        if (statsRoot == null)
            return;

        if (ForceStatsRootScale)
            statsRoot.localScale = new Vector3(StatsRootScale, StatsRootScale, StatsRootScale);

        RemoveLegacyGeneratedRoots();

        runtimeRoot = EnsureRectChild(statsRoot, RuntimeRootName);
        runtimeRoot.anchorMin = new Vector2(0f, 0f);
        runtimeRoot.anchorMax = new Vector2(1f, 1f);
        runtimeRoot.offsetMin = new Vector2(RootPadding, RootPadding);
        runtimeRoot.offsetMax = new Vector2(-RootPadding, -RootPadding);

        VerticalLayoutGroup rootLayout = runtimeRoot.GetComponent<VerticalLayoutGroup>();
        if (rootLayout == null)
            rootLayout = runtimeRoot.gameObject.AddComponent<VerticalLayoutGroup>();

        rootLayout.padding = new RectOffset(0, 0, 0, 0);
        rootLayout.spacing = RootSpacing;
        rootLayout.childAlignment = TextAnchor.UpperCenter;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = false;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        titleLabel = EnsureTextChild(runtimeRoot, TitleName);
        titleLabel.text = TitleValue;
        titleLabel.enableAutoSizing = false;
        titleLabel.fontSize = TitleFontSize;
        titleLabel.fontStyle = FontStyles.Bold;
        titleLabel.alignment = TextAlignmentOptions.Center;
        titleLabel.color = new Color(1f, 0.88f, 0.35f, 1f);
        titleLabel.raycastTarget = false;

        LayoutElement titleLayout = titleLabel.GetComponent<LayoutElement>();
        if (titleLayout == null)
            titleLayout = titleLabel.gameObject.AddComponent<LayoutElement>();

        titleLayout.minHeight = TitleHeight;
        titleLayout.preferredHeight = TitleHeight;
        titleLayout.flexibleHeight = 0f;

        rowsRoot = EnsureRectChild(runtimeRoot, RowsRootName);

        rowsLayout = rowsRoot.GetComponent<VerticalLayoutGroup>();
        if (rowsLayout == null)
            rowsLayout = rowsRoot.gameObject.AddComponent<VerticalLayoutGroup>();

        rowsLayout.padding = new RectOffset(0, 0, 0, 0);
        rowsLayout.spacing = RowsSpacing;
        rowsLayout.childAlignment = TextAnchor.UpperCenter;
        rowsLayout.childControlWidth = true;
        rowsLayout.childControlHeight = true;
        rowsLayout.childForceExpandWidth = true;
        rowsLayout.childForceExpandHeight = false;

        LayoutElement rowsLayoutElement = rowsRoot.GetComponent<LayoutElement>();
        if (rowsLayoutElement == null)
            rowsLayoutElement = rowsRoot.gameObject.AddComponent<LayoutElement>();

        rowsLayoutElement.flexibleHeight = 1f;
        rowsLayoutElement.minHeight = 0f;
        rowsLayoutElement.preferredHeight = 0f;

        UpdateRootHeight(0);
    }

    private void RemoveLegacyGeneratedRoots()
    {
        Transform oldA = statsRoot.Find(LegacyGeneratedRootName);
        if (oldA != null)
            Destroy(oldA.gameObject);

        Transform oldB = statsRoot.Find(LegacyGeneratedRootName2);
        if (oldB != null)
            Destroy(oldB.gameObject);
    }

    private void HandleLiveStatsChanged(IReadOnlyList<LanLiveStatsEntry> entries)
    {
        if (!LanRuntime.IsActive || runtimeRoot == null)
            return;

        sortedEntries.Clear();
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].isConnected)
                sortedEntries.Add(entries[i]);
        }

        sortedEntries.Sort(CompareEntries);
        RebuildRows(sortedEntries);
    }

    private void RebuildRows(IReadOnlyList<LanLiveStatsEntry> entries)
    {
        staleClientIds.Clear();
        foreach (ulong clientId in rowsByClientId.Keys)
            staleClientIds.Add(clientId);

        ulong localClientId = NetworkManager.Singleton != null
            ? NetworkManager.Singleton.LocalClientId
            : ulong.MaxValue;

        for (int i = 0; i < entries.Count; i++)
        {
            LanLiveStatsEntry entry = entries[i];
            RowView row = GetOrCreateRow(entry.clientId);
            row.root.transform.SetSiblingIndex(i);
            row.root.SetActive(true);

            staleClientIds.Remove(entry.clientId);

            string playerName = entry.playerName.ToString();
            if (string.IsNullOrWhiteSpace(playerName))
                playerName = $"Player {entry.clientId}";

            row.nameText.text = playerName;
            row.killsText.text = entry.killsThisRun.ToString();
            row.background.color = entry.clientId == localClientId
                ? new Color(0.22f, 0.42f, 0.72f, 0.55f)
                : new Color(1f, 1f, 1f, 0.08f);
        }

        for (int i = 0; i < staleClientIds.Count; i++)
            RemoveRow(staleClientIds[i]);

        UpdateRootHeight(entries.Count);
    }

    private RowView GetOrCreateRow(ulong clientId)
    {
        if (rowsByClientId.TryGetValue(clientId, out RowView existing))
            return existing;

        RowView created = CreateRow(clientId);
        rowsByClientId[clientId] = created;
        return created;
    }

    private void RemoveRow(ulong clientId)
    {
        if (!rowsByClientId.TryGetValue(clientId, out RowView row))
            return;

        rowsByClientId.Remove(clientId);
        if (row.root != null)
            Destroy(row.root);
    }

    private RowView CreateRow(ulong clientId)
    {
        GameObject rowObject = new GameObject(
            $"StatsRow_{clientId}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(HorizontalLayoutGroup),
            typeof(LayoutElement));
        rowObject.transform.SetParent(rowsRoot, false);

        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(0f, RowHeight);

        Image background = rowObject.GetComponent<Image>();
        background.raycastTarget = false;
        background.color = new Color(1f, 1f, 1f, 0.08f);

        HorizontalLayoutGroup rowLayout = rowObject.GetComponent<HorizontalLayoutGroup>();
        rowLayout.padding = new RectOffset(
            RowPaddingHorizontal,
            RowPaddingHorizontal,
            RowPaddingVertical,
            RowPaddingVertical);
        rowLayout.spacing = 8f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        LayoutElement rowElement = rowObject.GetComponent<LayoutElement>();
        rowElement.minHeight = RowHeight;
        rowElement.preferredHeight = RowHeight;
        rowElement.flexibleHeight = 0f;

        TextMeshProUGUI nameText = CreateRowText("PlayerName", FontStyles.Normal, TextAlignmentOptions.Left);
        nameText.transform.SetParent(rowObject.transform, false);

        LayoutElement nameLayout = nameText.GetComponent<LayoutElement>();
        if (nameLayout == null)
            nameLayout = nameText.gameObject.AddComponent<LayoutElement>();

        nameLayout.flexibleWidth = 1f;
        nameLayout.minWidth = 0f;
        nameLayout.preferredWidth = 0f;

        TextMeshProUGUI killsText = CreateRowText("Kills", FontStyles.Bold, TextAlignmentOptions.Right);
        killsText.transform.SetParent(rowObject.transform, false);

        LayoutElement killsLayout = killsText.GetComponent<LayoutElement>();
        if (killsLayout == null)
            killsLayout = killsText.gameObject.AddComponent<LayoutElement>();

        killsLayout.minWidth = KillsColumnWidth;
        killsLayout.preferredWidth = KillsColumnWidth;
        killsLayout.flexibleWidth = 0f;

        return new RowView
        {
            root = rowObject,
            background = background,
            nameText = nameText,
            killsText = killsText
        };
    }

    private TextMeshProUGUI CreateRowText(string name, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        label.enableAutoSizing = false;
        label.fontSize = RowFontSize;
        label.fontStyle = style;
        label.alignment = alignment;
        label.color = Color.white;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;
        label.text = string.Empty;
        return label;
    }

    private void UpdateRootHeight(int rowCount)
    {
        if (!AutoResizeStatsRoot || statsRoot == null)
            return;

        float rowsHeight = 0f;
        if (rowCount > 0)
            rowsHeight = (rowCount * RowHeight) + ((rowCount - 1) * RowsSpacing);

        float desiredHeight = (RootPadding * 2f) + TitleHeight + RootSpacing + rowsHeight;
        statsRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, desiredHeight);
    }

    private static RectTransform EnsureRectChild(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing != null && existing.TryGetComponent(out RectTransform existingRect))
            return existingRect;

        GameObject go = new GameObject(childName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static TextMeshProUGUI EnsureTextChild(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing != null && existing.TryGetComponent(out TextMeshProUGUI existingText))
            return existingText;

        GameObject go = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        return go.GetComponent<TextMeshProUGUI>();
    }

    private static int CompareEntries(LanLiveStatsEntry left, LanLiveStatsEntry right)
    {
        int byKills = right.killsThisRun.CompareTo(left.killsThisRun);
        if (byKills != 0)
            return byKills;

        int byName = string.Compare(
            left.playerName.ToString(),
            right.playerName.ToString(),
            StringComparison.OrdinalIgnoreCase);
        if (byName != 0)
            return byName;

        return left.clientId.CompareTo(right.clientId);
    }

    private struct RowView
    {
        public GameObject root;
        public Image background;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI killsText;
    }
}

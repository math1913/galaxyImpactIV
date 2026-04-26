using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public class LanLobbySkinPreviewHUD : MonoBehaviour
{
    private const int MaxSlots = 3;
    private const string RightPanelName = "RightPanel";
    private const string LegacyPreviewRootName = "LobbySkinPreviewRoot";
    private const string PreviewRootName = "LobbySkinPreviewFrame";
    private const string PreviewBorderName = "LobbySkinPreviewBorder";
    private const string LanPlayerPreviewResource = "Net/LanPlayer";

    [Serializable]
    private class SlotLayout
    {
        public string slotName;
        public Vector2 position;
        public Vector2 skinSize;
        public Vector2 skinPosition;
        public Vector2 namePosition;
        public Vector2 nameSize;
        public float nameFontSize = 28f;
        public bool showPlayerName = true;
    }

    private class SlotView
    {
        public RectTransform root;
        public Image skinImage;
        public TMP_Text nameText;
        public CanvasGroup canvasGroup;
    }

    private struct PlayerPreview
    {
        public int skinIndex;
        public string playerName;
    }

    [Header("Scene References")]
    [SerializeField] private RectTransform previewParent;
    [SerializeField] private RectTransform previewRoot;
    [SerializeField] private RectTransform previewBorder;

    [Header("Sprites")]
    [SerializeField] private Sprite previewBackgroundSprite;
    [SerializeField] private Sprite defaultLobbySprite;
    [SerializeField] private Sprite mateoLobbySprite;
    [SerializeField] private Sprite ericLobbySprite;
    [SerializeField] private Sprite ignasiLobbySprite;

    [Header("Preview Panel")]
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private Vector2 previewFrameSize = new Vector2(840f, 560f);
    [SerializeField] private Vector2 previewFramePosition = new Vector2(0f, -400f);
    [SerializeField] private Vector2 borderPadding = new Vector2(20f, 20f);
    [SerializeField] private Color borderColor = new Color(1f, 0.29411766f, 0.9882353f, 0.8f);
    [SerializeField] private Color fallbackBackgroundColor = new Color(0f, 0f, 0f, 0.25f);

    [Header("Player Slots")]
    [SerializeField] private SlotLayout[] slotLayouts = CreateDefaultLayouts();
    [SerializeField] private bool showEditorPreview = true;
    [SerializeField] private float refreshInterval = 0.15f;

    private readonly List<SlotView> slotViews = new List<SlotView>(MaxSlots);
    private readonly List<LanPlayerAvatar> sortedPlayers = new List<LanPlayerAvatar>(MaxSlots);
    private Image previewBackgroundImage;
    private Image previewBorderImage;
    private float nextRefreshTime;

#if UNITY_EDITOR
    private bool editorRefreshQueued;
#endif

    private void OnEnable()
    {
        if (Application.isPlaying)
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;

        if (Application.isPlaying && !IsLobbySceneActive())
        {
            DestroyRuntimePreviewObjects();
            return;
        }

        EnsureSlotLayouts();
        EnsureViews();
        RefreshNow();
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
    }

    private void Update()
    {
        if (Application.isPlaying && !IsLobbySceneActive())
        {
            DestroyRuntimePreviewObjects();
            return;
        }

        if (!Application.isPlaying)
        {
            if (showEditorPreview)
                RefreshEditorPreview();
            return;
        }

        if (Time.unscaledTime < nextRefreshTime)
            return;

        nextRefreshTime = Time.unscaledTime + Mathf.Max(0.05f, refreshInterval);
        RefreshNow();
    }

    private void HandleActiveSceneChanged(Scene previousScene, Scene newScene)
    {
        if (newScene.name != lobbySceneName)
            DestroyRuntimePreviewObjects();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        QueueEditorRefresh();
    }

    [ContextMenu("Apply Lobby Skin Preview Layout")]
    private void ApplyLayoutFromInspector()
    {
        EnsureSlotLayouts();
        EnsureViews();
        RefreshNow();
        MarkLayoutDirty();
    }

    private void QueueEditorRefresh()
    {
        if (editorRefreshQueued)
            return;

        editorRefreshQueued = true;
        EditorApplication.delayCall += () =>
        {
            if (this == null)
                return;

            editorRefreshQueued = false;
            ApplyLayoutFromInspector();
        };
    }

    private void MarkLayoutDirty()
    {
        if (Application.isPlaying)
            return;

        EditorUtility.SetDirty(this);
        if (previewRoot != null)
            EditorUtility.SetDirty(previewRoot);
        if (previewBorder != null)
            EditorUtility.SetDirty(previewBorder);
        if (previewBackgroundImage != null)
            EditorUtility.SetDirty(previewBackgroundImage);
        if (previewBorderImage != null)
            EditorUtility.SetDirty(previewBorderImage);

        foreach (SlotView view in slotViews)
        {
            if (view?.root != null)
                EditorUtility.SetDirty(view.root);
            if (view?.skinImage != null)
                EditorUtility.SetDirty(view.skinImage);
            if (view?.nameText != null)
                EditorUtility.SetDirty(view.nameText);
        }

        if (gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif

    private void RefreshNow()
    {
        if (Application.isPlaying && !IsLobbySceneActive())
        {
            DestroyRuntimePreviewObjects();
            return;
        }

        EnsureViews();

        if (!Application.isPlaying)
        {
            RefreshEditorPreview();
            return;
        }

        List<PlayerPreview> players = BuildRuntimePreviews();
        for (int i = 0; i < MaxSlots; i++)
        {
            if (i < players.Count)
                ApplySlot(i, players[i], true);
            else
                ApplySlot(i, default, false);
        }
    }

    private void RefreshEditorPreview()
    {
        EnsureViews();

        int[] previewSkins = { 1, 2, 3 };
        string[] previewNames = { "Host", "Client 1", "Client 2" };

        for (int i = 0; i < MaxSlots; i++)
        {
            PlayerPreview preview = new PlayerPreview
            {
                skinIndex = previewSkins[i],
                playerName = previewNames[i]
            };

            ApplySlot(i, preview, showEditorPreview);
        }
    }

    private List<PlayerPreview> BuildRuntimePreviews()
    {
        sortedPlayers.Clear();

        IReadOnlyList<LanPlayerAvatar> activePlayers = LanPlayerAvatar.ActivePlayers;
        for (int i = 0; i < activePlayers.Count; i++)
        {
            LanPlayerAvatar avatar = activePlayers[i];
            if (avatar != null && avatar.IsSpawned)
                sortedPlayers.Add(avatar);
        }

        sortedPlayers.Sort(CompareLobbyPlayers);

        List<PlayerPreview> previews = new List<PlayerPreview>(MaxSlots);
        for (int i = 0; i < sortedPlayers.Count && previews.Count < MaxSlots; i++)
        {
            LanPlayerAvatar avatar = sortedPlayers[i];
            ulong clientId = avatar.OwnerClientId;
            string displayName = avatar.SyncedDisplayName;
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = clientId == NetworkManager.ServerClientId ? "Host" : $"Client {clientId}";

            previews.Add(new PlayerPreview
            {
                skinIndex = avatar.SyncedSkinIndex,
                playerName = displayName
            });
        }

        return previews;
    }

    private int CompareLobbyPlayers(LanPlayerAvatar left, LanPlayerAvatar right)
    {
        if (left == right)
            return 0;
        if (left == null)
            return 1;
        if (right == null)
            return -1;

        bool leftIsHost = left.OwnerClientId == NetworkManager.ServerClientId;
        bool rightIsHost = right.OwnerClientId == NetworkManager.ServerClientId;

        if (leftIsHost != rightIsHost)
            return leftIsHost ? -1 : 1;

        return left.OwnerClientId.CompareTo(right.OwnerClientId);
    }

    private void ApplySlot(int slotIndex, PlayerPreview preview, bool visible)
    {
        if (slotIndex < 0 || slotIndex >= slotViews.Count || slotIndex >= slotLayouts.Length)
            return;

        SlotView view = slotViews[slotIndex];
        SlotLayout layout = slotLayouts[slotIndex];

        if (view.root != null)
        {
            view.root.anchoredPosition = layout.position;
            view.root.sizeDelta = previewFrameSize;
        }

        if (view.canvasGroup != null)
        {
            view.canvasGroup.alpha = visible ? 1f : 0f;
            view.canvasGroup.blocksRaycasts = false;
            view.canvasGroup.interactable = false;
        }

        if (view.skinImage != null)
        {
            RectTransform skinRect = view.skinImage.rectTransform;
            skinRect.anchoredPosition = layout.skinPosition;
            skinRect.sizeDelta = SizeOrFallback(layout.skinSize, new Vector2(240f, 360f));
            view.skinImage.sprite = visible ? GetLobbySkinSprite(preview.skinIndex) : null;
            view.skinImage.enabled = visible && view.skinImage.sprite != null;
            view.skinImage.preserveAspect = true;
            view.skinImage.raycastTarget = false;
        }

        if (view.nameText != null)
        {
            RectTransform nameRect = view.nameText.rectTransform;
            nameRect.anchoredPosition = layout.namePosition;
            nameRect.sizeDelta = SizeOrFallback(layout.nameSize, new Vector2(260f, 44f));
            view.nameText.gameObject.SetActive(visible && layout.showPlayerName);
            view.nameText.text = preview.playerName;
            view.nameText.fontSize = layout.nameFontSize > 0f ? layout.nameFontSize : 28f;
            view.nameText.alignment = TextAlignmentOptions.Center;
        }
    }

    private void EnsureViews()
    {
        EnsureSlotLayouts();

        if (previewParent == null)
            previewParent = FindPreviewParent();

        if (previewParent == null)
            return;

        DisableLegacyGeneratedRoots();

        if (previewBorder == null)
            previewBorder = FindOrCreateRect(previewParent, PreviewBorderName);
        else if (previewBorder.parent != previewParent)
            previewBorder.SetParent(previewParent, false);

        if (previewRoot == null)
            previewRoot = FindOrCreateRect(previewParent, PreviewRootName);
        else if (previewRoot.parent != previewParent)
            previewRoot.SetParent(previewParent, false);

        ConfigurePreviewFrame();

        while (slotViews.Count < MaxSlots)
            slotViews.Add(CreateSlotView(slotViews.Count));
    }

    private void ConfigurePreviewFrame()
    {
        Vector2 frameSize = SizeOrFallback(previewFrameSize, new Vector2(840f, 560f));

        previewBorder.anchorMin = new Vector2(0.5f, 1f);
        previewBorder.anchorMax = new Vector2(0.5f, 1f);
        previewBorder.pivot = new Vector2(0.5f, 0.5f);
        previewBorder.anchoredPosition = previewFramePosition;
        previewBorder.sizeDelta = frameSize + borderPadding;

        previewBorderImage = previewBorder.GetComponent<Image>();
        if (previewBorderImage == null)
            previewBorderImage = previewBorder.gameObject.AddComponent<Image>();

        previewBorderImage.sprite = null;
        previewBorderImage.type = Image.Type.Simple;
        previewBorderImage.color = borderColor;
        previewBorderImage.raycastTarget = false;
        previewBorder.SetAsLastSibling();

        previewRoot.anchorMin = new Vector2(0.5f, 1f);
        previewRoot.anchorMax = new Vector2(0.5f, 1f);
        previewRoot.pivot = new Vector2(0.5f, 0.5f);
        previewRoot.anchoredPosition = previewFramePosition;
        previewRoot.sizeDelta = frameSize;

        previewBackgroundImage = previewRoot.GetComponent<Image>();
        if (previewBackgroundImage == null)
            previewBackgroundImage = previewRoot.gameObject.AddComponent<Image>();

        previewBackgroundImage.sprite = previewBackgroundSprite;
        previewBackgroundImage.type = Image.Type.Simple;
        previewBackgroundImage.preserveAspect = false;
        previewBackgroundImage.color = previewBackgroundSprite != null ? Color.white : fallbackBackgroundColor;
        previewBackgroundImage.raycastTarget = false;
        previewRoot.SetAsLastSibling();
    }

    private void DisableLegacyGeneratedRoots()
    {
        foreach (RectTransform rectTransform in FindObjectsOfType<RectTransform>(true))
        {
            if (rectTransform.name == LegacyPreviewRootName)
                rectTransform.gameObject.SetActive(false);
        }
    }

    private SlotView CreateSlotView(int slotIndex)
    {
        RectTransform slotRoot = FindOrCreateRect(previewRoot, $"LobbySkinSlot_{slotIndex + 1}");
        slotRoot.anchorMin = new Vector2(0.5f, 0.5f);
        slotRoot.anchorMax = new Vector2(0.5f, 0.5f);
        slotRoot.pivot = new Vector2(0.5f, 0.5f);

        CanvasGroup canvasGroup = slotRoot.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = slotRoot.gameObject.AddComponent<CanvasGroup>();

        Image skin = FindOrCreateImage(slotRoot, "Skin");
        RectTransform skinRect = skin.rectTransform;
        skinRect.anchorMin = new Vector2(0.5f, 0.5f);
        skinRect.anchorMax = new Vector2(0.5f, 0.5f);
        skinRect.pivot = new Vector2(0.5f, 0.5f);

        TMP_Text nameText = FindOrCreateText(slotRoot, "Name");
        RectTransform nameRect = nameText.rectTransform;
        nameRect.anchorMin = new Vector2(0.5f, 0.5f);
        nameRect.anchorMax = new Vector2(0.5f, 0.5f);
        nameRect.pivot = new Vector2(0.5f, 0.5f);

        return new SlotView
        {
            root = slotRoot,
            skinImage = skin,
            nameText = nameText,
            canvasGroup = canvasGroup
        };
    }

    private RectTransform FindPreviewParent()
    {
        RectTransform rightPanel = FindRectByName(RightPanelName);
        if (rightPanel != null)
            return rightPanel;

        if (Application.isPlaying)
            return null;

        Canvas canvas = FindObjectOfType<Canvas>(true);
        return canvas != null ? canvas.transform as RectTransform : null;
    }

    private bool IsLobbySceneActive()
    {
        return SceneManager.GetActiveScene().name == lobbySceneName;
    }

    private void DestroyRuntimePreviewObjects()
    {
        if (!Application.isPlaying)
            return;

        DestroyRect(previewRoot);
        DestroyRect(previewBorder);

        previewRoot = null;
        previewBorder = null;
        previewBackgroundImage = null;
        previewBorderImage = null;
        slotViews.Clear();

        foreach (RectTransform rectTransform in FindObjectsOfType<RectTransform>(true))
        {
            if (rectTransform == null)
                continue;

            if (rectTransform.name == PreviewRootName ||
                rectTransform.name == PreviewBorderName ||
                rectTransform.name == LegacyPreviewRootName)
            {
                Destroy(rectTransform.gameObject);
            }
        }
    }

    private void DestroyRect(RectTransform rectTransform)
    {
        if (rectTransform != null)
            Destroy(rectTransform.gameObject);
    }

    private RectTransform FindRectByName(string objectName)
    {
        foreach (RectTransform rectTransform in FindObjectsOfType<RectTransform>(true))
        {
            if (rectTransform.name == objectName)
                return rectTransform;
        }

        return null;
    }

    private RectTransform FindOrCreateRect(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
            return existing as RectTransform;

        GameObject created = new GameObject(objectName, typeof(RectTransform));
        created.layer = parent.gameObject.layer;
        RectTransform rect = created.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private Image FindOrCreateImage(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null && existing.TryGetComponent(out Image existingImage))
            return existingImage;

        GameObject created = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        created.layer = parent.gameObject.layer;
        RectTransform rect = created.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return created.GetComponent<Image>();
    }

    private TMP_Text FindOrCreateText(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null && existing.TryGetComponent(out TMP_Text existingText))
            return existingText;

        GameObject created = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        created.layer = parent.gameObject.layer;
        RectTransform rect = created.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        TMP_Text text = created.GetComponent<TMP_Text>();
        text.raycastTarget = false;
        text.color = Color.white;
        text.fontStyle = FontStyles.Bold;
        return text;
    }

    private Sprite GetLobbySkinSprite(int skinIndex)
    {
        switch (PlayerSkinProfile.NormalizeSkinIndex(skinIndex))
        {
            case 1:
                return mateoLobbySprite != null ? mateoLobbySprite : LoadGameplaySkinSprite(1);
            case 2:
                return ericLobbySprite != null ? ericLobbySprite : LoadGameplaySkinSprite(2);
            case 3:
                return ignasiLobbySprite != null ? ignasiLobbySprite : LoadGameplaySkinSprite(3);
            default:
                return defaultLobbySprite != null ? defaultLobbySprite : LoadGameplaySkinSprite(0);
        }
    }

    private Sprite LoadGameplaySkinSprite(int skinIndex)
    {
        GameObject playerPrefab = Resources.Load<GameObject>(LanPlayerPreviewResource);
        if (playerPrefab == null)
            return null;

        PlayerSkinApplier skinApplier = playerPrefab.GetComponent<PlayerSkinApplier>();
        if (skinApplier == null)
            return null;

        return skinApplier.GetSkinSprite(skinIndex);
    }

    private void EnsureSlotLayouts()
    {
        SlotLayout[] defaults = CreateDefaultLayouts();
        if (slotLayouts == null || slotLayouts.Length != MaxSlots)
        {
            SlotLayout[] resized = new SlotLayout[MaxSlots];
            for (int i = 0; i < MaxSlots; i++)
                resized[i] = slotLayouts != null && i < slotLayouts.Length && slotLayouts[i] != null ? slotLayouts[i] : defaults[i];

            slotLayouts = resized;
            return;
        }

        for (int i = 0; i < MaxSlots; i++)
        {
            if (slotLayouts[i] == null)
                slotLayouts[i] = defaults[i];
        }
    }

    private static SlotLayout[] CreateDefaultLayouts()
    {
        return new[]
        {
            new SlotLayout
            {
                slotName = "Host Center",
                position = new Vector2(0f, -10f),
                skinSize = new Vector2(300f, 430f),
                skinPosition = new Vector2(0f, 10f),
                namePosition = new Vector2(0f, -238f),
                nameSize = new Vector2(320f, 46f),
                nameFontSize = 30f
            },
            new SlotLayout
            {
                slotName = "Client Left",
                position = new Vector2(-270f, -45f),
                skinSize = new Vector2(230f, 340f),
                skinPosition = new Vector2(0f, 10f),
                namePosition = new Vector2(0f, -190f),
                nameSize = new Vector2(270f, 40f),
                nameFontSize = 24f
            },
            new SlotLayout
            {
                slotName = "Client Right",
                position = new Vector2(270f, -45f),
                skinSize = new Vector2(230f, 340f),
                skinPosition = new Vector2(0f, 10f),
                namePosition = new Vector2(0f, -190f),
                nameSize = new Vector2(270f, 40f),
                nameFontSize = 24f
            }
        };
    }

    private Vector2 SizeOrFallback(Vector2 value, Vector2 fallback)
    {
        return value.x <= 0f || value.y <= 0f ? fallback : value;
    }
}

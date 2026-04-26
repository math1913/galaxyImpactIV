using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NetworkObject))]
public class LanPlayerAvatar : NetworkBehaviour
{
    private const string GameplaySceneName = "GameScene";
    private const string GameOverSceneName = "GameOver";
    private const float PredictedShotFallbackSeconds = 1.5f;
    private const int MaxPlayerNameLength = 24;

    private struct PredictedShotState
    {
        public Bullet bullet;
        public float expiresAt;
    }

    private static readonly List<LanPlayerAvatar> s_activePlayers = new List<LanPlayerAvatar>();
    private static readonly Dictionary<ulong, LanRunStatsSnapshot> s_serverRunStats = new Dictionary<ulong, LanRunStatsSnapshot>();
    private static readonly Dictionary<ulong, LanLiveStatsEntry> s_liveStats = new Dictionary<ulong, LanLiveStatsEntry>();
    private static readonly List<LanLiveStatsEntry> s_liveStatsSnapshot = new List<LanLiveStatsEntry>();

    private static bool s_serverMatchEnded;
    private static float s_serverMatchStartTime;
    private static int s_replicatedCurrentWave;
    private static bool s_replicatedMatchEnded;

    public static IReadOnlyList<LanPlayerAvatar> ActivePlayers => s_activePlayers;
    public static IReadOnlyList<LanLiveStatsEntry> LiveStats => s_liveStatsSnapshot;
    public static int CurrentWave => s_replicatedCurrentWave;
    public static bool MatchEnded => s_replicatedMatchEnded;
    public static event Action<int> OnWaveChanged;
    public static event Action<IReadOnlyList<LanLiveStatsEntry>> OnLiveStatsChanged;

    private readonly NetworkVariable<int> syncedHealth = new NetworkVariable<int>(100);
    private readonly NetworkVariable<int> syncedMaxHealth = new NetworkVariable<int>(100);
    private readonly NetworkVariable<bool> syncedDead = new NetworkVariable<bool>(false);
    private readonly NetworkVariable<int> syncedShield = new NetworkVariable<int>(0);
    private readonly NetworkVariable<int> syncedMaxShield = new NetworkVariable<int>(0);
    private readonly NetworkVariable<int> syncedAmmo = new NetworkVariable<int>(0);
    private readonly NetworkVariable<int> syncedTotalAmmo = new NetworkVariable<int>(0);
    private readonly NetworkVariable<bool> syncedReloading = new NetworkVariable<bool>(false);
    private readonly NetworkVariable<int> syncedDashCharges = new NetworkVariable<int>(0);
    private readonly NetworkVariable<int> syncedMaxDashCharges = new NetworkVariable<int>(0);
    private readonly NetworkVariable<int> syncedWave = new NetworkVariable<int>(0);
    private readonly NetworkVariable<bool> syncedMatchEnded = new NetworkVariable<bool>(false);
    private readonly NetworkVariable<int> syncedSkinIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<FixedString64Bytes> syncedPlayerName = new NetworkVariable<FixedString64Bytes>(
        new FixedString64Bytes("Jugador"),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly Dictionary<uint, PredictedShotState> predictedShots = new Dictionary<uint, PredictedShotState>();

    private PlayerController playerController;
    private Weapon weapon;
    private Health health;
    private Shield shield;
    private DashChargesEffect dash;
    private BuffManager buffManager;
    private PlayerSkinApplier skinApplier;
    private PlayerIdentity playerIdentity;
    private Rigidbody2D rb;
    private SpriteRenderer bodyRenderer;
    private Collider2D[] bodyColliders;
    private Transform currentBoundViewTarget;
    private LanPlayerAvatar currentSpectatorTarget;
    private readonly List<LanPlayerAvatar> spectatorCandidates = new List<LanPlayerAvatar>();

    private Vector2 serverMoveInput;
    private Vector3 serverAimWorld;
    private bool serverReloadRequested;
    private bool serverDashRequested;
    private uint localShotSequence;
    private float nextPredictedShotTime;
    private bool deathReported;

    public bool IsAlive => health == null || health.CurrentHealth > 0;
    public bool IsAuthorityAvatar => NetworkManager != null && OwnerClientId == Unity.Netcode.NetworkManager.ServerClientId;
    public int SyncedSkinIndex => PlayerSkinProfile.NormalizeSkinIndex(syncedSkinIndex.Value);
    public string SyncedDisplayName => syncedPlayerName.Value.ToString();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        ResetRuntimeState();
    }

    public static void ResetRuntimeState()
    {
        s_activePlayers.Clear();
        s_serverRunStats.Clear();
        s_liveStats.Clear();
        s_liveStatsSnapshot.Clear();
        s_serverMatchEnded = false;
        s_serverMatchStartTime = 0f;
        s_replicatedCurrentWave = 0;
        s_replicatedMatchEnded = false;
        OnWaveChanged = null;
        OnLiveStatsChanged = null;
    }

    public static Transform GetClosestPlayerTransform(Vector3 position)
    {
        LanPlayerAvatar bestPlayer = null;
        float bestDistance = float.MaxValue;

        foreach (LanPlayerAvatar lanPlayer in s_activePlayers)
        {
            if (lanPlayer == null || !lanPlayer.IsAlive)
                continue;

            float distance = (lanPlayer.transform.position - position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestPlayer = lanPlayer;
            }
        }

        return bestPlayer != null ? bestPlayer.transform : null;
    }

    public static LanPlayerAvatar GetAvatarByClientId(ulong clientId)
    {
        foreach (LanPlayerAvatar lanPlayer in s_activePlayers)
        {
            if (lanPlayer != null && lanPlayer.OwnerClientId == clientId)
                return lanPlayer;
        }

        return null;
    }

    public static LanPlayerAvatar GetAuthorityAvatar()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
            return null;

        return GetAvatarByClientId(Unity.Netcode.NetworkManager.ServerClientId);
    }

    public static void ServerSetCurrentWave(int wave)
    {
        if (!LanRuntime.IsServer)
            return;

        LanPlayerAvatar authorityAvatar = GetAuthorityAvatar();
        if (authorityAvatar == null)
            return;

        authorityAvatar.syncedWave.Value = Mathf.Max(0, wave);
        ApplyReplicatedWave(authorityAvatar.syncedWave.Value);

        foreach (LanPlayerAvatar lanPlayer in s_activePlayers)
        {
            if (lanPlayer == null)
                continue;

            LanRunStatsSnapshot stats = GetOrCreateServerStats(lanPlayer.OwnerClientId);
            stats.currentWaveReached = Mathf.Max(stats.currentWaveReached, authorityAvatar.syncedWave.Value);
            s_serverRunStats[lanPlayer.OwnerClientId] = stats;
        }
    }

    public static void ServerRecordWaveCompleted(int waveNumber)
    {
        if (!LanRuntime.IsServer)
            return;

        int xpReward = GameStatsManager.Instance != null
            ? GameStatsManager.Instance.GetWaveXpReward(waveNumber)
            : 0;

        foreach (LanPlayerAvatar lanPlayer in s_activePlayers)
        {
            if (lanPlayer == null)
                continue;

            LanRunStatsSnapshot stats = GetOrCreateServerStats(lanPlayer.OwnerClientId);
            stats.RegisterWaveCompleted(waveNumber, xpReward);
            s_serverRunStats[lanPlayer.OwnerClientId] = stats;
        }
    }

    public static void ServerRespawnDeadPlayers()
    {
        if (!LanRuntime.IsServer || s_serverMatchEnded)
            return;

        foreach (LanPlayerAvatar lanPlayer in s_activePlayers)
        {
            if (lanPlayer == null || lanPlayer.IsAlive)
                continue;

            lanPlayer.ServerRespawn();
        }
    }

    public static void ServerRecordEnemyKill(ulong killerClientId, EnemyController.EnemyType enemyType, int xpGained)
    {
        if (!LanRuntime.IsServer || killerClientId == ulong.MaxValue)
            return;

        LanRunStatsSnapshot stats = GetOrCreateServerStats(killerClientId);
        stats.RegisterKill(enemyType, xpGained);
        s_serverRunStats[killerClientId] = stats;
        ServerBroadcastLiveStats(killerClientId);
    }

    public static void ServerRecordPickup(ulong collectorClientId, LanPickupType pickupType, int xpAmount = 0)
    {
        if (!LanRuntime.IsServer || collectorClientId == ulong.MaxValue)
            return;

        LanRunStatsSnapshot stats = GetOrCreateServerStats(collectorClientId);
        stats.RegisterPickup(pickupType);
        if (pickupType == LanPickupType.Exp && xpAmount > 0)
            stats.AddXp(xpAmount);

        s_serverRunStats[collectorClientId] = stats;
    }

    public static void ServerResolvePlayerShot(ulong shooterClientId, uint shotId, Vector3 endPosition)
    {
        if (!LanRuntime.IsServer)
            return;

        LanPlayerAvatar shooterAvatar = GetAvatarByClientId(shooterClientId);
        if (shooterAvatar == null)
            return;

        ClientRpcParams targetParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { shooterClientId }
            }
        };

        shooterAvatar.ShotEndedClientRpc(shotId, endPosition, targetParams);
    }

    public static void ServerPlayPickupFeedback(ulong collectorClientId, ulong pickupNetworkObjectId)
    {
        if (!LanRuntime.IsServer)
            return;

        LanPlayerAvatar collectorAvatar = GetAvatarByClientId(collectorClientId);
        if (collectorAvatar == null)
            return;

        ClientRpcParams targetParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { collectorClientId }
            }
        };

        collectorAvatar.PickupFeedbackClientRpc(pickupNetworkObjectId, targetParams);
    }

    public static void ServerHandlePlayerDeath(LanPlayerAvatar deadAvatar)
    {
        if (!LanRuntime.IsServer || deadAvatar == null || s_serverMatchEnded)
            return;

        if (HasLivingPlayers())
            return;

        s_serverMatchEnded = true;
        s_replicatedMatchEnded = true;

        LanPlayerAvatar authorityAvatar = GetAuthorityAvatar();
        if (authorityAvatar != null)
            authorityAvatar.syncedMatchEnded.Value = true;

        WaveManager waveManager = FindObjectOfType<WaveManager>();
        if (waveManager != null)
            waveManager.StopWaves();

        int secondsPlayed = Mathf.FloorToInt(Time.unscaledTime - s_serverMatchStartTime);
        int minutesPlayed = Mathf.FloorToInt(secondsPlayed / 60f);

        foreach (LanPlayerAvatar lanPlayer in s_activePlayers)
        {
            if (lanPlayer == null)
                continue;

            LanRunStatsSnapshot stats = GetOrCreateServerStats(lanPlayer.OwnerClientId);
            stats.secondsPlayed = Mathf.Max(0, secondsPlayed);
            stats.minutesPlayed = Mathf.Max(0, minutesPlayed);
            stats.currentWaveReached = Mathf.Max(stats.currentWaveReached, s_replicatedCurrentWave);
            s_serverRunStats[lanPlayer.OwnerClientId] = stats;

            ClientRpcParams targetParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { lanPlayer.OwnerClientId }
                }
            };

            lanPlayer.ReceiveFinalStatsClientRpc(stats, targetParams);
        }

        LanSessionLifecycle.MarkLanRunEnded();

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager != null)
            networkManager.StartCoroutine(LoadGameOverAfterStatsFrame());
        else
            SceneManager.LoadScene(GameOverSceneName);
    }

    private static LanRunStatsSnapshot GetOrCreateServerStats(ulong clientId)
    {
        if (!s_serverRunStats.TryGetValue(clientId, out LanRunStatsSnapshot stats))
        {
            stats = default;
            s_serverRunStats[clientId] = stats;
        }

        return stats;
    }

    private static void ServerBroadcastLiveStats(ulong clientId)
    {
        if (!LanRuntime.IsServer || clientId == ulong.MaxValue)
            return;

        bool isConnected = IsClientConnectedOnServer(clientId);
        LanLiveStatsEntry entry = BuildServerLiveStatsEntry(clientId, isConnected);
        ApplyLiveStatsEntry(entry);

        LanPlayerAvatar broadcaster = GetAuthorityAvatar() ?? GetAvatarByClientId(clientId);
        if (broadcaster != null)
            broadcaster.ReceiveLiveStatsClientRpc(entry);
    }

    private static void ServerBroadcastAllLiveStats()
    {
        if (!LanRuntime.IsServer)
            return;

        foreach (LanPlayerAvatar lanPlayer in s_activePlayers)
        {
            if (lanPlayer == null)
                continue;

            if (!IsClientConnectedOnServer(lanPlayer.OwnerClientId))
                continue;

            GetOrCreateServerStats(lanPlayer.OwnerClientId);
            ServerBroadcastLiveStats(lanPlayer.OwnerClientId);
        }
    }

    private static void ServerRemoveLiveStats(ulong clientId)
    {
        if (!LanRuntime.IsServer || clientId == ulong.MaxValue)
            return;

        LanLiveStatsEntry entry = new LanLiveStatsEntry
        {
            clientId = clientId,
            playerName = NormalizePlayerName($"Player {clientId}"),
            killsThisRun = 0,
            isConnected = false
        };

        s_serverRunStats.Remove(clientId);
        ApplyLiveStatsEntry(entry);

        LanPlayerAvatar broadcaster = GetAuthorityAvatar();
        if (broadcaster != null)
            broadcaster.ReceiveLiveStatsClientRpc(entry);
    }

    private static LanLiveStatsEntry BuildServerLiveStatsEntry(ulong clientId, bool isConnected)
    {
        LanRunStatsSnapshot stats = GetOrCreateServerStats(clientId);
        return new LanLiveStatsEntry
        {
            clientId = clientId,
            playerName = GetServerLivePlayerName(clientId),
            killsThisRun = stats.killsThisRun,
            isConnected = isConnected
        };
    }

    private static FixedString64Bytes GetServerLivePlayerName(ulong clientId)
    {
        LanPlayerAvatar avatar = GetAvatarByClientId(clientId);
        if (avatar != null)
        {
            string syncedName = avatar.syncedPlayerName.Value.ToString();
            if (!string.IsNullOrWhiteSpace(syncedName))
                return NormalizePlayerName(syncedName);
        }

        return NormalizePlayerName($"Player {clientId}");
    }

    private static bool IsClientConnectedOnServer(ulong clientId)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsServer)
            return true;

        if (clientId == Unity.Netcode.NetworkManager.ServerClientId)
            return true;

        IReadOnlyList<ulong> connectedClientIds = networkManager.ConnectedClientsIds;
        for (int i = 0; i < connectedClientIds.Count; i++)
        {
            if (connectedClientIds[i] == clientId)
                return true;
        }

        return false;
    }

    private static void ApplyLiveStatsEntry(LanLiveStatsEntry entry)
    {
        if (entry.clientId == ulong.MaxValue)
            return;

        if (entry.isConnected)
            s_liveStats[entry.clientId] = entry;
        else
            s_liveStats.Remove(entry.clientId);

        RebuildLiveStatsSnapshot();
    }

    private static void RebuildLiveStatsSnapshot()
    {
        s_liveStatsSnapshot.Clear();
        foreach (LanLiveStatsEntry entry in s_liveStats.Values)
        {
            if (entry.isConnected)
                s_liveStatsSnapshot.Add(entry);
        }

        OnLiveStatsChanged?.Invoke(s_liveStatsSnapshot);
    }

    private static bool HasLivingPlayers()
    {
        foreach (LanPlayerAvatar lanPlayer in s_activePlayers)
        {
            if (lanPlayer != null && lanPlayer.IsAlive)
                return true;
        }

        return false;
    }

    private static void ApplyReplicatedWave(int wave)
    {
        if (s_replicatedCurrentWave == wave)
            return;

        s_replicatedCurrentWave = wave;
        OnWaveChanged?.Invoke(wave);
    }

    private static void ApplyReplicatedMatchEnded(bool matchEnded)
    {
        s_replicatedMatchEnded = matchEnded;
    }

    private void SetSyncedSkinIndex(int skinIndex)
    {
        if (!IsServer)
            return;

        syncedSkinIndex.Value = PlayerSkinProfile.NormalizeSkinIndex(skinIndex);
        ApplySkin(syncedSkinIndex.Value);
    }

    [ServerRpc]
    private void SubmitSkinSelectionServerRpc(int skinIndex)
    {
        SetSyncedSkinIndex(skinIndex);
    }

    private void HandleSkinChanged(int previousValue, int newValue)
    {
        ApplySkin(newValue);
    }

    private void ApplySkin(int skinIndex)
    {
        if (skinApplier != null)
            skinApplier.ApplySkinIndex(skinIndex);
    }

    private void SetSyncedPlayerName(FixedString64Bytes playerName)
    {
        if (!IsServer)
            return;

        syncedPlayerName.Value = NormalizePlayerName(playerName.ToString());
        ApplyPlayerName(syncedPlayerName.Value);
        ApplyPlayerNameClientRpc(syncedPlayerName.Value);
        ServerBroadcastLiveStats(OwnerClientId);
    }

    [ServerRpc]
    private void SubmitPlayerNameServerRpc(FixedString64Bytes playerName)
    {
        SetSyncedPlayerName(playerName);
    }

    private void HandlePlayerNameChanged(FixedString64Bytes previousValue, FixedString64Bytes newValue)
    {
        ApplyPlayerName(newValue);
    }

    private void ApplyPlayerName(FixedString64Bytes playerName)
    {
        string displayName = playerName.ToString();
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = $"Player {OwnerClientId}";

        if (playerIdentity != null)
        {
            playerIdentity.EnsureNameTagReady();
            playerIdentity.SetDisplayName(displayName);
        }
    }

    private void ApplyIdentityVisibility()
    {
        if (playerIdentity != null)
            playerIdentity.SetShowHealthBar(!IsOwner);
    }

    [ClientRpc]
    private void ApplyPlayerNameClientRpc(FixedString64Bytes playerName)
    {
        ApplyPlayerName(playerName);
    }

    [ClientRpc]
    private void ReceiveLiveStatsClientRpc(LanLiveStatsEntry entry)
    {
        ApplyLiveStatsEntry(entry);
    }

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        weapon = GetComponentInChildren<Weapon>(true);
        health = GetComponent<Health>();
        shield = GetComponent<Shield>();
        dash = GetComponent<DashChargesEffect>();
        buffManager = GetComponent<BuffManager>();
        skinApplier = GetComponent<PlayerSkinApplier>();
        playerIdentity = GetComponent<PlayerIdentity>();
        if (playerIdentity == null)
            playerIdentity = gameObject.AddComponent<PlayerIdentity>();

        if (playerIdentity != null)
        {
            playerIdentity.SetApplySavedNameOnStart(false);
            playerIdentity.EnsureNameTagReady();
        }

        if (skinApplier != null)
            skinApplier.SetApplySavedSkinOnStart(false);
        rb = GetComponent<Rigidbody2D>();
        bodyRenderer = GetComponent<SpriteRenderer>();
        bodyColliders = GetComponents<Collider2D>();

        if (health != null)
            health.OnDeath.AddListener(HandleLocalDeath);
    }

    public override void OnNetworkSpawn()
    {
        if (!s_activePlayers.Contains(this))
            s_activePlayers.Add(this);

        currentBoundViewTarget = null;
        currentSpectatorTarget = null;

        syncedSkinIndex.OnValueChanged += HandleSkinChanged;
        syncedPlayerName.OnValueChanged += HandlePlayerNameChanged;
        ApplySkin(syncedSkinIndex.Value);
        ApplyPlayerName(syncedPlayerName.Value);
        ApplyIdentityVisibility();

        if (weapon != null)
            weapon.SetUseLocalInput(false);

        if (dash != null)
            dash.SetUseLocalInput(false);

        if (IsAuthorityAvatar)
        {
            syncedWave.OnValueChanged += HandleWaveChanged;
            syncedMatchEnded.OnValueChanged += HandleMatchEndedChanged;
            ApplyReplicatedWave(syncedWave.Value);
            ApplyReplicatedMatchEnded(syncedMatchEnded.Value);
        }

        if (IsServer)
        {
            bool initializedMatch = false;
            if (SceneManager.GetActiveScene().name == GameplaySceneName && IsAuthorityAvatar)
            {
                InitializeServerMatchState();
                initializedMatch = true;
            }

            EnsureServerStatsEntry();
            ServerBroadcastLiveStats(OwnerClientId);
            if (!initializedMatch)
                ServerBroadcastAllLiveStats();

            Vector3 spawnPosition = GetSpawnPosition(OwnerClientId);
            transform.position = spawnPosition;
            if (rb != null)
                rb.position = spawnPosition;

            serverAimWorld = transform.position + transform.right;
            SyncStateFromComponents();
            SetAlivePresentation(true);
        }
        else if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
            SetAlivePresentation(true);
        }

        if (IsOwner)
        {
            int selectedSkinIndex = PlayerSkinProfile.GetSelectedSkinIndex();
            if (IsServer)
            {
                SetSyncedSkinIndex(selectedSkinIndex);
                SetSyncedPlayerName(GetLocalPlayerName());
            }
            else
            {
                SubmitSkinSelectionServerRpc(selectedSkinIndex);
                SubmitPlayerNameServerRpc(GetLocalPlayerName());
            }

            StartCoroutine(BindLocalSceneReferencesRoutine());
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
            NetworkManager.Singleton.OnTransportFailure += HandleTransportFailure;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnNetworkDespawn()
    {
        s_activePlayers.Remove(this);
        SceneManager.sceneLoaded -= OnSceneLoaded;
        syncedSkinIndex.OnValueChanged -= HandleSkinChanged;
        syncedPlayerName.OnValueChanged -= HandlePlayerNameChanged;

        if (IsAuthorityAvatar)
        {
            syncedWave.OnValueChanged -= HandleWaveChanged;
            syncedMatchEnded.OnValueChanged -= HandleMatchEndedChanged;
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
            NetworkManager.Singleton.OnTransportFailure -= HandleTransportFailure;
        }

        ClearPredictedShots();
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnDeath.RemoveListener(HandleLocalDeath);
    }

    private void Update()
    {
        if (!IsGameplaySceneLoaded())
            return;

        if (IsOwner)
        {
            CaptureOwnerInput();
            CleanupPredictedShots();
        }

        if (IsServer)
        {
            bool canControl = !s_serverMatchEnded && (health == null || health.CurrentHealth > 0);

            if (playerController != null)
                playerController.SetExternalInput(canControl ? serverMoveInput : Vector2.zero, serverAimWorld);

            if (!canControl)
            {
                serverMoveInput = Vector2.zero;
                serverReloadRequested = false;
                serverDashRequested = false;

                if (playerController != null)
                    playerController.ClearOverrideVelocity();

                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }

                SyncStateFromComponents();
                return;
            }

            if (serverReloadRequested && weapon != null)
            {
                weapon.Reload();
                serverReloadRequested = false;
            }

            if (serverDashRequested && dash != null)
            {
                dash.TryDashTowards(serverAimWorld);
                serverDashRequested = false;
            }

            SyncStateFromComponents();
            return;
        }

        ApplyStateToReplica();
    }

    private void CaptureOwnerInput()
    {
        if (MatchEnded)
            return;

        if (health != null && health.CurrentHealth <= 0)
        {
            UpdateSpectatorViewFromInput();
            return;
        }

        BindLocalViewTarget(transform);

        Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        Vector3 aimWorld = transform.position + transform.right;

        Camera cam = Camera.main;
        if (cam != null)
        {
            aimWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            aimWorld.z = 0f;
        }

        bool fireHeld = Input.GetButton("Fire1");
        bool reloadPressed = Input.GetKeyDown(KeyCode.R);
        bool dashPressed = Input.GetKeyDown(KeyCode.LeftShift);

        if (IsServer)
        {
            ApplyOwnerInput(moveInput, aimWorld);

            if (fireHeld)
                TryFireAsHost();

            if (reloadPressed && weapon != null)
                weapon.Reload();

            if (dashPressed && dash != null)
                dash.TryDashTowards(aimWorld);

            return;
        }

        SubmitInputServerRpc(moveInput, aimWorld);

        if (fireHeld)
            TryRequestServerShot();

        if (reloadPressed && weapon != null && weapon.CanReload())
        {
            weapon.PlayReloadStartSfxLocal();
            RequestReloadServerRpc();
        }

        if (dashPressed)
            RequestDashServerRpc(aimWorld);
    }

    [ServerRpc]
    private void SubmitInputServerRpc(Vector2 moveInput, Vector3 aimWorld)
    {
        if (s_serverMatchEnded)
            return;

        ApplyOwnerInput(moveInput, aimWorld);
    }

    [ServerRpc]
    private void RequestFireServerRpc(uint shotId, Vector3 muzzlePosition, Quaternion muzzleRotation)
    {
        if (s_serverMatchEnded)
        {
            RejectShotForOwner(shotId);
            return;
        }

        bool accepted = weapon != null
            && health != null
            && health.CurrentHealth > 0
            && weapon.TryFireWithShotId(shotId, muzzlePosition, muzzleRotation);

        if (!accepted)
        {
            RejectShotForOwner(shotId);
            return;
        }

        ShotStartedClientRpc(OwnerClientId, shotId, muzzlePosition, muzzleRotation, weapon.ProjectileLifeTime);
    }

    [ServerRpc]
    private void RequestReloadServerRpc()
    {
        if (!s_serverMatchEnded)
            serverReloadRequested = true;
    }

    [ServerRpc]
    private void RequestDashServerRpc(Vector3 aimWorld)
    {
        if (s_serverMatchEnded)
            return;

        serverAimWorld = aimWorld;
        serverDashRequested = true;
    }

    [ClientRpc]
    private void ShotStartedClientRpc(ulong shooterClientId, uint shotId, Vector3 muzzlePosition, Quaternion muzzleRotation, float projectileLifeTime)
    {
        if (NetworkManager == null)
            return;

        if (IsOwner && shooterClientId == NetworkManager.LocalClientId && predictedShots.TryGetValue(shotId, out PredictedShotState state))
        {
            if (state.bullet != null)
            {
                state.bullet.SnapTo(muzzlePosition);

                state.expiresAt = Time.unscaledTime + projectileLifeTime + PredictedShotFallbackSeconds;
                predictedShots[shotId] = state;
            }

            return;
        }

        if (LanRuntime.IsServer)
            return;

        if (weapon != null)
            weapon.PlayShootSfxLocal(muzzlePosition);
    }

    [ClientRpc]
    private void ShotEndedClientRpc(uint shotId, Vector3 endPosition, ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner || !predictedShots.TryGetValue(shotId, out PredictedShotState state))
            return;

        if (state.bullet != null)
        {
            state.bullet.SnapTo(endPosition);
            state.bullet.ForceDespawn();
        }

        predictedShots.Remove(shotId);
    }

    [ClientRpc]
    private void RejectShotClientRpc(uint shotId, ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner || !predictedShots.TryGetValue(shotId, out PredictedShotState state))
            return;

        if (state.bullet != null)
            state.bullet.ForceDespawn();

        if (weapon != null)
            weapon.RevertPredictedShot();

        predictedShots.Remove(shotId);
        nextPredictedShotTime = Time.unscaledTime;
    }

    [ClientRpc]
    private void ReceiveFinalStatsClientRpc(LanRunStatsSnapshot snapshot, ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner)
            return;

        if (GameStatsManager.Instance != null)
        {
            _ = GameStatsManager.Instance.ApplyLanFinalSnapshotAndSend(snapshot);
            return;
        }

        GameStatsManager.CachePendingLanSnapshot(snapshot, sendToApi: true);
    }

    [ClientRpc]
    private void PickupFeedbackClientRpc(ulong pickupNetworkObjectId, ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner || NetworkManager == null || NetworkManager.SpawnManager == null)
            return;

        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(pickupNetworkObjectId, out NetworkObject pickupNetworkObject))
            return;

        if (pickupNetworkObject != null && pickupNetworkObject.TryGetComponent(out PickupBase pickup))
            pickup.PlayPickupFeedbackLocal();
    }

    private void ApplyOwnerInput(Vector2 moveInput, Vector3 aimWorld)
    {
        serverMoveInput = moveInput;
        serverAimWorld = aimWorld;
    }

    private void TryFireAsHost()
    {
        if (weapon == null || !weapon.CanAttemptShot() || !weapon.TryGetMuzzleSnapshot(out Vector3 position, out Quaternion rotation))
            return;

        localShotSequence++;
        weapon.TryFireWithShotId(localShotSequence, position, rotation);
    }

    private void TryRequestServerShot()
    {
        if (weapon == null)
            return;

        if (Time.unscaledTime < nextPredictedShotTime)
            return;

        if (weapon.IsReloading || weapon.CurrentAmmo <= 0)
            return;

        if (!weapon.TryGetMuzzleSnapshot(out Vector3 position, out Quaternion rotation))
            return;

        localShotSequence++;
        nextPredictedShotTime = Time.unscaledTime + weapon.SecondsPerShot;

        RequestFireServerRpc(localShotSequence, position, rotation);
    }

    private void CleanupPredictedShots()
    {
        if (predictedShots.Count == 0)
            return;

        List<uint> staleShots = null;
        foreach (KeyValuePair<uint, PredictedShotState> shot in predictedShots)
        {
            bool expired = Time.unscaledTime >= shot.Value.expiresAt;
            bool missingBullet = shot.Value.bullet == null;
            if (!expired && !missingBullet)
                continue;

            if (staleShots == null)
                staleShots = new List<uint>();

            staleShots.Add(shot.Key);
        }

        if (staleShots == null)
            return;

        foreach (uint shotId in staleShots)
            predictedShots.Remove(shotId);
    }

    private void ClearPredictedShots()
    {
        foreach (PredictedShotState shot in predictedShots.Values)
        {
            if (shot.bullet != null)
                shot.bullet.ForceDespawn();
        }

        predictedShots.Clear();
    }

    private void SyncStateFromComponents()
    {
        if (health != null)
        {
            syncedHealth.Value = health.CurrentHealth;
            syncedMaxHealth.Value = health.MaxHealth;
            syncedDead.Value = health.CurrentHealth <= 0;
        }

        if (shield != null)
        {
            syncedShield.Value = shield.CurrentShield;
            syncedMaxShield.Value = shield.MaxShield;
        }

        if (weapon != null)
        {
            syncedAmmo.Value = weapon.CurrentAmmo;
            syncedTotalAmmo.Value = weapon.TotalAmmo;
            syncedReloading.Value = weapon.IsReloading;
        }

        if (dash != null)
        {
            syncedDashCharges.Value = dash.Charges;
            syncedMaxDashCharges.Value = dash.MaxCharges;
        }

        if (IsAuthorityAvatar)
        {
            syncedWave.Value = s_replicatedCurrentWave;
            syncedMatchEnded.Value = s_replicatedMatchEnded;
        }
    }

    private void ApplyStateToReplica()
    {
        if (health != null)
            health.ApplyStateFromNetwork(syncedHealth.Value, syncedMaxHealth.Value, syncedDead.Value, triggerDamageFeedback: true);

        if (shield != null)
            shield.ApplyStateFromNetwork(syncedShield.Value, syncedMaxShield.Value);

        if (weapon != null)
        {
            int ammoToApply = syncedAmmo.Value;
            if (IsOwner && predictedShots.Count > 0)
                ammoToApply = Mathf.Min(ammoToApply, weapon.CurrentAmmo);

            weapon.ApplyStateFromNetwork(ammoToApply, syncedTotalAmmo.Value, syncedReloading.Value);
        }

        if (dash != null)
            dash.ApplyStateFromNetwork(syncedDashCharges.Value, syncedMaxDashCharges.Value);

        SetAlivePresentation(!syncedDead.Value);
    }

    private IEnumerator BindLocalSceneReferencesRoutine()
    {
        for (int attempt = 0; attempt < 60; attempt++)
        {
            foreach (HUDController hud in FindObjectsOfType<HUDController>())
                hud.BindPlayer(health, weapon, shield);

            foreach (DashHUD dashHud in FindObjectsOfType<DashHUD>())
                dashHud.BindPlayer(dash);

            foreach (BuffIconsHUD buffHud in FindObjectsOfType<BuffIconsHUD>())
                buffHud.BindPlayer(buffManager);

            BindLocalViewTarget(GetPreferredOwnerViewTarget());

            yield return null;
        }
    }

    private void EnsureServerStatsEntry()
    {
        if (!IsServer)
            return;

        GetOrCreateServerStats(OwnerClientId);
    }

    private void InitializeServerMatchState()
    {
        s_serverRunStats.Clear();
        s_liveStats.Clear();
        s_liveStatsSnapshot.Clear();
        foreach (LanPlayerAvatar lanPlayer in s_activePlayers)
        {
            if (lanPlayer != null && IsClientConnectedOnServer(lanPlayer.OwnerClientId))
            {
                s_serverRunStats[lanPlayer.OwnerClientId] = default;
                ApplyLiveStatsEntry(BuildServerLiveStatsEntry(lanPlayer.OwnerClientId, isConnected: true));
            }
        }

        s_serverMatchEnded = false;
        s_serverMatchStartTime = Time.unscaledTime;
        s_replicatedCurrentWave = 0;
        s_replicatedMatchEnded = false;
        syncedMatchEnded.Value = false;
        syncedWave.Value = 0;
        ApplyReplicatedWave(0);
        ServerBroadcastAllLiveStats();
    }

    private void HandleLocalDeath()
    {
        if (deathReported)
            return;

        deathReported = true;
        serverMoveInput = Vector2.zero;
        serverReloadRequested = false;
        serverDashRequested = false;
        currentSpectatorTarget = null;
        SetAlivePresentation(false);

        if (playerController != null)
            playerController.ClearOverrideVelocity();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (IsOwner)
            UpdateSpectatorViewFromInput();

        if (IsServer)
            ServerHandlePlayerDeath(this);
    }

    private static FixedString64Bytes GetLocalPlayerName()
    {
        return NormalizePlayerName(PlayerPrefs.GetString("username", "Jugador"));
    }

    private static FixedString64Bytes NormalizePlayerName(string playerName)
    {
        string normalized = string.IsNullOrWhiteSpace(playerName) ? "Jugador" : playerName.Trim();
        if (normalized.Length > MaxPlayerNameLength)
            normalized = normalized.Substring(0, MaxPlayerNameLength);

        return new FixedString64Bytes(normalized);
    }

    private Vector3 GetSpawnPosition(ulong clientId)
    {
        float angle = (clientId % 8) * 45f * Mathf.Deg2Rad;
        float radius = 2.5f;
        return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
    }

    private void ServerRespawn()
    {
        if (!IsServer || health == null)
            return;

        deathReported = false;
        currentSpectatorTarget = null;
        serverMoveInput = Vector2.zero;
        serverReloadRequested = false;
        serverDashRequested = false;

        Vector3 spawnPosition = GetSpawnPosition(OwnerClientId);
        transform.position = spawnPosition;
        if (rb != null)
        {
            rb.position = spawnPosition;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = true;
        }

        if (playerController != null)
            playerController.ClearOverrideVelocity();

        health.ResetHealth();
        if (dash != null)
            dash.ResetChargesToFull();

        SetAlivePresentation(true);
        SyncStateFromComponents();

        ClientRpcParams targetParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { OwnerClientId }
            }
        };

        OwnerRespawnedClientRpc(targetParams);
    }

    [ClientRpc]
    private void OwnerRespawnedClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner)
            return;

        deathReported = false;
        currentSpectatorTarget = null;
        SetAlivePresentation(true);
        BindLocalViewTarget(transform);
    }

    private Transform GetPreferredOwnerViewTarget()
    {
        if (!IsOwner)
            return transform;

        if (health == null || health.CurrentHealth > 0)
            return transform;

        RefreshSpectatorCandidates();
        SelectSpectatorTarget(direction: 0);
        return currentSpectatorTarget != null ? currentSpectatorTarget.transform : transform;
    }

    private void UpdateSpectatorViewFromInput()
    {
        if (!IsOwner)
            return;

        RefreshSpectatorCandidates();

        int direction = 0;
        if (Input.GetKeyDown(KeyCode.Tab))
            direction = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? -1 : 1;

        SelectSpectatorTarget(direction);

        Transform target = currentSpectatorTarget != null ? currentSpectatorTarget.transform : transform;
        BindLocalViewTarget(target);
    }

    private void RefreshSpectatorCandidates()
    {
        spectatorCandidates.Clear();

        foreach (LanPlayerAvatar lanPlayer in s_activePlayers)
        {
            if (lanPlayer == null || lanPlayer == this || !lanPlayer.IsAlive)
                continue;

            spectatorCandidates.Add(lanPlayer);
        }
    }

    private void SelectSpectatorTarget(int direction)
    {
        if (spectatorCandidates.Count == 0)
        {
            currentSpectatorTarget = null;
            return;
        }

        if (currentSpectatorTarget == null || !currentSpectatorTarget.IsAlive)
        {
            currentSpectatorTarget = spectatorCandidates[0];
            return;
        }

        if (direction == 0)
            return;

        int currentIndex = spectatorCandidates.IndexOf(currentSpectatorTarget);
        if (currentIndex < 0)
        {
            currentSpectatorTarget = spectatorCandidates[0];
            return;
        }

        int nextIndex = (currentIndex + direction + spectatorCandidates.Count) % spectatorCandidates.Count;
        currentSpectatorTarget = spectatorCandidates[nextIndex];
    }

    private void BindLocalViewTarget(Transform target)
    {
        if (!IsOwner || target == null || currentBoundViewTarget == target)
            return;

        foreach (CameraFollow2D cameraFollow in FindObjectsOfType<CameraFollow2D>())
            cameraFollow.BindPlayer(target);

        foreach (MinimapController minimap in FindObjectsOfType<MinimapController>())
            minimap.BindPlayer(target);

        currentBoundViewTarget = target;
    }

    private void SetAlivePresentation(bool alive)
    {
        if (bodyRenderer != null)
            bodyRenderer.enabled = alive;

        if (bodyColliders != null)
        {
            foreach (Collider2D bodyCollider in bodyColliders)
            {
                if (bodyCollider != null)
                    bodyCollider.enabled = alive;
            }
        }

        if (playerIdentity != null)
            playerIdentity.SetVisible(alive);

        if (rb != null)
        {
            if (LanRuntime.IsServer)
                rb.simulated = alive;
            else
                rb.simulated = false;

            if (!alive)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == GameplaySceneName)
        {
            currentBoundViewTarget = null;
            currentSpectatorTarget = null;

            if (IsServer)
            {
                if (IsAuthorityAvatar)
                    InitializeServerMatchState();

                Vector3 spawnPosition = GetSpawnPosition(OwnerClientId);
                transform.position = spawnPosition;
                if (rb != null)
                    rb.position = spawnPosition;
            }

            if (playerIdentity != null)
                playerIdentity.EnsureNameTagReady();

            ApplyPlayerName(syncedPlayerName.Value);
            ApplyIdentityVisibility();
            SetAlivePresentation(!syncedDead.Value);

            if (IsOwner)
                StartCoroutine(BindLocalSceneReferencesRoutine());

            return;
        }

        if (scene.name == GameOverSceneName)
            ClearPredictedShots();
    }

    private bool IsGameplaySceneLoaded()
    {
        return SceneManager.GetActiveScene().name == GameplaySceneName;
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (!IsOwner || networkManager == null)
            return;

        if (networkManager.IsServer && clientId != networkManager.LocalClientId)
            ServerRemoveLiveStats(clientId);

        if (!networkManager.IsServer && clientId == networkManager.LocalClientId)
            LanSessionLifecycle.ExitToLobby();
    }

    private void HandleTransportFailure()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (!IsOwner || networkManager == null)
            return;

        if (!networkManager.IsServer)
            LanSessionLifecycle.ExitToLobby();
    }

    private void HandleWaveChanged(int previousValue, int newValue)
    {
        ApplyReplicatedWave(newValue);
    }

    private void HandleMatchEndedChanged(bool previousValue, bool newValue)
    {
        ApplyReplicatedMatchEnded(newValue);
    }

    private void RejectShotForOwner(uint shotId)
    {
        ClientRpcParams targetParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { OwnerClientId }
            }
        };

        RejectShotClientRpc(shotId, targetParams);
    }

    private static IEnumerator LoadGameOverAfterStatsFrame()
    {
        yield return null;

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager != null && networkManager.SceneManager != null)
            networkManager.SceneManager.LoadScene(GameOverSceneName, LoadSceneMode.Single);
        else
            SceneManager.LoadScene(GameOverSceneName);
    }
}

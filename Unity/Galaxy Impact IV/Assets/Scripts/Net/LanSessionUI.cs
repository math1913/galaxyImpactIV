using System.Text;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Networking.Transport;

public class LanSessionUI : MonoBehaviour
{
    private const string LanPlayerPrefabPath = "Net/LanPlayer";

    [Header("UI")]
    [SerializeField] private TMP_InputField ipInput;
    [SerializeField] private TMP_InputField portInput;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button startMatchButton;

    [Header("Single Exit Button")]
    [SerializeField] private Button exitButton;
    [SerializeField] private TMP_Text exitButtonLabel;

    [Header("Text Outputs")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text connectedText;
    [SerializeField] private TMP_Text clientsText;

    [Header("Scenes")]
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string gameSceneName = "GameScene";

    private NetworkManager nm;
    private UnityTransport utp;

    private void Start()
    {
        LanSessionLifecycle.MarkLobbyReady();

        nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError("[LAN] No hay NetworkManager.Singleton en escena.");
            enabled = false;
            return;
        }

        utp = nm.NetworkConfig.NetworkTransport as UnityTransport;
        if (utp == null)
        {
            Debug.LogError("[LAN] El NetworkTransport del NetworkManager no es UnityTransport o no está asignado.");
            enabled = false;
            return;
        }

        if (ipInput != null && string.IsNullOrWhiteSpace(ipInput.text))
            ipInput.text = "127.0.0.1";

        if (portInput != null && string.IsNullOrWhiteSpace(portInput.text))
            portInput.text = "7777";

        if (hostButton != null)
            hostButton.onClick.AddListener(StartHost);

        if (joinButton != null)
            joinButton.onClick.AddListener(StartClient);

        if (startMatchButton != null)
            startMatchButton.onClick.AddListener(StartMatch);

        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(OnExitPressed);
        }

        nm.OnClientConnectedCallback += OnClientConnected;
        nm.OnClientDisconnectCallback += OnClientDisconnected;
        nm.OnTransportFailure += OnTransportFailure;

        RefreshUI();
        RefreshClientsList();
        SetStatus("Listo");
    }

    private void OnDestroy()
    {
        if (nm == null)
            return;

        nm.OnClientConnectedCallback -= OnClientConnected;
        nm.OnClientDisconnectCallback -= OnClientDisconnected;
        nm.OnTransportFailure -= OnTransportFailure;
    }

    public void StartHost()
    {
        if (nm.IsListening || !ConfigureLanPlayerPrefab())
            return;

        ushort port = ParsePort();
        var listen = NetworkEndpoint.AnyIpv4;
        listen.Port = port;

        var server = NetworkEndpoint.LoopbackIpv4;
        server.Port = port;

        utp.SetConnectionData(server, listen);

        bool ok = nm.StartHost();
        SetStatus(ok ? $"Host escuchando en :{port}" : "Error al iniciar Host");
        RefreshUI();
        RefreshClientsList();
    }

    public void StartClient()
    {
        if (nm.IsListening || !ConfigureLanPlayerPrefab())
            return;

        ushort port = ParsePort();
        string ip = string.IsNullOrWhiteSpace(ipInput?.text) ? "127.0.0.1" : ipInput.text.Trim();

        if (!NetworkEndpoint.TryParse(ip, port, out var serverEp, NetworkFamily.Ipv4))
        {
            SetStatus("IP inválida (usa IPv4 tipo 192.168.1.X)");
            return;
        }

        utp.SetConnectionData(serverEp);

        bool ok = nm.StartClient();
        SetStatus(ok ? $"Conectando a {ip}:{port}..." : "Error al iniciar Client");
        RefreshUI();
    }

    public void StartMatch()
    {
        if (!nm.IsHost)
        {
            SetStatus("Solo el host puede iniciar la partida");
            return;
        }

        nm.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        SetStatus($"Cargando {gameSceneName}...");
    }

    private void OnExitPressed()
    {
        if (nm == null)
            return;

        if (nm.IsHost)
            SetStatus("Cerrando host...");
        else if (nm.IsClient)
            SetStatus("Saliendo...");

        LanSessionLifecycle.ExitToLobby(lobbySceneName);
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!nm.IsServer && clientId == nm.LocalClientId)
            SetStatus("Conectado al host");

        RefreshUI();
        RefreshClientsList();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!nm.IsServer && clientId == nm.LocalClientId)
        {
            SetStatus("Desconectado. Volviendo al Lobby...");
            LanSessionLifecycle.ExitToLobby(lobbySceneName);
            return;
        }

        RefreshUI();
        RefreshClientsList();
    }

    private void OnTransportFailure()
    {
        SetStatus("Fallo de transporte/red");

        if (nm != null && nm.IsListening)
            LanSessionLifecycle.ExitToLobby(lobbySceneName);

        RefreshUI();
    }

    private void RefreshUI()
    {
        bool listening = nm.IsListening;

        if (hostButton != null)
            hostButton.interactable = !listening;

        if (joinButton != null)
            joinButton.interactable = !listening;

        if (ipInput != null)
            ipInput.interactable = !listening;

        if (portInput != null)
            portInput.interactable = !listening;

        if (startMatchButton != null)
            startMatchButton.gameObject.SetActive(nm.IsHost);

        bool connected = nm.IsClient || nm.IsServer;
        if (exitButton != null)
            exitButton.gameObject.SetActive(connected);

        if (exitButtonLabel != null && connected)
            exitButtonLabel.text = nm.IsHost ? "Stop Host" : "Leave";

        int connectedCount = nm.IsServer ? nm.ConnectedClientsIds.Count : 0;
        if (connectedText != null)
            connectedText.text = $"Conectados: {connectedCount}";
    }

    private void RefreshClientsList()
    {
        if (clientsText == null)
            return;

        if (!nm.IsServer)
        {
            clientsText.text = "";
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("Clientes:");

        foreach (var id in nm.ConnectedClientsIds)
        {
            var ep = utp.GetEndpoint(id);
            sb.AppendLine($"- {id} => {ep.Address}:{ep.Port}");
        }

        clientsText.text = sb.ToString();
    }

    private ushort ParsePort()
    {
        if (portInput != null && ushort.TryParse(portInput.text, out ushort port))
            return port;

        if (portInput != null)
            portInput.text = "7777";

        return 7777;
    }

    private bool ConfigureLanPlayerPrefab()
    {
        GameObject lanPlayerPrefab = Resources.Load<GameObject>(LanPlayerPrefabPath);
        if (lanPlayerPrefab == null)
        {
            Debug.LogError($"[LAN] No se encontró el prefab del jugador LAN en Resources/{LanPlayerPrefabPath}.");
            SetStatus("Falta el prefab LAN del jugador");
            return false;
        }

        nm.NetworkConfig.PlayerPrefab = lanPlayerPrefab;
        return true;
    }

    private void SetStatus(string msg)
    {
        Debug.Log("[LAN] " + msg);
        if (statusText != null)
            statusText.text = msg;
    }
}

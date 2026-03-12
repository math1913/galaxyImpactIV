using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LanSessionUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TMP_InputField ipInput;
    [SerializeField] TMP_InputField portInput;
    [SerializeField] Button hostButton;
    [SerializeField] Button joinButton;
    [SerializeField] Button startMatchButton; // solo hostCriterio de éxito inicial
    
    [SerializeField] TMP_Text statusText;
    [SerializeField] TMP_Text connectedText;

    [Header("Scene")]
    [SerializeField] string gameSceneName = "GameScene";

    NetworkManager nm;
    UnityTransport utp;

    void Start()
    {
        nm = NetworkManager.Singleton;
        Debug.Log("Singleton exists? " + (nm != null));

        if (nm == null)
        {
            Debug.LogError("No hay NetworkManager.Singleton. ¿NetworkManager desactivado o no está en la escena?");
            enabled = false;
            return;
        }

        utp = nm.NetworkConfig.NetworkTransport as UnityTransport;
        if (utp == null)
        {
            Debug.LogError("El NetworkTransport del NetworkManager no es UnityTransport o no está asignado.");
            enabled = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(ipInput.text)) ipInput.text = "127.0.0.1";
        if (string.IsNullOrWhiteSpace(portInput.text)) portInput.text = "7777";

        hostButton.onClick.AddListener(StartHost);
        joinButton.onClick.AddListener(StartClient);
        startMatchButton.onClick.AddListener(StartMatch);

        nm.OnClientConnectedCallback += _ => RefreshUI();
        nm.OnClientDisconnectCallback += _ => RefreshUI();
        nm.OnServerStarted += RefreshUI;
        nm.OnTransportFailure += () => SetStatus("Fallo de transporte/red");

        RefreshUI();
        SetStatus("Listo");
    }

    void OnDestroy()
    {
        if (nm == null) return;
        nm.OnClientConnectedCallback -= _ => RefreshUI();
        nm.OnClientDisconnectCallback -= _ => RefreshUI();
        nm.OnServerStarted -= RefreshUI;
    }

    public void StartHost()
    {
        ushort port = ParsePort();
        utp.SetConnectionData("0.0.0.0", port); // escuchar LAN
        bool ok = nm.StartHost();
        SetStatus(ok ? $"Host iniciado en :{port}" : "Error al iniciar Host");
        RefreshUI();
    }

    public void StartClient()
    {
        ushort port = ParsePort();
        string ip = string.IsNullOrWhiteSpace(ipInput.text) ? "127.0.0.1" : ipInput.text.Trim();
        utp.SetConnectionData(ip, port);
        bool ok = nm.StartClient();
        SetStatus(ok ? $"Conectando a {ip}:{port}..." : "Error al iniciar Client");
        RefreshUI();
    }

    public void StartMatch()
    {
        if (!nm.IsHost) { SetStatus("Solo el host puede iniciar"); return; }
        nm.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        SetStatus($"Cargando {gameSceneName}...");
    }

    ushort ParsePort()
    {
        if (ushort.TryParse(portInput.text, out ushort p)) return p;
        portInput.text = "7777";
        return 7777;
    }

    void RefreshUI()
    {
        int connected = nm.IsServer ? nm.ConnectedClientsIds.Count : 0;
        connectedText.text = $"Conectados: {connected}";

        startMatchButton.gameObject.SetActive(nm.IsHost);

        bool listening = nm.IsListening;
        hostButton.interactable = !listening;
        joinButton.interactable = !listening;
        ipInput.interactable = !listening;
        portInput.interactable = !listening;
    }

    void SetStatus(string msg)
    {
        statusText.text = msg;
        Debug.Log("[LAN] " + msg);
    }
}
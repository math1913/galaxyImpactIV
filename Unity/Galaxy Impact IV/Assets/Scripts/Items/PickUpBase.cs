using Unity.Netcode;
using UnityEngine;

public abstract class PickupBase : MonoBehaviour
{
    [Header("Configuración General")]
    [SerializeField] protected float floatAmplitude = 0.2f;
    [SerializeField] protected float floatSpeed = 2f;
    [SerializeField] protected AudioClip pickupSound;
    
    private Vector3 startPos;

    protected virtual void Start() => startPos = transform.position;

    protected virtual void Update()
    {
        // animación flotante
        transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.Rotate(Vector3.forward * 45f * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (LanRuntime.IsActive && !LanRuntime.IsServer)
            return;

        if (!other.CompareTag("Player")) return;
        OnPickup(other);

        if (pickupSound != null)
        {
            if (LanRuntime.IsActive
                && other.TryGetComponent(out LanPlayerAvatar lanPlayer)
                && TryGetComponent(out NetworkObject pickupNetworkObject)
                && pickupNetworkObject.IsSpawned)
            {
                if (lanPlayer.OwnerClientId == Unity.Netcode.NetworkManager.ServerClientId)
                    PlayPickupFeedbackLocal();
                else
                    LanPlayerAvatar.ServerPlayPickupFeedback(lanPlayer.OwnerClientId, pickupNetworkObject.NetworkObjectId);
            }
            else
            {
                PlayPickupFeedbackLocal();
            }
        }

        if (TryGetComponent<NetworkObject>(out var networkObject) && networkObject.IsSpawned)
            networkObject.Despawn(true);
        else
            Destroy(gameObject);
    }

    public void PlayPickupFeedbackLocal()
    {
        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
    }

    protected abstract void OnPickup(Collider2D player);

    /// <summary>
    /// Devuelve una posición válida (sin colisionar) para spawnear pick-ups
    /// </summary>
    public static Vector3 GetValidSpawn(Vector3 candidate, float radius, LayerMask obstructMask, int tries = 20)
    {
        while (tries-- > 0)
        {
            bool blocked = Physics2D.OverlapCircle(candidate, radius, obstructMask);
            if (!blocked)
                return candidate;

            // si está bloqueado, probamos otro random
            candidate = new Vector3(
                candidate.x + Random.Range(-radius, radius),
                candidate.y + Random.Range(-radius, radius),
                candidate.z
            );
        }

        return candidate; // fallback, por si acaso
    }
}

using Unity.Netcode;
using UnityEngine;

/// Movimiento top-down + rotaciÃ³n hacia el mouse.
/// Requiere Rigidbody2D.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 30f;

    [Header("Referencias")]
    [SerializeField] private Camera cam;
    [SerializeField] private SpriteRenderer background; // Fondo de referencia
    [SerializeField] private float margin = 0.5f;       // Margen interno opcional
    [Header("Buffs")]
    [SerializeField] private float speedMultiplier = 1f;

    //exponer al HUD/debug
    public float BaseMoveSpeed => moveSpeed;
    public float CurrentMoveSpeed => moveSpeed * speedMultiplier;
    private Rigidbody2D _rb;
    private Vector2 _moveInput;
    private Vector2 _currentVelocity;

    private Vector2 minBounds;
    private Vector2 maxBounds;
    private bool _overrideVelocityActive = false;
    private Vector2 _overrideVelocity;
    private bool _useExternalInput = false;
    private Vector2 _externalMoveInput;
    private Vector3 _externalAimWorld;

    private void Awake()
    {
        if (LanRuntime.IsActive && GetComponent<NetworkObject>() == null)
        {
            Destroy(gameObject);
            return;
        }

        _rb = GetComponent<Rigidbody2D>();
        if (!cam)
            cam = Camera.main;
        if (GetComponent<BuffManager>() == null)
            gameObject.AddComponent<BuffManager>();
    }

    private void Start()
    {
        if (background == null)
            background = GameObject.Find("Background")?.GetComponent<SpriteRenderer>();

        if (background != null)
            UpdateBounds();
    }

    private void Update()
    {
        if (LanRuntime.IsClientReplica(gameObject))
            return;

        if (_useExternalInput)
        {
            _moveInput = _externalMoveInput;
            RotateTowards(_externalAimWorld);
            return;
        }

        // Movimiento (WASD)
        _moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        // RotaciÃ³n hacia el cursor
        if (cam == null)
            cam = Camera.main;

        if (cam == null)
            return;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        RotateTowards(mouseWorld);
    }

    private void FixedUpdate()
    {
        if (LanRuntime.IsClientReplica(gameObject))
            return;

        if (_overrideVelocityActive)
        {
            _rb.linearVelocity = _overrideVelocity;
            return;
        }

        Vector2 targetVelocity = _moveInput * (moveSpeed * speedMultiplier);

        // InterpolaciÃ³n entre aceleraciÃ³n y frenado
        float rate = (targetVelocity.magnitude > _rb.linearVelocity.magnitude) ? acceleration : deceleration;

        _currentVelocity = Vector2.MoveTowards(_rb.linearVelocity, targetVelocity, rate * Time.fixedDeltaTime);
        _rb.linearVelocity = _currentVelocity;
    }

    private void LateUpdate()
    {
        if (LanRuntime.IsClientReplica(gameObject))
            return;

        if (background == null) return;

        // Limitar la posiciÃ³n del jugador dentro del fondo
        Vector3 pos = transform.position;

        pos.x = Mathf.Clamp(pos.x, minBounds.x + margin, maxBounds.x - margin);
        pos.y = Mathf.Clamp(pos.y, minBounds.y + margin, maxBounds.y - margin);

        transform.position = pos;
    }

    private void UpdateBounds()
    {
        Bounds bgBounds = background.bounds; // lÃ­mites reales del sprite en el mundo
        minBounds = bgBounds.min;
        maxBounds = bgBounds.max;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Evita empujones entre enemigos y jugador
        if (!collision.collider.CompareTag("Enemy"))
            return;

        Vector2 toEnemy = (collision.collider.transform.position - transform.position).normalized;
        float towardEnemy = Vector2.Dot(_rb.linearVelocity, toEnemy);
        if (towardEnemy > 0f)
            _rb.linearVelocity -= toEnemy * towardEnemy;
        Debug.Log("ChocÃ³ con: " + collision.gameObject.name);
    }

    public void MultiplySpeed(float multiplier)
    {
        speedMultiplier *= multiplier;
    }

    public void DivideSpeed(float multiplier)
    {
        if (Mathf.Approximately(multiplier, 0f)) return;
        speedMultiplier /= multiplier;
    }

    public void SetOverrideVelocity(Vector2 velocity)
    {
        _overrideVelocityActive = true;
        _overrideVelocity = velocity;
    }
 
    public void ClearOverrideVelocity()
    {
        _overrideVelocityActive = false;
        _overrideVelocity = Vector2.zero;
    }

    public void SetExternalInput(Vector2 moveInput, Vector3 aimWorld)
    {
        _useExternalInput = true;
        _externalMoveInput = moveInput;
        _externalAimWorld = aimWorld;
    }

    public void DisableExternalInput()
    {
        _useExternalInput = false;
        _externalMoveInput = Vector2.zero;
    }

    private void RotateTowards(Vector3 worldTarget)
    {
        Vector2 dir = worldTarget - transform.position;
        if (dir.sqrMagnitude < 0.0001f)
            return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        _rb.SetRotation(angle);
    }
}

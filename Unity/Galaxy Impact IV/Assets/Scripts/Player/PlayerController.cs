using UnityEngine;

/// Movimiento top-down + rotación hacia el mouse.
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

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (!cam) 
            cam = Camera.main;
        if (GetComponent<BuffManager>() == null)
            gameObject.AddComponent<BuffManager>();
    }

    private void Start()
    {
        if (background != null)
            UpdateBounds();
    }

    private void Update()
    {
        // Movimiento (WASD)
        _moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        // Rotación hacia el cursor
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (mouseWorld - transform.position);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        _rb.SetRotation(angle);
    }

    private void FixedUpdate()
    {
        if (_overrideVelocityActive)
        {
            _rb.linearVelocity = _overrideVelocity;
            return;
        }

        Vector2 targetVelocity =  _moveInput * (moveSpeed * speedMultiplier);

        // Interpolación entre aceleración y frenado
        float rate = (targetVelocity.magnitude > _rb.linearVelocity.magnitude) ? acceleration : deceleration;

        _currentVelocity = Vector2.MoveTowards(_rb.linearVelocity, targetVelocity, rate * Time.fixedDeltaTime);
        _rb.linearVelocity = _currentVelocity;
    }

    private void LateUpdate()
    {
        if (background == null) return;

        // Limitar la posición del jugador dentro del fondo
        Vector3 pos = transform.position;

        pos.x = Mathf.Clamp(pos.x, minBounds.x + margin, maxBounds.x - margin);
        pos.y = Mathf.Clamp(pos.y, minBounds.y + margin, maxBounds.y - margin);

        transform.position = pos;
    }

    private void UpdateBounds()
    {
        Bounds bgBounds = background.bounds; // límites reales del sprite en el mundo
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
        Debug.Log("Chocó con: " + collision.gameObject.name);
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

}
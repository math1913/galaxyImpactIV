using UnityEngine;

public class DashChargesEffect : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode dashKey = KeyCode.LeftShift;

    [Header("Charges")]
    [SerializeField] private int maxCharges = 3;
    [SerializeField] private int charges = 3;

    [Header("Dash")]
    [SerializeField] private float dashDistanceUnits = 4f;
    [SerializeField] private float dashDuration = 0.10f;
    [SerializeField] private float dashCooldown = 0.05f;

    [Header("HUD")]
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(0.15f, 0.15f, 0.15f, 1f);

    [Header("References")]
    [SerializeField] private Camera cam;

    private PlayerController pc;
    private bool isDashing = false;
    private float dashEndTime = 0f;
    private float nextDashTime = 0f;

    public int Charges => charges;
    public int MaxCharges => maxCharges;
    public Color ActiveColor => activeColor;
    public Color EmptyColor => emptyColor;

    private void Awake()
    {
        pc = GetComponent<PlayerController>();
        if (!cam) cam = Camera.main;
    }

    private void Update()
    {
        if (charges <= 0) return;
        if (isDashing) return;
        if (Time.time < nextDashTime) return;

        if (Input.GetKeyDown(dashKey))
        {
            StartDash();
        }
    }

    private void FixedUpdate()
    {
        if (!isDashing) return;

        if (Time.time >= dashEndTime)
        {
            isDashing = false;
            if (pc != null) pc.ClearOverrideVelocity();
        }
    }

    private void StartDash()
    {
        if (pc == null || cam == null) return;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        Vector2 dir = (mouseWorld - transform.position);
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        float dur = Mathf.Max(0.02f, dashDuration);
        float speed = dashDistanceUnits / dur;

        pc.SetOverrideVelocity(dir * speed);

        isDashing = true;
        dashEndTime = Time.time + dur;
        nextDashTime = Time.time + dashCooldown;

        charges = Mathf.Max(0, charges - 1);
    }

    // === API ===

    public void ResetChargesToFull()
    {
        charges = maxCharges;
    }

    public void AddCharge(int amount = 1)
    {
        charges = Mathf.Clamp(charges + Mathf.Max(0, amount), 0, maxCharges);
    }

    public void SetDashUIColor(Color filledColor)
    {
        activeColor = filledColor;
    }
}

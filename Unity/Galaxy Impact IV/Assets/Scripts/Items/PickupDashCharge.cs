using UnityEngine;

public class PickupDashCharge : PickupBase
{
    [Header("Dash Charge Pickup")]
    [SerializeField] private Color dashColor = Color.white;

    protected override void OnPickup(Collider2D player)
    {
        var dash = player.GetComponent<DashChargesEffect>();
        if (dash == null) dash = player.gameObject.AddComponent<DashChargesEffect>();

        dash.SetDashUIColor(dashColor);
        dash.AddCharge(3);
    }
}

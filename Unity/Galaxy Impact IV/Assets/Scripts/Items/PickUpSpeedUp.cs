using UnityEngine;

public class PickupSpeedUp : PickupBase
{
    [Header("Speed Up")]
    [SerializeField] private float duration = 8f;
    [SerializeField] private float multiplier = 1.5f;
    [SerializeField] private Sprite icon;

    protected override void OnPickup(Collider2D player)
    {
        var pc = player.GetComponent<PlayerController>();
        var bm = player.GetComponent<BuffManager>();
        if (pc == null || bm == null) return;

        bm.AddOrRefresh(
            id: "SpeedUp",
            duration: duration,
            onApply: () => pc.MultiplySpeed(multiplier),
            onRemove: () => pc.DivideSpeed(multiplier),
            icon: icon
        );
    }
}

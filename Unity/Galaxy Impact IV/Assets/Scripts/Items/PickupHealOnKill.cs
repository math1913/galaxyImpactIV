using UnityEngine;

public class PickupHealOnKill : PickupBase
{
    [Header("Heal On Kill")]
    [SerializeField] private float duration = 8f;
    [SerializeField] private int healPerKill = 5;
    [SerializeField] private Sprite icon;

    protected override void OnPickup(Collider2D player)
    {
        var bm = player.GetComponent<BuffManager>() ?? player.gameObject.AddComponent<BuffManager>();

        bm.AddOrRefresh(
            id: "HealOnKill",
            duration: duration,
            onApply: () =>
            {
                var effect = player.GetComponent<HealOnKillEffect>();
                if (effect == null) effect = player.gameObject.AddComponent<HealOnKillEffect>();

                effect.SetHealAmount(healPerKill);
                effect.enabled = true;
            },
            onRemove: () =>
            {
                var effect = player.GetComponent<HealOnKillEffect>();
                if (effect != null) effect.enabled = false;
            },
            icon: icon
        );
    }
}

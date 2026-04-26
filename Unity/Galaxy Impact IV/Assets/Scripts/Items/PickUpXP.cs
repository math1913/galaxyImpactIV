using UnityEngine;

public class PickupXP : PickupBase
{
    [SerializeField] private int xpAmount = 20;

    protected override void OnPickup(Collider2D player)
    {
        if (LanRuntime.IsActive && player.TryGetComponent(out LanPlayerAvatar lanPlayer))
        {
            LanPlayerAvatar.ServerRecordPickup(lanPlayer.OwnerClientId, LanPickupType.Exp, xpAmount);
            return;
        }

        if (GameStatsManager.Instance != null)
        {
            GameStatsManager.Instance.RegisterPickup(LanPickupType.Exp);
            GameStatsManager.Instance.AddXP(xpAmount);
        }
        else
        {
            Debug.LogWarning("PickupXP: No se encontró GameStatsManager.Instance.");
        }
    }
}

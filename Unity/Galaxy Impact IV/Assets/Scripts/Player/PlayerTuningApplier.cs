using UnityEngine;

[DefaultExecutionOrder(-500)]
public class PlayerTuningApplier : MonoBehaviour
{
    [SerializeField] private PlayerTuningProfile profile;
    [SerializeField] private bool applyOnAwake = true;

    private void Awake()
    {
        if (applyOnAwake)
            ApplyProfile();
    }

    public void ApplyProfile()
    {
        if (profile == null)
            return;

        if (TryGetComponent(out PlayerController playerController))
        {
            playerController.ApplyTuning(
                profile.moveSpeed,
                profile.acceleration,
                profile.deceleration,
                profile.movementMargin);
        }

        if (TryGetComponent(out DashChargesEffect dash))
        {
            dash.ApplyTuning(
                profile.maxDashCharges,
                profile.startDashCharges,
                profile.dashDistanceUnits,
                profile.dashDuration,
                profile.dashCooldown,
                profile.dashActiveColor,
                profile.dashEmptyColor);
        }

        Weapon weapon = GetComponentInChildren<Weapon>(true);
        if (weapon != null)
        {
            weapon.ApplyTuning(
                profile.fireRate,
                profile.magazineSize,
                profile.reloadTime,
                profile.spreadDeg,
                profile.totalAmmo,
                profile.shootSfx,
                profile.reloadStartSfx,
                profile.reloadCompleteSfx,
                profile.shootVolume,
                profile.reloadVolume);
        }

        if (TryGetComponent(out Health health))
        {
            health.ApplyTuning(
                profile.maxHealth,
                profile.playerDamageSfx,
                profile.playerDeathSfx,
                profile.damageVolume,
                profile.deathVolume);
        }
    }
}

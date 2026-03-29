using Unity.Netcode;

public enum LanPickupType : byte
{
    Health = 0,
    Shield = 1,
    Ammo = 2,
    Exp = 3
}

public struct LanRunStatsSnapshot : INetworkSerializable
{
    public int killsNormal;
    public int killsFast;
    public int killsTank;
    public int killsShooter;
    public int minutesPlayed;
    public int score;
    public int pickupHealth;
    public int pickupShield;
    public int pickupAmmo;
    public int pickupExp;
    public int wavesCompleted;
    public int xpThisRun;
    public int killsThisRun;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref killsNormal);
        serializer.SerializeValue(ref killsFast);
        serializer.SerializeValue(ref killsTank);
        serializer.SerializeValue(ref killsShooter);
        serializer.SerializeValue(ref minutesPlayed);
        serializer.SerializeValue(ref score);
        serializer.SerializeValue(ref pickupHealth);
        serializer.SerializeValue(ref pickupShield);
        serializer.SerializeValue(ref pickupAmmo);
        serializer.SerializeValue(ref pickupExp);
        serializer.SerializeValue(ref wavesCompleted);
        serializer.SerializeValue(ref xpThisRun);
        serializer.SerializeValue(ref killsThisRun);
    }

    public void RegisterKill(EnemyController.EnemyType enemyType, int xpGained)
    {
        switch (enemyType)
        {
            case EnemyController.EnemyType.Normal:
                killsNormal++;
                break;
            case EnemyController.EnemyType.Fast:
                killsFast++;
                break;
            case EnemyController.EnemyType.Tank:
                killsTank++;
                break;
            case EnemyController.EnemyType.Shooter:
                killsShooter++;
                break;
        }

        killsThisRun++;
        xpThisRun += xpGained;
        score += xpGained;
    }

    public void RegisterWaveCompleted(int waveNumber, int xpReward)
    {
        wavesCompleted = waveNumber;
        xpThisRun += xpReward;
    }

    public void RegisterPickup(LanPickupType pickupType)
    {
        switch (pickupType)
        {
            case LanPickupType.Health:
                pickupHealth++;
                break;
            case LanPickupType.Shield:
                pickupShield++;
                break;
            case LanPickupType.Ammo:
                pickupAmmo++;
                break;
            case LanPickupType.Exp:
                pickupExp++;
                break;
        }
    }

    public void AddXp(int amount)
    {
        xpThisRun += amount;
        score += amount;
    }
}

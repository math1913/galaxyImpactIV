package com.galaxyimpactv.dto;

import lombok.Getter;
import lombok.Setter;

@Getter @Setter
public class AchievementBatchRequest {
    private Long userId;

    private long killsNormal;
    private long killsFast;
    private long killsTank;
    private long killsShooter;

    private long minutesPlayed;
    private long score;

    private long pickupHealth;
    private long pickupShield;
    private long pickupAmmo;
    private long pickupExp;

    private int wavesCompleted;
}

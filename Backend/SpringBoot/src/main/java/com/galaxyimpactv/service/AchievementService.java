package com.galaxyimpactv.service;

import com.galaxyimpactv.dto.AchievementBatchRequest;
import com.galaxyimpactv.dto.AchievementDTO;
import java.util.List;

public interface AchievementService {

    void addProgress(Long userId, String codigoLogro, long amount);
    void processBatch(AchievementBatchRequest request);
    List<AchievementDTO> getLogrosUsuario(Long userId);
}

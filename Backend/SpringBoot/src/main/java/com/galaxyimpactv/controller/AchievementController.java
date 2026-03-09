package com.galaxyimpactv.controller;

import com.galaxyimpactv.dto.AchievementBatchRequest;
import com.galaxyimpactv.dto.AchievementDTO;
import com.galaxyimpactv.service.AchievementService;
import lombok.RequiredArgsConstructor;

import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/api/achievements")
@RequiredArgsConstructor
@CrossOrigin(origins = "*") // Permite que Unity llame desde cualquier origen
public class AchievementController {

    private final AchievementService achievementService;

    // ------------------------------------------
    // 1) Agregar progreso a un logro
    // ------------------------------------------
    @PostMapping("/progress")
    public void addProgress(
            @RequestParam Long userId,
            @RequestParam String codigoLogro,
            @RequestParam Long cantidad
    ) {
        achievementService.addProgress(userId, codigoLogro, cantidad);
    }

    // ------------------------------------------
    // 2) Obtener logros del usuario
    // ------------------------------------------
    @GetMapping("/user/{idUser}")
    public List<AchievementDTO> getAchievementsByUser(@PathVariable Long idUser) {
        return achievementService.getLogrosUsuario(idUser);
    }

    @PostMapping("/updateBatch")
    public void updateBatch(@RequestBody AchievementBatchRequest request) {

        achievementService.processBatch(request);
    }

}

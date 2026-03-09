package com.galaxyimpactv.dto;

import java.time.LocalDateTime;

import com.galaxyimpactv.model.Achievement;
import com.galaxyimpactv.model.UsuarioLogro;

import lombok.*;

@Getter @Setter
@NoArgsConstructor @AllArgsConstructor
@Builder
public class AchievementDTO {

    private String codigo;
    private String nombre;
    private String descripcion;
    private Long progresoActual;
    private Long objetivo;
    private Boolean completado;
    private Long puntosRecompensa;
    private LocalDateTime fechaDesbloqueo;
    private String categoria;
    private String tipo;

}

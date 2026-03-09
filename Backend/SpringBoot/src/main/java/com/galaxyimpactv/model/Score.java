package com.galaxyimpactv.model;

import jakarta.persistence.*;
import lombok.*;

import java.time.LocalDateTime;

@Entity
@Table(name = "puntuacion")
@Getter @Setter
@NoArgsConstructor @AllArgsConstructor
@Builder
public class Score {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "idPuntuacion")
    private Integer idPuntuacion;

    @Column(name = "nivel") //columna en la base de datos
    private Integer puntos; // nombre en Java = puntos, columna sigue siendo nivel

    @Column(name = "fecha")
    private LocalDateTime fecha;

    @ManyToOne
    @JoinColumn(name = "FK_idUsuario", nullable = false)
    private User usuario;
}

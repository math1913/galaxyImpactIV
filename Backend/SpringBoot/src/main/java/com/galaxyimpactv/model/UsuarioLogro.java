package com.galaxyimpactv.model;

import jakarta.persistence.*;
import lombok.*;
import java.time.LocalDateTime;
import com.fasterxml.jackson.annotation.JsonIgnore;

@Entity
@Table(name = "usuario_logro")
@Getter @Setter
@NoArgsConstructor @AllArgsConstructor
@Builder
public class UsuarioLogro {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "idUsuarioLogro")
    private Long idUsuarioLogro;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "FK_idUsuario", nullable = false)
    @JsonIgnore
    private User usuario;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "FK_idLogro", nullable = false)
    private Achievement logro;

    @Column(name = "progreso_actual", nullable = false)
    private Long progresoActual;

    @Column(name = "completado", nullable = false)
    private Boolean completado;

    @Column(name = "fecha_desbloqueo")
    private LocalDateTime fechaDesbloqueo;
}

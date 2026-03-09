package com.galaxyimpactv.model;

import jakarta.persistence.*;
import lombok.*;
import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.List;
import com.fasterxml.jackson.annotation.JsonIgnore;

@Entity
@Table(name = "usuario")
@Getter @Setter
@NoArgsConstructor @AllArgsConstructor
@Builder
public class User {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "idUsuario")
    private Long idUsuario;

    @Column(name = "username", nullable = false, unique = true, length = 255)
    private String username;

    @Column(name = "email", nullable = false, unique = true, length = 255)
    private String email;

    @Column(name = "password", nullable = false, length = 255)
    private String password;

    @Column(name = "fecha_registro", insertable = false, updatable = false)
    private LocalDateTime fechaRegistro;

    @Column(name = "nivelActual")
    private Integer nivelActual;

    @Column(name = "experiencia")
    private Long experiencia;

    // ✅ Mantener puntuaciones
    @ElementCollection
    @CollectionTable(
            name = "user_puntuaciones",
            joinColumns = @JoinColumn(name = "user_id")
    )
    @Column(name = "puntos")
    private List<Integer> puntuaciones = new ArrayList<>();

    // Relación con logros
    @OneToMany(mappedBy = "usuario", cascade = CascadeType.ALL, orphanRemoval = true)
    @JsonIgnore
    private List<UsuarioLogro> usuarioLogros = new ArrayList<>();
}

package com.galaxyimpactv.repository;
import java.util.Optional;

import com.galaxyimpactv.model.User;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

// Esta interfaz es la "puerta" hacia la base de datos.
// Extiende JpaRepository,que tiene metodos para listar, buscar, crear/actualizar y borrar
@Repository
public interface UserRepository extends JpaRepository<User, Long> {

    // Podemos definir métodos personalizados si queremos (Spring genera las consultas automáticamente)
    Optional<User> findByUsername(String username);
}   
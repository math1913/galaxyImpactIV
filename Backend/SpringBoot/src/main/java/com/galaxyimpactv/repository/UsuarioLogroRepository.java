package com.galaxyimpactv.repository;
import java.util.Optional;
import java.util.List;

import com.galaxyimpactv.model.UsuarioLogro;
import com.galaxyimpactv.model.User;
import com.galaxyimpactv.model.Achievement;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;   

@Repository
public interface UsuarioLogroRepository extends JpaRepository<UsuarioLogro, Long> {
    Optional<UsuarioLogro> findByUsuarioAndLogro(User usuario, Achievement logro);
    List<UsuarioLogro> findByUsuario(User usuario);
}

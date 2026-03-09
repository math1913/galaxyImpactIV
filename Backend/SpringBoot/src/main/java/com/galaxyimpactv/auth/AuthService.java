package com.galaxyimpactv.auth;
import java.util.Optional;
import com.galaxyimpactv.model.User;
import com.galaxyimpactv.repository.UserRepository;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.stereotype.Service;

/**
 * Servicio que valida las credenciales de un usuario.
 */
@Service
public class AuthService {

    private final UserRepository userRepository;
    private final BCryptPasswordEncoder passwordEncoder;

    public AuthService(UserRepository userRepository) {
        this.userRepository = userRepository;
        this.passwordEncoder = new BCryptPasswordEncoder();
    }

    /**
     * Verifica si el usuario existe y si la contraseña es válida.
     */
    public boolean login(String username, String password) {
        // Busca el usuario por nombre (usa tu método existente)
        
        Optional<User>  optionalUser = userRepository.findByUsername(username);
        if (optionalUser.isEmpty()) {
            return false; // Usuario no existe
        }

        User user = optionalUser.get();
        // Compara la contraseña ingresada con el hash guardado
        return passwordEncoder.matches(password, user.getPassword());
    }
}

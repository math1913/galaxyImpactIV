package com.galaxyimpactv.auth;

import com.galaxyimpactv.model.User;
import com.galaxyimpactv.service.UserService;
import java.util.Optional;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.Map;

@RestController
@RequestMapping("/api/auth")
public class AuthController {

    private final AuthService authService;
    private final UserService userService; // <-- Añadido

    public AuthController(AuthService authService, UserService userService) {
        this.authService = authService;
        this.userService = userService; // <-- Guardamos referencia
    }

    @PostMapping("/login")
    public ResponseEntity<Map<String, Object>> login(@RequestBody Map<String, String> loginRequest) {

        String username = loginRequest.get("username");
        String password = loginRequest.get("password");

        boolean success = authService.login(username, password);

        if (success) {

            User user = userService.findByUsername(username)
                    .orElseThrow(() -> new RuntimeException("Usuario no encontrado"));

            return ResponseEntity.ok(Map.of(
                    "status", 200,
                    "message", "Login correcto",
                    "user", username,
                    "id", user.getIdUsuario()   // <-- Aquí devuelves el ID real
            ));
        }

        return ResponseEntity.status(401).body(Map.of(
                "status", 401,
                "error", "Credenciales inválidas"
        ));
    }
}

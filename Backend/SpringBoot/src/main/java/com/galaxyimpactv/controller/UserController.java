package com.galaxyimpactv.controller;

import com.galaxyimpactv.model.User;
import com.galaxyimpactv.service.UserService;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import com.galaxyimpactv.dto.UpdateStatsRequest;

import java.util.List;

@RestController // Indica que esta clase define endpoints REST
@RequestMapping("/api/users") // Ruta base: http://localhost:8080/api/users
@RequiredArgsConstructor // Genera el constructor con los atributos final (evista hacer this.userService = userService, etc)
public class UserController {

    private final UserService userService;

    // GET /api/users -> Lista todos los usuarios
    @GetMapping
    public ResponseEntity<List<User>> getAllUsers() {
        List<User> users = userService.getAllUsers();
        return ResponseEntity.ok(users); // Devuelve lista + código HTTP 200
    }

    // GET /api/users/{id} -> Devuelve un usuario por ID
    @GetMapping("/{id}")
    public ResponseEntity<User> getUserById(@PathVariable Long id) {
        return userService.getUserById(id)
                .map(ResponseEntity::ok)
                .orElse(ResponseEntity.notFound().build()); // 404 si no existe
    }

    // POST /api/users -> Crea un nuevo usuario
    @PostMapping
    public ResponseEntity<User> createUser(@RequestBody User user) {
        User savedUser = userService.saveUser(user);

        // Inicializar todos los logros del usuario
        userService.initializeUserAchievements(savedUser);

        return ResponseEntity.ok(savedUser);
    }


    // DELETE /api/users/{id} -> Elimina un usuario
    @DeleteMapping("/{id}")
    public ResponseEntity<Void> deleteUser(@PathVariable Long id) {
        userService.deleteUser(id);
        return ResponseEntity.noContent().build(); // 204 sin cuerpo
    }

    // POST /api/users/{id}/updateStats -> Actualiza estadísticas del usuario
    @PostMapping("/{id}/updateStats")
    public ResponseEntity<User> updateUserStats(
            @PathVariable Long id,
            @RequestBody UpdateStatsRequest request) {

        User updated = userService.updateStats(id, request.getKills(), request.getXpEarned());
        return ResponseEntity.ok(updated);
    }
}
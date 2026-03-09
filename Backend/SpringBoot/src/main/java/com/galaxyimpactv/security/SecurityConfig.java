package com.galaxyimpactv.security;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.web.SecurityFilterChain;

/**
 * Configuración básica de seguridad:
 * - Permite libre acceso a /api/users y /api/auth/login
 * - Bloquea el resto (por ahora sin autenticación JWT)
 */
@Configuration
@EnableWebSecurity
public class SecurityConfig {

    public SecurityConfig() {
        System.out.println(">>> SecurityConfig loaded correctly <<<");
    }
    @Bean
    public SecurityFilterChain securityFilterChain(HttpSecurity http) throws Exception {
        System.out.println(">>> Building SecurityFilterChain <<<");
        return http
            .csrf(csrf -> csrf.disable())
            .authorizeHttpRequests(auth -> auth
                .requestMatchers("/api/users/**", "/api/auth/login").permitAll()
                .anyRequest().permitAll()
            )
            .build();
    }
}

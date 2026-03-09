
USE galaxy_impact_v;

-- ============================
-- 1️⃣ Eliminar tablas antiguas si existen
-- ============================
SET FOREIGN_KEY_CHECKS = 0;
DROP TABLE IF EXISTS Usuario_Logro;
DROP TABLE IF EXISTS Puntuacion;
DROP TABLE IF EXISTS Logro;
DROP TABLE IF EXISTS Usuario;
DROP TABLE IF EXISTS Progreso;
SET FOREIGN_KEY_CHECKS = 1;

-- ============================
-- 2️⃣ Crear tabla Usuario
-- ============================
CREATE TABLE Usuario (
    idUsuario BIGINT AUTO_INCREMENT PRIMARY KEY,
    Username VARCHAR(100) NOT NULL UNIQUE,
    email VARCHAR(150) NOT NULL UNIQUE,
    password VARCHAR(255) NOT NULL,
    fecha_registro DATETIME DEFAULT CURRENT_TIMESTAMP,
    nivelActual INT DEFAULT 1,
    experiencia BIGINT DEFAULT 0
) ENGINE=InnoDB;

-- ============================
-- 3️⃣ Crear tabla Logro
-- ============================
CREATE TABLE Logro (
    idLogro BIGINT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(150) NOT NULL UNIQUE,
    descripcion VARCHAR(255),
    puntosRecompensa BIGINT DEFAULT 0
) ENGINE=InnoDB;

-- ============================
-- 4️⃣ Crear tabla Puntuacion
-- ============================
CREATE TABLE Puntuacion (
    idPuntuacion BIGINT AUTO_INCREMENT PRIMARY KEY,
    FK_idUsuarios BIGINT NOT NULL,
    nivel INT DEFAULT NULL,
    fecha DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (FK_idUsuarios) REFERENCES Usuario(idUsuario)
        ON DELETE CASCADE
        ON UPDATE CASCADE
) ENGINE=InnoDB;

-- ============================
-- 5️⃣ Crear tabla intermedia Usuario_Logro
-- ============================
CREATE TABLE Usuario_Logro (
    FK_idUsuario BIGINT NOT NULL,
    FK_idLogro BIGINT NOT NULL,
    fecha DATETIME DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (FK_idUsuario, FK_idLogro),
    FOREIGN KEY (FK_idUsuario) REFERENCES Usuario(idUsuario)
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    FOREIGN KEY (FK_idLogro) REFERENCES Logro(idLogro)
        ON DELETE CASCADE
        ON UPDATE CASCADE
) ENGINE=InnoDB;

-- ============================
-- 6️⃣ Índices recomendados
-- ============================
CREATE INDEX idx_puntuacion_usuario ON Puntuacion(FK_idUsuarios);
CREATE INDEX idx_usuario_logro_usuario ON Usuario_Logro(FK_idUsuario);
CREATE INDEX idx_usuario_logro_logro ON Usuario_Logro(FK_idLogro);

-- ============================
-- 7️⃣ Datos de prueba opcionales
-- ============================
INSERT INTO Usuario (Username, email, password, nivelActual, experiencia)
VALUES
('player1', 'player1@example.com', 'hash123', 3, 1500),
('player2', 'player2@example.com', 'hash456', 1, 200);

INSERT INTO Logro (nombre, descripcion, puntosRecompensa)
VALUES
('Primer disparo', 'Realiza tu primer disparo', 10),
('Derrota 100 enemigos', 'Elimina 100 enemigos', 50);

INSERT INTO Puntuacion (FK_idUsuarios, nivel)
VALUES
(1, 3),
(2, 1);

INSERT INTO Usuario_Logro (FK_idUsuario, FK_idLogro)
VALUES
(1, 1),
(1, 2);

-- ===========================================
-- FIN DEL SCRIPT
-- ===========================================
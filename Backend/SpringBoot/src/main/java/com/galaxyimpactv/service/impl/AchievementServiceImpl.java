package com.galaxyimpactv.service.impl;

import com.galaxyimpactv.dto.AchievementBatchRequest;
import com.galaxyimpactv.dto.AchievementDTO;
import com.galaxyimpactv.model.*;
import com.galaxyimpactv.repository.*;

import com.galaxyimpactv.service.AchievementService;
import com.galaxyimpactv.service.UserService;

import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import java.util.Comparator;
import java.time.LocalDateTime;
import java.util.List;
import java.util.stream.Collectors;

@Service
@RequiredArgsConstructor
public class AchievementServiceImpl implements AchievementService {

    private final UserRepository userRepository;
    private final AchievementRepository achievementRepository;
    private final UsuarioLogroRepository usuarioLogroRepository;
    private final UserService userService;
	private static final List<String> CATEGORIA_ORDEN = List.of(
			"KILL",
			"WAVES",
			"TIME",
			"SCORE",
			"PICKUP",
			"LEVEL"
	);

	@Override
	public void addProgress(Long userId, String codigoLogro, long amount) {

		// Buscar usuario
		User usuario = userRepository.findById(userId)  
				.orElseThrow(() -> new RuntimeException("Usuario no encontrado"));

		// Buscar logro
		Achievement logro = achievementRepository.findByCodigo(codigoLogro)
				.orElseThrow(() -> new RuntimeException("Logro no encontrado: " + codigoLogro));

		// Buscar progreso usuario-logro
		UsuarioLogro usuarioLogro = usuarioLogroRepository
				.findByUsuarioAndLogro(usuario, logro)
				.orElse(null);

		if (usuarioLogro == null) {
				usuarioLogro = UsuarioLogro.builder()
						.usuario(usuario)
						.logro(logro)
						.progresoActual(0L)
						.completado(false)
						.build();
		}

		// Sumar progreso
		long nuevoProgreso = usuarioLogro.getProgresoActual() + amount;
		usuarioLogro.setProgresoActual(nuevoProgreso);

		// COMPLETADO
		if (!usuarioLogro.getCompletado() && nuevoProgreso >= logro.getObjetivo()) {

				usuarioLogro.setCompletado(true);
				usuarioLogro.setFechaDesbloqueo(LocalDateTime.now());
				// Actualizar puntos de experiencia del usuario
				userService.updateStats(userId, 0, logro.getPuntosRecompensa());
		}

		usuarioLogroRepository.save(usuarioLogro);
    }

	private AchievementDTO mapToDTO(UsuarioLogro ul) {

		Achievement logro = ul.getLogro();
		String[] partes = logro.getCodigo().split("_");

		String categoria = partes.length > 0 ? partes[0] : "GENERAL";
		String tipo      = partes.length > 1 ? partes[1] : "NONE";

		return AchievementDTO.builder()
				.codigo(logro.getCodigo())
				.nombre(logro.getNombre())
				.descripcion(logro.getDescripcion())
				.objetivo(logro.getObjetivo())
				.progresoActual(ul.getProgresoActual())
				.completado(ul.getCompletado())
				.puntosRecompensa(logro.getPuntosRecompensa())
				.fechaDesbloqueo(ul.getFechaDesbloqueo())
				.categoria(categoria)
				.tipo(tipo)
				.build();
	}
    @Override
	public List<AchievementDTO> getLogrosUsuario(Long idUser) {

		// 1) Cargar el usuario (necesario para el findByUsuario)
		User usuario = userRepository.findById(idUser)
				.orElseThrow(() -> new RuntimeException("Usuario no encontrado: " + idUser));

		// 2) Obtener todos los usuario_logro del usuario
		List<UsuarioLogro> lista = usuarioLogroRepository.findByUsuario(usuario);

		// 3) Convertir a DTOs
		List<AchievementDTO> dtos = lista.stream()
				.map(this::mapToDTO)
				.collect(Collectors.toList());

		// 4) Ordenar logros según reglas
		dtos.sort(
			Comparator
				.comparing((AchievementDTO dto) -> CATEGORIA_ORDEN.indexOf(dto.getCategoria()))
				.thenComparing(AchievementDTO::getTipo)
				.thenComparing(dto -> extractLastNumber(dto.getCodigo()))
		);

		return dtos;
	}

	@Override
	public void processBatch(AchievementBatchRequest r) {

		Long userId = r.getUserId();

		//Kills totales
		long killsTotal = 
			r.getKillsNormal() 
			+ r.getKillsFast() 
			+ r.getKillsTank() 
			+ r.getKillsShooter();

		addProgress(userId, "KILL_TOTAL_100", killsTotal);
		addProgress(userId, "KILL_TOTAL_500", killsTotal);
		addProgress(userId, "KILL_TOTAL_1000", killsTotal);
		addProgress(userId, "KILL_TOTAL_2500", killsTotal);
		addProgress(userId, "KILL_TOTAL_5000", killsTotal);

		// Kills normales
		addProgress(userId, "KILL_NORMAL_100", r.getKillsNormal());
		addProgress(userId, "KILL_NORMAL_300", r.getKillsNormal());
		addProgress(userId, "KILL_NORMAL_700", r.getKillsNormal());
		addProgress(userId, "KILL_NORMAL_1500", r.getKillsNormal());

		// Kills fast
		addProgress(userId, "KILL_FAST_50", r.getKillsFast());
		addProgress(userId, "KILL_FAST_150", r.getKillsFast());
		addProgress(userId, "KILL_FAST_400", r.getKillsFast());
		addProgress(userId, "KILL_FAST_800", r.getKillsFast());

		//Kills tank
		addProgress(userId, "KILL_TANK_25", r.getKillsTank());
		addProgress(userId, "KILL_TANK_75", r.getKillsTank());
		addProgress(userId, "KILL_TANK_200", r.getKillsTank());
		addProgress(userId, "KILL_TANK_400", r.getKillsTank());

		// Kills shooter
		addProgress(userId, "KILL_SHOOTER_10", r.getKillsShooter());
		addProgress(userId, "KILL_SHOOTER_40", r.getKillsShooter());
		addProgress(userId, "KILL_SHOOTER_100", r.getKillsShooter());
		addProgress(userId, "KILL_SHOOTER_250", r.getKillsShooter());

		// tiempo jugado
		addProgress(userId, "TIME_SURVIVE_30", r.getMinutesPlayed());
		addProgress(userId, "TIME_SURVIVE_60", r.getMinutesPlayed());
		addProgress(userId, "TIME_SURVIVE_180", r.getMinutesPlayed());
		addProgress(userId, "TIME_SURVIVE_360", r.getMinutesPlayed());
		addProgress(userId, "TIME_SURVIVE_720", r.getMinutesPlayed());

		// Score
		addProgress(userId, "SCORE_SINGLE_200", r.getScore());
		addProgress(userId, "SCORE_SINGLE_500", r.getScore());
		addProgress(userId, "SCORE_SINGLE_800", r.getScore());
		addProgress(userId, "SCORE_SINGLE_1200", r.getScore());
		addProgress(userId, "SCORE_SINGLE_1600", r.getScore());
		addProgress(userId, "SCORE_SINGLE_2500", r.getScore());

		// Pickups
		addProgress(userId, "PICKUP_HEALTH_25", r.getPickupHealth());
		addProgress(userId, "PICKUP_HEALTH_100", r.getPickupHealth());
		addProgress(userId, "PICKUP_HEALTH_250", r.getPickupHealth());

		addProgress(userId, "PICKUP_SHIELD_25", r.getPickupShield());
		addProgress(userId, "PICKUP_SHIELD_100", r.getPickupShield());
		addProgress(userId, "PICKUP_SHIELD_250", r.getPickupShield());

		addProgress(userId, "PICKUP_AMMO_50", r.getPickupAmmo());
		addProgress(userId, "PICKUP_AMMO_200", r.getPickupAmmo());
		addProgress(userId, "PICKUP_AMMO_500", r.getPickupAmmo());

		addProgress(userId, "PICKUP_EXP_50", r.getPickupExp());
		addProgress(userId, "PICKUP_EXP_200", r.getPickupExp());
		addProgress(userId, "PICKUP_EXP_500", r.getPickupExp());

		// Waves totales completadas
		addProgress(userId, "WAVES_TOTAL_10", r.getWavesCompleted());
		addProgress(userId, "WAVES_TOTAL_25", r.getWavesCompleted());
		addProgress(userId, "WAVES_TOTAL_50", r.getWavesCompleted());
		addProgress(userId, "WAVES_TOTAL_100", r.getWavesCompleted());
		addProgress(userId, "WAVES_TOTAL_200", r.getWavesCompleted());
		addProgress(userId, "WAVES_TOTAL_500", r.getWavesCompleted());

		User usuario = userRepository.findById(userId)
			.orElseThrow(() -> new RuntimeException("Usuario no encontrado: " + userId));

		int nivelActual = usuario.getNivelActual();

		setProgress(userId, "LEVEL_REACH_10", nivelActual);
		setProgress(userId, "LEVEL_REACH_30", nivelActual);
		setProgress(userId, "LEVEL_REACH_100", nivelActual);
		setProgress(userId, "LEVEL_REACH_500", nivelActual);
	}
	private void setProgress(Long userId, String codigoLogro, long value) {

		User usuario = userRepository.findById(userId)
				.orElseThrow(() -> new RuntimeException("Usuario no encontrado"));

		Achievement logro = achievementRepository.findByCodigo(codigoLogro)
				.orElseThrow(() -> new RuntimeException("Logro no encontrado: " + codigoLogro));

		UsuarioLogro usuarioLogro = usuarioLogroRepository.findByUsuarioAndLogro(usuario, logro)
				.orElse(null);

		if (usuarioLogro == null) {
			usuarioLogro = UsuarioLogro.builder()
					.usuario(usuario)
					.logro(logro)
					.progresoActual(0L)
					.completado(false)
					.build();
		}

		// Setear progreso exacto (sin sumar)
		usuarioLogro.setProgresoActual(value);

		// Marcar completado si corresponde
		if (!usuarioLogro.getCompletado() && value >= logro.getObjetivo()) {
			usuarioLogro.setCompletado(true);
			usuarioLogro.setFechaDesbloqueo(LocalDateTime.now());
			userService.updateStats(userId, 0, logro.getPuntosRecompensa());
		}

		usuarioLogroRepository.save(usuarioLogro);
	}

	private int extractLastNumber(String code) {
		String[] parts = code.split("_");
		return Integer.parseInt(parts[parts.length - 1]);
	}


}

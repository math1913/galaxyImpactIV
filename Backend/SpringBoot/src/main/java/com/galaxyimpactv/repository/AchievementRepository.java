package com.galaxyimpactv.repository;
import java.util.Optional;

import com.galaxyimpactv.model.Achievement;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface AchievementRepository extends JpaRepository<Achievement, Long> {
    Optional<Achievement> findByCodigo(String codigo);
}

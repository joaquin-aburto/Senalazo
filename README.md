#  Señalazo — Videojuego Inclusivo de Lenguaje de Señas

Señalazo es un videojuego multijugador inspirado en la **lotería mexicana** que enseña el abecedario del **Lenguaje de Señas Mexicano (LSM)** de forma interactiva. En lugar de escuchar las cartas, los jugadores deben realizar la seña correspondiente en tiempo real usando un guante inteligente con sensores.

> Proyecto de titulación — Tecnólogo en Desarrollo de Software  
> **CETI Tonalá, 2025**

##  Motivación

En México, el 33.4% de mujeres y el 34.4% de hombres con discapacidad reportaron haber sido discriminados en 2022 (ENADIS). Señalazo nace como respuesta a esa realidad: un videojuego accesible que fomenta la inclusión y el aprendizaje del LSM de manera natural y divertida.

##  Funcionalidades

-  Videojuego multijugador estilo lotería mexicana en tiempo real
-  IA personalizada por usuario que se adapta a la forma de hacer señas de cada jugador
-  Guante inteligente con sensores de flexión y movimiento vía Bluetooth
-  Versión especial para personas con hipoacusia con animaciones adaptadas
-  Sistema de seguridad basado en servicios de Amazon (AWS)
-  Sincronización en tiempo real entre el guante y el videojuego

## Tecnologías

| Área | Tecnología |
|------|-----------|
| Motor gráfico | Unity (C#) |
| Hardware | Guante con sensores de flexión + Bluetooth |
| IA / ML | Python (scikit-learn / TensorFlow) |
| Backend / Seguridad | Amazon Web Services (AWS) |
| Diseño visual | Adobe (Illustrator / Photoshop) |

##  Arquitectura del sistema

```
Guante inteligente (sensores de flexión)
        ↓ Bluetooth
Módulo de captura de señas
        ↓ Python (IA personalizada por usuario)
Detección y validación de seña
        ↓
Motor de juego Unity ←→ AWS (sincronización multijugador + seguridad)
```

##  Equipo

- **Alondra Silva López**
- **Joaquin Aburto Sanchez**
- **Maximiliano Ruiz Romero**

Asesora: MTI. Celia Guadalupe Hernandez Arteaga

⚠️ Este repositorio contiene únicamente el código de Unity (C#). 
> El módulo de Python (IA) y la configuración de AWS no están incluidos.
---

> *"Cada partida de Señalazo es un acto de compromiso con la construcción de una sociedad más justa y empática."*

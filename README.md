# 🐟 Pescadojam

Juego de peces de **un solo clic** para game jam, hecho en **Godot 4**.

## De qué va

Un anzuelo (el cursor) gira alrededor de un círculo. Con **un solo clic** hay que enganchar los objetivos correctos en el momento justo: si el cursor los toca al pulsar, cuentan; si no, se falla.

## Cómo ejecutar

1. Instalar [Godot 4.7](https://godotengine.org/download) (o superior 4.x).
2. Abrir Godot → **Importar** → seleccionar `project.godot` de esta carpeta.
3. Pulsar **F5** para ejecutar.

> El proyecto usa el renderer **GL Compatibility**.

## Controles

| Acción | Entrada |
|---|---|
| Enganchar (acción principal) | `K`, `R` o clic (acción `player_action` / `click_debug`) |

## Estructura del proyecto

```
├── project.godot        # Configuración del proyecto
├── main.tscn / main.gd  # Escena principal: cursor giratorio, spawn de objetivos
├── correcta.tscn / correcta.gd  # Objetivo que se puede enganchar (CorrectaButton)
└── *.png / *.svg        # Arte (anzuelo, ilustraciones, icono)
```
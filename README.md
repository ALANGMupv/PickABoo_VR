# PickABoo VR

Versión de realidad virtual de Pick A Boo. Parte de la casa que ya usábamos en la versión AR ([PickABoo_AR](https://github.com/ALANGMupv/PickABoo_AR)) y se irá ampliando sprint a sprint.

## Estado actual

Por ahora el proyecto contiene la escena `MainDemo` con la casa completa (prefab `Level A`), su iluminación horneada y el post-procesado, tal y como estaban en el proyecto AR. Todavía no hay mecánicas de juego.

## Requisitos

- Unity **6000.3.24f1** (Unity 6.3 LTS)
- Universal Render Pipeline 17.3 (se instala solo con el `manifest.json`)

Al abrir el proyecto, carga `Assets/PickABoo/Scenes/MainDemo.unity`.

## Estructura de `Assets`

```
__pruebas/              escenas de prueba de cada uno
PickABoo/
  Scenes/               escenas del juego (MainDemo y su carpeta de lightmaps)
  AssetCasa/            todo lo que necesita la casa
    Modelos3D/  Materiales/  Texturas/  Shaders/
    Prefabs/    Iluminacion/  Scripts/
  UI/  HUD/  Sonido/  Animaciones/
Settings/               URP_Asset y renderer del proyecto
```

Los scripts se organizan por funcionalidad: cada una tiene su carpeta dentro de `PickABoo` con su propia subcarpeta `Scripts` (por ejemplo, `PickABoo/CazarFantasmas/Scripts`).

## Ramas

Usamos GitFlow:

- `main`: versiones entregadas.
- `develop`: integración del sprint en curso.
- `feature/<nombre>`: una rama por tarea, que sale de `develop` y vuelve a `develop`.
- `release/` y `hotfix/` para preparar entregas y arreglos urgentes.

## Equipo

[ALANGMupv](https://github.com/ALANGMupv) · [exby04](https://github.com/exby04) · [GreysyBurgos](https://github.com/GreysyBurgos) · [NereaAguilar](https://github.com/NereaAguilar) · [rigg-ops](https://github.com/rigg-ops)

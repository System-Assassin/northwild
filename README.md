# Northwild HDRP

Northwild HDRP is the high-fidelity edition of the Windows-first, single-player bushcraft survival prototype. It uses a real 1 km², one-metre-sample terrain crop from Nedre Roasten in Femundsmarka, Norway, alongside physically based sky rendering, true volumetric cloud volumes, cloud shadows, volumetric fog, weather-driven cloud presets, HDRP lighting, 2K PBR surfaces and original alpha-clipped boreal vegetation.

## Recommended editor

Unity 6.5 (`6000.5.0f1` or a newer Unity 6.5 patch). The project uses Unity's official High Definition Render Pipeline package `17.5.0`; Unity downloads it during the first import. No paid assets are required.

The default cloud quality is tuned for a modern desktop GPU such as the Radeon RX 7900 GRE with 16 GB VRAM.

## Start the prototype

1. Extract the project.
2. In Unity Hub, select **Add > Add project from disk** and choose the `Northwild_HDRP` folder.
3. Open it with Unity 6.5. Let the first HDRP package import and shader compilation finish; this can take several minutes.
4. From Unity's top menu, select **Northwild > Create HDRP Prototype Scene**.
5. Open `Assets/Northwild/Scenes/Prototype.unity` if it is not already open.
6. Press **Play**.

HDRP configures itself on first open. If Unity was interrupted during import, select **Northwild > HDRP > Repair Project Setup**, wait for compilation to finish, then create the scene again.

The clear-weather lighting uses a neutral white sun, controlled HDRP exposure and a blue-grey Scandinavian sky gradient. Volumetric clouds remain fully enabled, with natural warm tones limited to sunrise and sunset.

The texture importer configures the supplied normal maps, HDRP mask maps and coverage-preserving vegetation alpha automatically. If a texture ever appears flat, glossy or boxed-in after an interrupted import, select **Northwild > HDRP > Reimport PBR Textures** once.

The visual realism pass replaces the original sphere trees and capsule player with layered Norway spruce and downy birch crowns, batched bilberry/fern undergrowth, exposed-rock terrain blending, fallen timber, cut-ended firewood, detailed gatherable props, a layered-bough shelter and an articulated third-person survivor with a walking gait. Trees respond subtly to the live wind model. Snow now builds into visible ground cover over roughly 15 seconds of snowfall and melts gradually when the weather clears. Nedre Roasten uses an animated 65×65 wave grid together with wind-scrolled multi-scale ripple normals instead of a static flat surface.

High-quality TAA stabilises fine needles and branches. The player camera now renders the complete 1 km terrain and uses a safer near clipping plane, while weather-dependent volumetric fog replaces the old hard 400 m horizon.

The measured heightmap preserves the real shoreline, islands and land relief. A shallow synthetic lake bed was added beneath the surveyed water surface so the HDRP water plane renders cleanly. Source details and attribution are in `Assets/Northwild/SourceData/Heightmaps/README.md`.

## Controls

| Key | Action |
| --- | --- |
| WASD | Move |
| Mouse | Look |
| Left Shift | Sprint |
| Left Ctrl | Crouch |
| C | Switch first/third person |
| E | Gather or interact |
| Tab | Inventory |
| B | Build a stone fire ring |
| H | Build a lean-to shelter |
| T | Add birch-bark tinder to nearby fire |
| K | Add twig kindling to nearby fire |
| L | Add a stick/log to nearby fire |
| I | Ignite nearby prepared fire |
| P | Boil one unit of raw water at a lit fire |
| R | Drink one unit of safe water |
| O | Eat gathered cloudberries |
| F5 | Save |
| F9 | Load |
| Escape | Release/capture mouse |
| F6 | Cycle volumetric clouds, fog and precipitation weather |
| F7 | Place a lit test fire in the Unity Editor |

## First playable loop

- Explore the woodland and identify useful ground resources.
- Collect birch bark, twigs, sticks, logs and stones.
- Collect unsafe water at the lake.
- Build and prepare a fire correctly: tinder, kindling, then fuel.
- Protect the fire from precipitation and boil the water before drinking.
- Manage core temperature, wetness, hydration, calories and fatigue.
- Use the lean-to shelter to reduce wind chill and precipitation exposure.
- Survive the changing Scandinavian weather and day/night cycle.

## Prototype status

This is a systems-first HDRP vertical slice. The survival logic, measured terrain, PBR surfaces and first environmental-art pass are functional. The new vegetation is performance-conscious instanced/batched card geometry and the third-person survivor is an articulated procedural model, so a fully rigged production character, hand interactions, wildlife and spatial sound remain future milestones.

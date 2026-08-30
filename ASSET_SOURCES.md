# Northwild free asset plan

The project deliberately keeps third-party Unity Asset Store packages out of
the repository until their licence and redistribution requirements have been
checked. Add them to the local Unity project through Package Manager rather
than copying a downloaded `.unitypackage` into Git.

## Recommended first pack

### Unity Terrain - HDRP Demo Scene

- Publisher: Unity Technologies
- Cost: Free
- Download size: 2.9 GB
- Unity 6 / HDRP: Compatible
- Useful content: six SpeedTree models, terrain materials and GPU-instanced
  terrain detail examples
- Store page:
  https://assetstore.unity.com/packages/3d/environments/unity-terrain-hdrp-demo-scene-213198

This is the safest starting point because its vegetation is already authored
for HDRP. Import the environment assets required by Northwild rather than
replacing the Northwild project settings or opening the supplied demo as the
main game scene.

## Useful supporting assets

### Vegetation Spawner - FREE

- Publisher: Staggart Creations
- Cost: Free
- Download size: 3.5 MB
- Unity 6 / HDRP: Compatible
- Store page:
  https://assetstore.unity.com/packages/tools/terrain/vegetation-spawner-free-automatic-tree-grass-placement-177192

Northwild already has deterministic runtime placement, so this tool is optional
and is mainly useful for hand-tuning vegetation in the Editor.

### Poly Haven

- Cost and licence: CC0; commercial use and redistribution permitted
- Best uses here: mossy rocks, fallen logs, shoreline stones and additional PBR
  terrain materials
- Models: https://polyhaven.com/models/nature/rocks-stone
- Licence: https://polyhaven.com/license

Use 1K or 2K game-ready downloads first. The 4K/8K photogrammetry versions are
unnecessarily heavy for repeated open-world props unless lower LODs are made.

## Packs requiring conversion

`Conifers [BOTD]`, `Foliage Pack Free` and `Nature Starter Kit 2` are free, but
their original Unity versions and shaders predate current HDRP. They may import
with pink materials and should not be the first choice for this project.

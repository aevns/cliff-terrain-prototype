# Terrain Generation with Integrated Cliffs

The goal of this project is to create a proof of concept for a terrain generation method that integrates sheer cliffs into heightmap based terrain.
Smooth cliff faces are generated in an otherwise grid-based terrain system by identifying conditions for each grid space which determine if and how a cliff section of a given type should be included.
While current implementation is limited to simple walls, more complex art could be used to created detailed cliffs using the same premise.

The project is motivated primarily by the importance of terrain features in real-time strategy games such as Supreme Commander and Planetary Annihilation.
Heightmap based terrain generation is used in many such games, and terrain features can have a significant impact on gameplay and strategies in these games.
Planetary Annihilation uses CSG-based features to introduce novel complexity to its terrain in ways not commonly found in these games.
Unfortunately, such a system requires using pre-fabricated features, heavily limiting the ability to generate complex, organic terrain features.
Inspired by the CSG approach, the intent of this project is to achieve similarly complex features directly from heightmap data, without the limitations of the CSG approach.

![example image 1](https://bytebucket.org/snippets/aevns/Ke9rpn/raw/c4be966f0b02cf69326269546c8db17668639972/terrainexample.png "testtitle")

*An example of generated terrain featuring layered cliffs and spiralling slopes.*

![example image 2](https://bytebucket.org/snippets/aevns/reznX7/raw/f715bb6285bd481d470c92bb65f659f620805030/dungeonexample.png)

*An example of dungeon-like terrain with its associated heightmap.
The heightmap used is shown at full resolution, demonstrating the accuracy of wall and cliff generation.*

# General Usage

The prefab *TerrainSystem.prefab* should be included in a scene to use the system, and its settings options are as follows:

![settings image 1](https://bytebucket.org/snippets/aevns/pebebX/raw/b91eab8bfdae66e507235feb290a867cbf6cc8f5/prefabsettings.png)

## Image Sampler

Two different types of terrain samplers are included with the system, which generate data needed by the terrain system.
These are both image samplers:

* The fast image sampler samples an image at a fixed 1:2 resolution. This is done because a 1:1 sampling of the image has insufficient degrees of freedom for the full solution space of possible cell types and shapes to exist.

* The slow image sampler is able to sample images at arbitrary resolutions and offsets, although doing so is relatively slow.

## Terrain Builder

This component contains references to meshes required for the system to work, and defines how the mesh construction takes place.
The referenced meshes currently require very specific properties for the system to work. For this reason, these references are set by default in the TerrainSystem prefab, and should not be changed.

## Terrain System

This component defines the parameters of the terrain generation, and acts as a factory for generated terrain cells.
Its settings are:

* ***Vertical Scale:*** The height scaling of the generated terrain.

* ***Cliff Height:*** The minimum height difference between vertices in the terrain past which cliffs will be generated. This can also be thought of as the steepest rate of ascent allowed.

* ***Layering:*** This parameter contols regions over which walls below the maximum cliff height can still exist. When set to 0, it has no effect. When set to 1, cliffs will exist everywhere the terrain passes a multiple of the Cliff Height, much like a contour map.

* ***Map Width, Length:*** Dimensions of the generated terrain.

* ***Block Size:*** Maximum size of a given terrain cell (Block Size x Block Size)

* ***Materials:*** Materials used for the ground and walls.

* ***Outline Thickness:*** This parameter controls the way the 2nd UV map is generated for walls. This UV map is used to texture outlines along the top and bottom of walls (sample materials are included for this), and the Outline Thickness setting controls the thickness of these lines.

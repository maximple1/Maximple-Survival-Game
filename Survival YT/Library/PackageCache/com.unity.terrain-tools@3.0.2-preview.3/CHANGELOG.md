# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [3.0.2-preview.3] - 2020-11-04
### Fixed
- Fixed regression with SetHeight Flatten Tile option
- Fixed brush mask filters with SetHeight painting

## [3.0.2-preview.2] - 2020-11-02
### Added
- Editor Analytics integration.
- Fallback shader to the HDRP Visualization shader.
- PNG/TGA heightmaps support level remapping and vertical flipping.
- Create New Terrain supports heightmaps in Texture2D format.

### Changed
- Changed the package name that's used to get core Universal Render Pipeline (URP) HLSL code from Lightweight to Universal.
- Added missing Terrain visualization settings.
- Removed Filter Stack Image assets.
- Changed Base Map Maximum Distance to 20000 to match the range in the Terrain Inspector.
- Terrain Toolbox splatmap modifications are now applied to Terrain only after you click **Apply To Terrain** in the Terrain Toolbox.

### Fixed
#### Brushes
- Brush sizes are clamped to a maximum based on texture size. ([1276290](https://issuetracker.unity3d.com/product/unity/issues/guid/1276290/))
- Clone tool correctly blends heightmap.
- Clone tool clones alphamaps as well as height.
- Clone tool correctly maintains clone position when you rotate the Scene Camera or click on the UI.
- Corrected Smooth tool behavior when you paint near the edges of Terrain tiles so that it produces less discontinuities. ([1186005](https://issuetracker.unity3d.com/issues/terrain-tools-terrain-gets-terraced-when-smoothed-near-the-edge-of-the-terrain-tile))
- Removed artifacts that occurred when you used the Twist Brush. ([1276448](https://issuetracker.unity3d.com/issues/terrain-tools-twist-tool-certain-values-cause-value-spikes-in-the-heightmap))
- Added support for Brush Mask Filters to the Wind Erosion tool.

#### Splatmaps
- Corrected blending when you apply splatmaps from the Terrain Toolbox.
- Fixed error thrown when you highlight a splatmap in the Terrain Toolbox.
- Fixed Undo/Redo functions after you apply a splatmap to a Terrain from the Terrain Toolbox.
- Fixed error thrown when you export a splatmap to a Terrain without selecting a Terrain. ([1240314](https://issuetracker.unity3d.com/issues/terrain-tools-terrain-toolbox-splatmap-export-to-terrain-throws-dividebyzeroexception-when-no-terrain-is-selected))
- Fixed Flip Adjustment on splatmap. ([1276204](https://issuetracker.unity3d.com/issues/terrain-tools-utilities-splatmap-flip-horizontal-and-flip-vertical-are-reversed))

#### Heightmaps
- Corrected heightmap when you create Terrain using a RAW file type in the Terrain Toolbox.
- Fixed Flip Axis when you use Import Heightmap to create Terrain.
- Heightmaps can be exported in 8-bit mode from the Terrain Toolbox. ([1276244](https://issuetracker.unity3d.com/issues/terrain-tools-toolbox-utilities-export-heightmaps-heightmap-depth-always-resets-to-16-bit))
- Terrain Toolbox heightmap export properly remaps levels.

#### Noise Editor
- Noise Editor displays correctly when you use the Personal (light) theme.
- Noise shader import works correctly on Linux. ([1188556](https://issuetracker.unity3d.com/issues/linux-adding-terrain-tools-package-makes-an-endless-loop-of-importing))
- You can now open only one Export Noise window at a time. ([1269540](https://issuetracker.unity3d.com/issues/terraintools-new-instance-of-export-noice-to-texture-window-is-created-every-time-even-though-another-is-already-open))
- Removed non-functional normalize option from the Export Noise UI.
- Unique asset path is generated when you export noise.
- Removed shader warnings from noise shaders.

#### Terrain Splitting
- Details and Trees are preserved when you split Terrain in the Terrain Toolbox. ([1248489](https://issuetracker.unity3d.com/product/unity/issues/guid/1248489/))
- Splitting Terrain with resolution of 33x33 warns the user and allows for an alternate split mode. ([1276273](https://issuetracker.unity3d.com/issues/terrain-tools-toolbox-utilities-splitting-terrain-with-heightmap-resolution-33-breaks-terrain-and-causes-error))
- Scenes now don't automatically save when you split Terrain in the Terrain Toolbox.

#### Additional Fixes
- Editing the Remap Curve in the Image Filter Stack no longer throws error. ([1254251](https://issuetracker.unity3d.com/issues/terraintools-nullreferenceexception-is-thrown-on-editing-the-remap-curve-in-the-image-filter-stack))
- Reverting to values in a Terrain preset now correctly reverts to saved values.
- Base texture size now appears correctly in the Terrain Inspector drop-down menu for Terrains you create with the Terrain Toolbox.
- Fixed toggle button behavior in the Terrain Toolbox, Brushes, and Brush Mask Filters.

## [3.0.1-preview] - 2020-02-06

- Removed test meta file

## [3.0.0-preview] - 2020-01-30

- Added Common Brush Controls and Brush Mask Filters to Paint Holes tool
- Added Terrain Visualization tool support for Universal Render Pipeline
- Added world space height support for Set Height tool
- Fixed bugs for foldout UI
- Toolbox Split Terrain tool bug fixing and added support for terrain holes
- Added terrain holes support to Toolbox Heightmap resolution change

## [2.0.0-preview] - 2019-08-05
- Brush Mask Filter Stack and Filters
- Added Brush Mask Filter Stack to each tool
- New noise type "Strata"
- Noise Filter
- Noise Editor Window
- Wind Erosion bug fixes. Looks great!
- TerrainToolbox Material Updates
- Can import splatmaps with the Terrain Toolbox
- Bug fixing for Terrain Toolbox Gizmo
- Terrain visualization utilies in Terrain Toolbox
- Improved Paint Texture tool. Now uses brush controllers for size, rotation, etc.
- Reorderable Layer List/Palette Assets
- Eyedropper feature for Paint Texture Tool that selects the most prominent Terrain Layer in a given area
- Fixed Mesh Stamp Tool
- Rotation for Mesh Stamp now treats brush rotation and mesh rotation as one transformation
- Fixed depth for Mesh Stamp
- Moved Mesh to RenderTexture to public API for folks to use in their tools
- Ability to generate noise based on input Texture. Noise Filter uses this to pipe the heightmap into the noise generation for another way of doing strata
- Options to "Reset" brush settings to defaults added on the Tool foldout headers
- Removed AssetDatabase.Refresh from static constructor of NoiseLib
- LOTS of bug fixing

## [1.1.4-preview] - 2019-05-22
- Updating Mesh Stamp Tool to use Brush Controllers for size, rotation, etc.
- Fix errors with Noise shader generation writing to read-only files and manually setting locale (so commas dont get used for decimals)

## [1.1.2-preview] - 2019-05-22
- Removing TestRunnerOptions.json

## [1.1.1-preview] - 2019-05-22
- Removed Samples directory

## [1.1.0-preview] - 2019-05-22
- Added more automated tests
- Deprecated terrain material type for users on 2019.2 in terrain toolbox

## [1.0.0-preview] - 2019-05-17

### This is the first release of *Unity Package \<Terrain Tools\>*.
- Terrain Tools package released in preview: Terrain Tools package helps improve the workflow for creating Terrain in Unity. It includes a number of brand new sculpting Brushes, and a collection of terrain tools in a new Terrain Toolbox to help automate terrain workflows.
- New sculpting brushes.
- New Terrain Toolbox.
- Initial package manual and documentations.


## [0.1.0-preview] - 2019-04-30

- Testing publish pipeline of Unity Package <Terrain Tools>.

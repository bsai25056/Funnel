# Funnel and Filter Paper System

A Unity laboratory simulation module containing an interactive funnel and filter paper.

## Features

- Transparent 3D funnel
- Removable filter paper
- Mouse dragging for both objects
- Filter paper snaps back when released near the funnel
- Gradual liquid flow through the funnel
- Filtrate and residue separation
- Residue amount and type tracking
- Marked liquid entry and exit ports
- Prefabs ready for reuse in other Unity projects

## Mouse Controls

1. Enter Play Mode.
2. Hold the left mouse button on the funnel or filter paper.
3. Move the mouse to drag it.
4. Release the filter paper near the funnel to attach it again.

## Main Interfaces

### Funnel

- `ReceiveLiquid(liquidData)`
- `IsFilterAttached()`
- `SetFilterAttached(bool)`

### Filter Paper

- `ProcessLiquid(liquidData)`
- `GetResidue()`
- `OnSnappedToFunnel(funnel)`
- `OnDetachedFromFunnel()`

## Testing

The included test scene demonstrates contaminated liquid containing suspended solid particles.

Expected result:

- Clean liquid passes through as filtrate.
- Solid impurities remain on the filter paper as residue.
- Liquid volume is conserved.

## Adding It to Another Unity Project

Copy these folders into the other project’s `Assets` folder:

- `Assets/Prefabs/Funnel`
- `Assets/Prefabs/FilterPaper`
- `Assets/Scripts/Funnel`
- `Assets/Scripts/FilterPaper`
- `Assets/Scripts/Interaction`
- `Assets/Models/Funnel`
- `Assets/Models/FilterPaper`
- `Assets/Materials/Funnel`
- `Assets/Materials/FilterPaper`

Keep every `.meta` file when copying.

Drag `Funnel.prefab` and `FilterPaper.prefab` into the required scene.

## Requirements

- Unity 6
- Universal Render Pipeline
- Unity Input System

## Practical Applications

- Copper sulphate solution and solid impurity separation
- Softened water and calcium precipitate separation

<p align="center">
  <img src="docs/images/app-icon-rounded.png" width="160" alt="AR Kitchen icon">
</p>

<h1 align="center">AR Kitchen</h1>

<p align="center">
  Visualize a real kitchen in a client's space, in AR, before a single cabinet is installed.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Unity-6000.3.13f1-black?logo=unity" alt="Unity 6000.3.13f1">
  <img src="https://img.shields.io/badge/Render-URP%20(Performant)-blue" alt="URP">
  <img src="https://img.shields.io/badge/AR%20Foundation-6.3-success" alt="AR Foundation 6.3">
  <img src="https://img.shields.io/badge/Platforms-Android%20%C2%B7%20iOS-lightgrey" alt="Android / iOS">
</p>

---

## Table of contents

1. [Overview](#overview)
2. [Screenshots / visuals](#screenshots--visuals)
3. [Features](#features)
4. [Tech stack](#tech-stack)
5. [How the app works](#how-the-app-works)
6. [Architecture](#architecture)
7. [Kitchen element models](#kitchen-element-models)
8. [Finishes & customization](#finishes--customization)
9. [Getting started](#getting-started)
10. [Editor tooling](#editor-tooling)

---

## Overview

AR Kitchen is an AR mobile app (Android/ARCore, iOS/ARKit) that lets **kitchen makers
visualize a kitchen in a client's space before installation**. The user scans the room,
places a single cuboid (a "voxel") on a detected AR plane to mark the kitchen volume,
adjusts its dimensions, and fills it with predefined kitchen elements (fridge, sink,
stove, worktop…) sized to standard real-world measurements — then tries different
finishes and accent colours on the result.

It was bootstrapped from Unity's Mobile AR Template; the template's demo content was
stripped so the app is built on a clean AR baseline.

## Screenshots / visuals

A walkthrough of the app, from scanning the room to a finished, priced kitchen:

<table>
  <tr>
    <td align="center"><img src="docs/images/Screenshot%20From%202026-06-27%2016-17-59.png" width="190"><br><b>1.</b> Scanning the room</td>
    <td align="center"><img src="docs/images/Screenshot%20From%202026-06-27%2016-18-15.png" width="190"><br><b>2.</b> Surfaces found</td>
    <td align="center"><img src="docs/images/Screenshot%20From%202026-06-27%2016-18-23.png" width="190"><br><b>3.</b> Tap to place</td>
    <td align="center"><img src="docs/images/Screenshot%20From%202026-06-27%2016-18-32.png" width="190"><br><b>4.</b> Kitchen space placed</td>
  </tr>
  <tr>
    <td align="center"><img src="docs/images/Screenshot%20From%202026-06-27%2016-18-42.png" width="190"><br><b>5.</b> Scale (W / D / H)</td>
    <td align="center"><img src="docs/images/Screenshot%20From%202026-06-27%2016-18-55.png" width="190"><br><b>6.</b> Move</td>
    <td align="center"><img src="docs/images/Screenshot%20From%202026-06-27%2016-19-00.png" width="190"><br><b>7.</b> Rotate</td>
    <td align="center"><img src="docs/images/Screenshot%20From%202026-06-27%2016-19-04.png" width="190"><br><b>8.</b> Add units</td>
  </tr>
  <tr>
    <td align="center"><img src="docs/images/Screenshot%20From%202026-06-27%2016-19-45.png" width="190"><br><b>9.</b> Filled & priced</td>
    <td align="center"><img src="docs/images/Screenshot%20From%202026-06-27%2016-19-54.png" width="190"><br><b>10.</b> Texture & colour</td>
    <td align="center"><img src="docs/images/Screenshot%20From%202026-06-27%2016-20-01.png" width="190"><br><b>11.</b> Finished result</td>
    <td></td>
  </tr>
</table>

**Available finishes** — the body textures offered by the in-app picker (see
[Finishes & customization](#finishes--customization)):

<table>
  <tr>
    <td align="center"><img src="Assets/Textures/Kitchen/Steel.png"   width="84"><br>Steel</td>
    <td align="center"><img src="Assets/Textures/Kitchen/Marble.png"  width="84"><br>Marble</td>
    <td align="center"><img src="Assets/Textures/Kitchen/Granite.png" width="84"><br>Granite</td>
    <td align="center"><img src="Assets/Textures/Kitchen/Oak.png"     width="84"><br>Oak</td>
    <td align="center"><img src="Assets/Textures/Kitchen/Walnut.png"  width="84"><br>Walnut</td>
    <td align="center"><img src="Assets/Textures/Kitchen/White.png"   width="84"><br>White</td>
    <td align="center"><img src="Assets/Textures/Kitchen/Black.png"   width="84"><br>Black</td>
  </tr>
</table>

## Features

- **Room scanning** with AR plane detection and edge-feathered plane visualization.
- **One-tap volume placement** — drop a resizable cuboid on a real surface to mark the kitchen.
- **Dimension editing** — width / depth / height sliders with centimetre snapping.
- **Camera-relative move pad** and rotation (free + snap angles) to align with the wall.
- **Element catalog** grouped into Fridges / Sinks / Stoves, with live fit & "missing
  mandatory" feedback; elements line up automatically along the wall edge.
- **Per-element variants** — tap a placed unit to swap its model without disturbing the layout.
- **Worktop filler** that flexes to span the remaining run.
- **Finishes & accent colours** — world-space triplanar textures on the body plus a
  secondary-colour swatch row (incl. metallic gold/silver).
- **Pricing readouts** per element/variant, and a "kitchen complete" celebration overlay.

## Tech stack

- **Unity** `6000.3.13f1` (Unity 6)
- **Universal Render Pipeline (URP)** — `URP-Performant` preset
- **AR Foundation 6.3** (ARCore + ARKit providers)
- **XR Interaction Toolkit 3.3**
- **New Input System 1.19**
- **Runtime UI:** UI Toolkit (UXML in `Assets/UI/Documents/`, USS in `Assets/UI/Styles/`)
- **Target platforms:** Android (ARCore), iOS (ARKit)

## How the app works

1. **Scanning** — `SurfaceScanFlow` shows the "Scanning your room" panel (`ScanningPanel`)
   until `ARPlaneManager` reports a detected plane (with a minimum on-screen time so it
   doesn't flash by).
2. **Surfaces found** — cross-fades to `SurfacesFoundPanel`; the user confirms they want
   to place the kitchen.
3. **Placement** — `PlaceKitchenPanel` shows a hint; `VoxelPlacer` is enabled and the next
   tap on an AR plane instantiates the voxel prefab there (one-shot).
4. **Placed HUD** — `PlacedHudPanel` fades in: Voxel/Planes visibility toggles top-right, a
   "Kitchen space" card, and an **Edit** button at the bottom.
5. **Editing** — Edit puts `VoxelStateManager` into editing state; `KitchenEditPanel` takes
   over the bottom of the screen with five modes:
   - **Scale** — W/D/H sliders
   - **Move** — camera-relative move pad + tap-to-replace
   - **Rotate** — free rotation + snap angles
   - **Units** — the element catalog (add/remove, worktop filler, variant picker)
   - **Texture** — tap an element, pick a body **finish** (texture) and/or a **secondary
     colour** for its accent surfaces
6. **Filling** — in Units mode, elements from the catalog are added to
   `KitchenLayoutController`, which lines them up along the voxel's wall edge, tracks
   used/remaining length, and rejects elements that don't fit. Tapping a placed element
   opens a variant picker that swaps its model without changing the layout.

## Architecture

Everything hangs off **`VoxelStateManager`** — the central hub. It owns the editing state
and the current `VoxelEditMode` (`None / Scale / Placement / Rotation / FillKitchen / Color`)
and broadcasts three events that every UI script subscribes to: `OnVoxelPlaced`,
`OnEditingChanged`, and `OnModeChanged`. UI scripts never talk to each other directly; they
react to state-manager events and call into the voxel/layout controllers.

```
VoxelPlacer ──OnPlaced──▶ VoxelStateManager ──events──▶ all UI panels
                               │
                               ▼ Controller
                         VoxelController ──OnResized──▶ KitchenLayoutController ──OnLayoutChanged──▶ HUD/Edit UI
                                                              │
                                                              ▼ instantiates
                                                        KitchenElementView (per element)
```

> Note: the `VoxelEditMode.Color` value is the internal name for what the UI now presents
> as **Texture** mode (finishes + secondary colour).


## Kitchen element models

Kitchen element 3D models live in `Assets/Models/`, split into 3 groups, each a subfolder
with a `.blend` source + exported `.fbx` (imported with useFileScale/useFileUnits). Each
model file is named `<code> <Type>.fbx` (e.g. `C3 Stove.fbx`), where the code marks its
standardised measurement. Measurements are **width × height × depth in cm**:

| Group | Code | Type | W × H × D (cm) |
|---|---|---|---|
| **Storage** (`Assets/Models/Storage`) | S1 | Fridge | 60 × 90 × 60 |
| | S2 | Fridge | 60 × 180 × 60 |
| | S3 | Fridge | 90 × 180 × 60 |
| | S4 | Fridge | 120 × 180 × 60 |
| **Washing** (`Assets/Models/Washing`) | W1 | Sink | 30 × 90 × 60 |
| | W2 | Sink | 60 × 90 × 60 |
| | W3 | Sink | 90 × 90 × 60 |
| | W4 | Sink | 120 × 90 × 60 |
| **Cooking** (`Assets/Models/Cooking`) | C1 | Stove | 30 × 2 × 60 *(thin drop-in cooktop, 2 cm tall)* |
| | C2 | Stove | 60 × 90 × 60 |
| | C3 | Stove | 120 × 180 × 60 |

Each model has a `KitchenElementDefinition` ScriptableObject (in
`Assets/Scripts/Kitchen/Definitions/`, named `<code> <Type>.asset`) carrying
group/code/dimensions and a reference to its FBX. `KitchenElementView` instantiates that
FBX and drops its bounding-box min corner onto the view's bottom/back/left pivot (keeping
the FBX's imported axis-correction rotation/scale, plus a 180° yaw so the front faces the
room). `KitchenLayoutController` lines them up along the voxel's −X wall.

> Long term, an [external layout service](docs/external-layout-service.md) will receive the
> voxel's W/D/H and return a generated layout that selects and places these models
> automatically.

## Finishes & customization

The Texture edit mode applies two **independent** finishes to a tapped element:

- **Body texture (primary)** — surfaces using `KitchenMainMaterial` (the
  `AR Kitchen/Triplanar` shader) take one of the finish PNGs above. The triplanar shader
  projects the texture in world space, so a finish keeps a consistent real-world scale
  across every part of a model regardless of its UV layout. Each placed unit instances its
  material, so finishes are per-element.
- **Secondary colour** — surfaces using `KitchenSecondaryMaterial` (URP Lit, handles/trims/
  accents) take a solid colour from a fixed palette. **Gold** and **Silver** also drive
  `_Metallic`/`_Smoothness` so they read as real metal, not flat mustard/grey; **Black**
  and **White** stay matte.

| | Colour | Hex |
|---|---|---|
| ![Black](https://img.shields.io/badge/Black-0D0D0F-0D0D0F) | Black | `#0D0D0F` |
| ![Gold](https://img.shields.io/badge/Gold-FFC757-FFC757) | Gold (metallic) | `#FFC757` |
| ![White](https://img.shields.io/badge/White-F2F2F2-F2F2F2) | White | `#F2F2F2` |
| ![Silver](https://img.shields.io/badge/Silver-E6E6EB-E6E6EB) | Silver (metallic) | `#E6E6EB` |

Elements without a secondary surface (the C1 cooktop and the procedural worktop) simply
hide the colour row.

## Getting started

**Prerequisites**
- Unity **`6000.3.13f1`** (install via Unity Hub).
- For on-device testing: an **ARCore-capable Android** phone or an **ARKit iPhone/iPad**.

**Run in the Editor (no device needed)**
1. Open the project in Unity Hub and open `Assets/Scenes/MainScene.unity`.
2. Press **Play**. AR Foundation's **XR Simulation** loads a simulated room (environments
   live under `Assets/UnityXRContent/ARFoundation/SimulationEnvironments/`) — move the
   camera to let planes be detected, then tap to place and edit the kitchen.

**Build to a device**
- **Android:** *File ▸ Build Settings ▸ Android*, switch platform, then *Build And Run* on
  a connected ARCore device.
- **iOS:** switch platform to iOS, build the Xcode project, then run on an ARKit device
  (set your signing team in Xcode first).

> If the scene ever looks unwired (missing managers/UI), the `Tools ▸ AR Kitchen` menu can
> regenerate it — see below.

## Editor tooling

One-time setup menu items under **`Tools ▸ AR Kitchen ▸ …`** build the scene and assets
(they are editor-only, not used at runtime):

- **AR Scene Setup** — ARSession + XR Origin + managers (`ARSceneSetup`)
- **UI / Placed HUD Setup** — the UI Toolkit documents (`UISceneSetup`, `PlacedHudSetup`)
- **AR Plane Setup** — the feathered AR plane prefab (`ARPlaneSetup`)
- **Create Default Kitchen Definitions** — generates the element definition assets (`KitchenDefinitionsSetup`)
- **Create Kitchen Element Prefab** — the bare `KitchenElementView` container (`KitchenElementPrefabSetup`)
- **Kitchen Layout Setup** — wires the voxel/layout (`KitchenLayoutSetup`)
- **Setup Texture Picker** — switches the body material to the triplanar shader and wires
  finish textures onto each definition (`TexturePickerSetup`)
- **Working Desk Setup** — the flexible worktop filler (`WorkingDeskSetup`)





# AR Tarot — Queen of Swords (MAMN60)

A marker-based AR prototype built in Unity with AR Foundation and ARKit image tracking.

## Concept

The application recognizes one tarot card image: **Queen of Swords**.

After the marker is detected, a spatial AR interpretation appears around the card:

- **Right side — Upright:** Clarity / Independence / Truth
- **Left side — Reversed:** Coldness / Harsh Judgment / Isolation
- **Center:** Queen of Swords

The interface reacts to the viewer's position relative to the marker. Moving the camera toward one side makes that interpretation larger, clearer, and less angled, while the opposite side recedes. The interaction is intended to create a perspective-based, "empty-box" style layout rather than a simple static overlay.

## Marker

Use the Queen of Swords image selected for the project and save it exactly as:

`Assets/Marker/QueenOfSwords.png`

The image can be shown on a second iPad/tablet instead of using a physical tarot card. Keep the marker screen still, avoid strong reflections, and display the image as large and cleanly as possible.

The setup script currently assumes a physical marker width of **0.12 m (12 cm)**. If the image is displayed at a noticeably different physical width, change `DefaultMarkerWidthMeters` in `Assets/Editor/TarotARSetup.cs`.

## Unity Setup

The project uses Unity 6 and these XR packages:

- AR Foundation 6.3.1
- Apple ARKit XR Plugin 6.3.1
- XR Plug-in Management 4.5.3

### 1. Add the marker image

Put the Queen of Swords PNG at:

`Assets/Marker/QueenOfSwords.png`

### 2. Generate the AR scene

After Unity finishes importing packages, run:

`MAMN60 > Setup Queen of Swords Marker AR`

This menu command:

1. Creates an AR Session.
2. Creates an XR Origin (Mobile AR).
3. Adds the AR camera components.
4. Adds `ARTrackedImageManager`.
5. Creates/updates an `XRReferenceImageLibrary`.
6. Registers `QueenOfSwords.png` as the image marker.
7. Adds `TarotMarkerController`.
8. Saves the generated scene as `Assets/Scenes/ARTarotQueenOfSwords.unity`.
9. Adds the generated scene to Build Settings.

If the scene was created before the PNG was added, either run the setup command again or run:

`MAMN60 > Register Queen of Swords Marker`

### 3. Configure iOS / ARKit

Run:

`MAMN60 > Configure iOS + ARKit for Tarot`

Then switch the active build platform/profile to iOS and verify that ARKit is enabled under XR Plug-in Management.

## Interaction Logic

`TarotMarkerController.cs` reads the AR camera position in the tracked marker's local coordinate system:

- Camera moves toward the marker's right side -> Upright panel is emphasized.
- Camera moves toward the marker's left side -> Reversed panel is emphasized.
- Camera remains near the center -> both side panels stay partially visible and angled.

The effect uses position, scale, opacity, and local Y rotation to create a simple perspective transition.

If left/right feels reversed on the device, enable `Invert Left Right` on the `TarotMarkerController` component.

## Code Structure

- `Assets/Scripts/TarotMarkerController.cs` — marker tracking lookup and viewpoint-dependent AR UI.
- `Assets/Editor/TarotARSetup.cs` — creates the AR scene, reference image library, iOS settings, and required components.

## Code provenance / references

The project-specific scripts were written for this MAMN60 prototype. The AR setup and image-tracking architecture follow Unity's official AR Foundation / ARKit APIs rather than third-party application code.

Relevant official documentation:

- Unity AR Foundation image tracking: https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.3/manual/features/image-tracking.html
- ARTrackedImageManager API: https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.3/api/UnityEngine.XR.ARFoundation.ARTrackedImageManager.html
- Apple ARKit XR Plugin: https://docs.unity3d.com/Packages/com.unity.xr.arkit@6.3/manual/index.html

## Current MVP Scope

- [x] One tarot marker only: Queen of Swords
- [x] Upright and reversed AR panels
- [x] Viewpoint-dependent perspective emphasis
- [x] One-click Unity scene setup
- [x] iOS / ARKit configuration helper
- [ ] Add the final Queen of Swords PNG to `Assets/Marker/QueenOfSwords.png`
- [ ] Run the setup command in Unity
- [ ] Build to iPhone/iPad and test physical marker size / left-right direction

## Course

MAMN60 — marker-based AR assignment

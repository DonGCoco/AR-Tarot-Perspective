# AR Tarot Perspective — Queen of Swords Prototype (MAMN60)

A marker-based AR prototype built in Unity with AR Foundation and ARKit image tracking.

## Concept

The current assignment prototype recognizes one tarot card image: **Queen of Swords**.

After the marker is detected, a spatial AR interpretation appears around the physical/digital card:

- **Right side — Upright:** Clarity / Independence / Truth
- **Left side — Reversed:** Coldness / Harsh Judgment / Isolation
- **Above the card:** Queen of Swords

The physical card image itself stays visible in the middle. The interface reacts to the viewer's position relative to the marker: moving the camera toward one side makes that interpretation larger, clearer, less angled, and slightly closer while the opposite side recedes. The interaction is intended to create a perspective-based, "empty-box" style layout rather than a static overlay.

## Marker

Use the selected Rider–Waite–Smith **Queen of Swords** image. The setup script accepts any of these paths:

- `Assets/Marker/QueenOfSwords.jpg`
- `Assets/Marker/Swords13.jpg`
- `Assets/Marker/QueenOfSwords.png`

The image can be shown on a second iPad/tablet instead of using a physical tarot card. Keep the marker screen still, avoid strong reflections, and display the image without other UI covering it.

The setup script currently assumes a physical/displayed marker width of **0.12 m (12 cm)**. If the displayed card is noticeably wider or narrower, change `DefaultMarkerWidthMeters` in `Assets/Editor/TarotARSetup.cs` to the actual displayed width.

## Unity Setup

The project uses Unity 6 and these XR packages:

- AR Foundation 6.3.1
- Apple ARKit XR Plugin 6.3.1
- XR Plug-in Management 4.5.3

### 1. Add the marker image

Create `Assets/Marker/` if it does not already exist and put the selected Queen of Swords image there using one of the accepted filenames above.

### 2. Generate the AR scene

After Unity finishes importing packages, run:

`MAMN60 > Setup Queen of Swords Marker AR`

This menu command:

1. Creates an AR Session.
2. Creates an XR Origin (Mobile AR).
3. Adds the AR camera components.
4. Adds `ARTrackedImageManager`.
5. Creates/updates an `XRReferenceImageLibrary`.
6. Registers the Queen of Swords image as the marker named `QueenOfSwords`.
7. Adds `TarotMarkerController`.
8. Saves the generated scene as `Assets/Scenes/ARTarotQueenOfSwords.unity`.
9. Adds the generated scene to Build Settings.

If the scene was created before the image was added, either run the setup command again or run:

`MAMN60 > Register Queen of Swords Marker`

### 3. Configure iOS / ARKit

Run:

`MAMN60 > Configure iOS + ARKit for Tarot`

Then switch the active build platform/profile to iOS and verify that ARKit is enabled under XR Plug-in Management.

## Interaction Logic

`TarotMarkerController.cs` converts the AR camera position into the tracked marker's local coordinate system:

- Camera moves toward the marker's right side -> Upright panel is emphasized.
- Camera moves toward the marker's left side -> Reversed panel is emphasized.
- Camera remains near the center -> both side panels stay partially visible and angled.

The effect uses local position, scale, opacity, and local Y rotation. It does **not** depend on the phone's gyroscope tilt alone; the interaction is based on the viewer's spatial viewpoint relative to the tracked image.

If left/right feels reversed on the device, enable `Invert Left Right` on the `TarotMarkerController` component.

## Code Structure

- `Assets/Scripts/TarotMarkerController.cs` — finds the tracked Queen of Swords marker, generates the three world-space UI panels, and controls the viewpoint-dependent perspective transition.
- `Assets/Editor/TarotARSetup.cs` — creates the AR scene, reference image library, iOS settings, and required AR Foundation components.

## Code provenance / references

The project-specific scripts were written for this MAMN60 prototype. No third-party application script was copied into the project. The implementation follows Unity's official AR Foundation / ARKit APIs and the same AR Foundation setup pattern used in the course work.

Relevant official documentation:

- Unity AR Foundation image tracking: https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.3/manual/features/image-tracking.html
- ARTrackedImageManager API: https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.3/api/UnityEngine.XR.ARFoundation.ARTrackedImageManager.html
- Apple ARKit XR Plugin: https://docs.unity3d.com/Packages/com.unity.xr.arkit@6.3/manual/index.html

### What was implemented/modified for this prototype

- Created a one-image `XRReferenceImageLibrary` for Queen of Swords.
- Added an `ARTrackedImageManager` to detect and track the marker.
- Added custom logic to compare the AR camera position with the marker's local X axis.
- Built the Upright/Reversed UI at runtime using world-space canvases.
- Added smooth scale, opacity, translation, and Y-axis rotation transitions to create the perspective effect.
- Added an editor setup command so the AR scene and iOS/ARKit configuration can be reproduced consistently.

## Current Assignment Scope

- [x] One tarot marker: Queen of Swords
- [x] Upright and reversed AR content
- [x] Viewpoint-dependent perspective emphasis
- [x] One-click Unity scene setup
- [x] iOS / ARKit configuration helper
- [ ] Put the final marker image into `Assets/Marker/`
- [ ] Run the setup command in Unity
- [ ] Build to iPhone/iPad and test detection, actual marker width, and left/right direction

The repository name is intentionally broader (`AR-Tarot-Perspective`) so the same interaction can later be extended to additional tarot cards. The MAMN60 submission itself remains a one-card Queen of Swords prototype.

## Course

MAMN60 — marker-based AR assignment

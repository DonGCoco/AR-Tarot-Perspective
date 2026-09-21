# AR Tarot Perspective — Queen of Swords (MAMN60)

A marker-based AR prototype built in **Unity 6**, **AR Foundation**, and **Apple ARKit** for the MAMN60 Introduction to AR assignment.

The prototype uses a Rider–Waite–Smith **Queen of Swords** image as an AR marker. After the card is recognized, a spatial tarot interpretation appears around it and reacts to the viewer's position.

## Current Experience

1. The app initially displays:

   **SCAN THE TAROT CARD**

2. When the Queen of Swords marker is recognized, the virtual card rises into view and performs a **360° vertical flip around the X axis**.

3. The app then displays:

   **MOVE YOUR PHONE LEFT OR RIGHT**  
   **TO EXPLORE UPRIGHT / REVERSED**

4. Moving the phone/viewpoint to either side changes the spatial composition:

   - **Right — Upright**
   - **Left — Reversed**

5. The selected interpretation plane moves forward, rotates toward the viewer, becomes larger, and reveals more text. At the same time, the central tarot card becomes smaller and moves slightly backward, while the opposite interpretation recedes.

The interaction is based on the viewer's **spatial position relative to the tracked marker**, not only on device tilt.

## Tarot Content

### Upright

**Clarity · Independence · Truth**

> A clear mind, an honest voice, and the courage to stand by your own judgment.

> What truth are you ready to face?

### Reversed

**Coldness · Harsh Judgment · Isolation**

> Clarity hardens into distance; discernment becomes criticism; independence turns inward.

> Where might you be judging too quickly?

## Marker-Based Tracking

The marker-tracking setup uses:

- AR Session
- XR Origin (Mobile AR)
- AR Tracked Image Manager
- XR Reference Image Library
- Apple ARKit XR Plugin
- A reference image with a specified physical size

The runtime flow is:

**Marker detected → tarot card entrance animation → viewpoint-dependent Upright/Reversed AR interface**

## Marker

The reference image is the Rider–Waite–Smith **Queen of Swords**.

Accepted local marker paths:

- `Assets/Marker/QueenOfSwords.jpg`
- `Assets/Marker/Swords13.jpg`
- `Assets/Marker/QueenOfSwords.png`

The marker can be printed or displayed on a second tablet/iPad. For reliable tracking, keep the marker still, avoid strong reflections, and avoid covering the image with other UI.

The current reference-image setup uses a marker width of approximately **0.12 m**.

## Unity / AR Setup

The project currently uses:

- Unity 6 / 6000.3
- AR Foundation 6.3.1
- Apple ARKit XR Plugin 6.3.1
- XR Plug-in Management 4.5.3
- iOS target platform

The generated AR scene is:

`Assets/Scenes/ARTarotQueenOfSwords.unity`

## Main Scripts

### `Assets/Scripts/TarotMarkerController.cs`

Controls the runtime AR experience:

- Finds the tracked `QueenOfSwords` reference image.
- Creates the central virtual tarot card.
- Creates the Upright and Reversed world-space interpretation planes.
- Plays the vertical 360° entrance flip.
- Displays scan and interaction guidance.
- Converts the AR camera position into marker-local coordinates.
- Detects whether the viewer has moved left or right.
- Animates panel position, rotation, scale, opacity, and depth.
- Shrinks/recedes the center card when one interpretation is emphasized.
- Reveals interpretation text and reflection questions progressively.
- Hides the movement instruction automatically after interaction or after a short delay.
- Restores the scan instruction if marker tracking is lost.

### `Assets/Editor/TarotARSetup.cs`

Provides editor-side project setup:

- Creates the AR Session.
- Creates XR Origin (Mobile AR).
- Adds/configures `ARTrackedImageManager`.
- Creates or updates the XR Reference Image Library.
- Registers the Queen of Swords marker.
- Prepares the display image used by the runtime card.
- Configures the generated AR scene.
- Assists with iOS / ARKit setup.

## Viewpoint Interaction

The controller converts the AR camera position into the tracked image's local coordinate system.

Conceptually:

```text
camera on left side
        ↓
Reversed plane expands
Center card shrinks/recedes
Upright plane recedes

camera near center
        ↓
Neutral three-plane composition

camera on right side
        ↓
Upright plane expands
Center card shrinks/recedes
Reversed plane recedes
```

This creates a spatial perspective-carousel effect inspired by an "empty-box" style composition rather than a flat screen overlay.

## Visual Structure

The AR content consists of three independent spatial elements:

```text
Reversed Plane     Queen of Swords     Upright Plane
      \                   |                  /
       \                  |                 /
        \             Viewer              /
```

The two side surfaces are deliberately strongly foreshortened in the neutral state. When the viewer moves toward one side, that surface becomes more front-facing and reveals its full rectangular shape.

## Reproducing the Scene

If the marker image is available locally, run:

`MAMN60 > Setup Queen of Swords Marker AR`

The helper creates or updates the scene and tracking configuration.

If only the marker registration needs to be refreshed, run:

`MAMN60 > Register Queen of Swords Marker`

For iOS configuration:

`MAMN60 > Configure iOS + ARKit for Tarot`

Then build using **Build and Run** for the connected iOS device.

## Implementation Notes

The main interaction logic is implemented in the custom `TarotMarkerController`.

It handles:

- Marker detection
- Tarot card presentation
- 360° vertical entrance flip
- Viewpoint-relative left/right interaction
- Spatial panel movement and perspective
- Central-card shrink/recede behavior
- Progressive text reveal
- Scan and movement guidance

Relevant Unity documentation:

- Unity AR Foundation image tracking: https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.3/manual/features/image-tracking.html
- ARTrackedImageManager API: https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.3/api/UnityEngine.XR.ARFoundation.ARTrackedImageManager.html
- Apple ARKit XR Plugin: https://docs.unity3d.com/Packages/com.unity.xr.arkit@6.3/manual/index.html

## Current Assignment Scope

- [x] Marker-based tracking with Queen of Swords
- [x] XR Reference Image Library
- [x] ARKit / iOS setup
- [x] Custom tarot-card AR content
- [x] Upright and Reversed interpretations
- [x] Viewpoint-dependent left/right interaction
- [x] Spatial perspective / foreshortening effect
- [x] Central-card shrink/recede behavior
- [x] 360° vertical entrance flip
- [x] Scan instruction
- [x] Left/right movement instruction with automatic fade
- [x] Progressive interpretation and reflection-question reveal
- [ ] Final device tuning after the latest animation/UI changes

## Future Extension

The current prototype uses **Queen of Swords** as the first implemented tarot card, but the long-term goal is to extend the project into a complete interactive **78-card tarot deck**.

The planned structure is:

- **22 Major Arcana cards**
- **56 Minor Arcana cards**
- A separate reference image for each tarot card
- Individual Upright and Reversed meanings for every card
- Card-specific keywords, interpretations, and reflection questions
- Reusable viewpoint-based interaction across the full deck
- Card-specific accent colors and visual styling
- A data-driven card system so new cards can be added without rewriting the core AR interaction code

The intended final experience is that the user can scan any supported tarot card and automatically receive the corresponding AR interpretation while keeping the same spatial perspective interaction.

## Course

**MAMN60 — Introduction to Augmented Reality**  
Marker-based AR assignment.

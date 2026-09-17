# MAMN60 Assignment Submission Notes

This assignment is implemented as **two separate AR applications**:

1. **Marker-based tracking:** AR Tarot Perspective — Queen of Swords
2. **Surface-based tracking / SLAM:** AR Whack-a-Mole

## 1. Marker-based tracking — Queen of Swords

Repository: `AR-Tarot-Perspective`

The app uses **AR Foundation image tracking**. The Queen of Swords card is added to an `XRReferenceImageLibrary` and an `ARTrackedImageManager` searches the camera feed for that reference image. When ARKit recognizes it, Unity provides an `ARTrackedImage` pose (position, rotation, and tracked image size). The AR interface is parented to that tracked image so the content stays registered to the card as the camera moves.

The current prototype uses one card only. The physical card is not required: the same image can be displayed on a second iPad/tablet and used as the image target.

### Custom interaction

The app does more than place a static object on the marker. `TarotMarkerController.cs` converts the AR camera position from world space into the tracked card's local coordinate system using `InverseTransformPoint`. The local X value is used to determine whether the viewer is looking more from the left or the right side.

- Right side -> **Upright**: Clarity / Independence / Truth
- Left side -> **Reversed**: Coldness / Harsh Judgment / Isolation

The selected side becomes larger, more opaque, less rotated, and moves slightly toward the viewer. The other side becomes smaller and more transparent. This creates the perspective-based interaction used in the prototype.

## 2. Surface-based tracking / SLAM — AR Whack-a-Mole

Repository: `AR-Whack-a-Mole-MAMN60`

The surface-based app uses AR Foundation with ARKit's world tracking. ARKit estimates the device pose while building a representation of the surrounding environment from camera and motion-sensor data. AR Foundation exposes detected surfaces through `ARPlaneManager`.

The application requests **horizontal plane detection**. While the user scans a table or other horizontal surface, `ARRaycastManager.Raycast(...)` casts a ray from the screen into the detected AR planes using `TrackableType.PlaneWithinPolygon`. When a valid hit is found, a placement reticle is shown at the returned pose. Tapping performs another raycast and places the Whack-a-Mole game at that surface pose.

After placement, plane visualization/detection is disabled because the game already has a stable world-space pose. The user can then tap the virtual moles to play.

### How SLAM is used

The application does not implement a SLAM algorithm from scratch. **ARKit performs the underlying visual-inertial world tracking / SLAM**, and AR Foundation provides the cross-platform Unity API used by the project. The custom code consumes the tracking results by detecting horizontal planes, raycasting against them, and anchoring the game content at the selected pose.

## Code sources and modifications

No third-party application script was copied verbatim into either project. The implementation uses Unity's standard AR Foundation / ARKit APIs and follows the architecture described in Unity's official documentation and the AR setup patterns covered in the course.

### Marker application

Reference APIs / documentation:

- AR Foundation image tracking / `ARTrackedImageManager`
- `XRReferenceImageLibrary`
- Apple ARKit XR Plugin

Custom work added for the assignment:

- Queen of Swords reference image registration
- tracked-image lookup
- world-space Upright/Reversed UI generation
- camera-to-marker local-coordinate calculation
- perspective transition using position, scale, opacity, and rotation
- editor setup helper for the AR scene and iOS/ARKit settings

### Surface application

Reference APIs / documentation:

- `ARPlaneManager`
- `ARRaycastManager`
- AR Foundation plane detection and raycasting
- Apple ARKit XR Plugin

Custom work added for the assignment:

- horizontal-surface placement reticle
- tap-to-place logic
- board orientation toward the viewer
- Whack-a-Mole game generation and hit interaction
- reset flow and runtime UI

## Short supervisor description

For the marker-based part, I created a Queen of Swords tarot-card prototype using AR Foundation image tracking. The card image is stored in an XR Reference Image Library and detected using ARTrackedImageManager. When the card is tracked, the app attaches an AR interface to its pose. The interface is viewpoint-dependent: moving to the right emphasizes the upright interpretation, while moving to the left emphasizes the reversed interpretation.

For the surface-based part, I used AR Foundation/ARKit world tracking with horizontal plane detection. ARKit performs the underlying visual-inertial tracking/SLAM, while ARPlaneManager exposes detected surfaces in Unity. I use ARRaycastManager to raycast against a detected plane, show a placement reticle, and place the Whack-a-Mole game at the selected surface pose.

using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.ARSubsystems;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARKit;
using UnityEngine.XR.ARSubsystems;

public static class TarotARSetup
{
    private const string ScenePath = "Assets/Scenes/ARTarotQueenOfSwords.unity";
    private const string MarkerTexturePath = "Assets/Marker/QueenOfSwords.png";
    private const string LibraryPath = "Assets/ReferenceImages/TarotReferenceImageLibrary.asset";
    private const string MarkerName = "QueenOfSwords";
    private const float DefaultMarkerWidthMeters = 0.12f;

    [MenuItem("MAMN60/Setup Queen of Swords Marker AR")]
    public static void SetupScene()
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Marker");
        EnsureFolder("Assets/ReferenceImages");

        XRReferenceImageLibrary library = GetOrCreateReferenceLibrary();
        bool markerReady = TryRegisterMarker(library);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        if (!EditorApplication.ExecuteMenuItem("GameObject/XR/AR Session"))
        {
            Debug.LogError("Could not create AR Session. Wait for AR Foundation packages to finish importing, then run MAMN60 > Setup Queen of Swords Marker AR again.");
            return;
        }

        Selection.activeGameObject = null;

        if (!EditorApplication.ExecuteMenuItem("GameObject/XR/XR Origin (Mobile AR)"))
        {
            Debug.LogError("Could not create XR Origin (Mobile AR). Wait for AR Foundation packages to finish importing, then run setup again.");
            return;
        }

        XROrigin xrOrigin = Object.FindFirstObjectByType<XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogError("XR Origin was not found after creation.");
            return;
        }

        Camera camera = xrOrigin.Camera;
        if (camera == null)
            camera = xrOrigin.GetComponentInChildren<Camera>(true);

        if (camera == null)
        {
            Debug.LogError("AR camera was not found under XR Origin.");
            return;
        }

        camera.tag = "MainCamera";

        ARCameraManager cameraManager = camera.GetComponent<ARCameraManager>();
        if (cameraManager == null)
            cameraManager = camera.gameObject.AddComponent<ARCameraManager>();

        ARCameraBackground cameraBackground = camera.GetComponent<ARCameraBackground>();
        if (cameraBackground == null)
            cameraBackground = camera.gameObject.AddComponent<ARCameraBackground>();

        ARTrackedImageManager trackedImageManager = xrOrigin.GetComponent<ARTrackedImageManager>();
        if (trackedImageManager == null)
            trackedImageManager = xrOrigin.gameObject.AddComponent<ARTrackedImageManager>();

        trackedImageManager.referenceLibrary = library;
        trackedImageManager.requestedMaxNumberOfMovingImages = 1;

        TarotMarkerController controller = xrOrigin.GetComponent<TarotMarkerController>();
        if (controller == null)
            controller = xrOrigin.gameObject.AddComponent<TarotMarkerController>();

        controller.Configure(trackedImageManager, camera);

        if (Object.FindFirstObjectByType<Light>() == null)
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = xrOrigin.gameObject;

        if (markerReady)
        {
            Debug.Log("Queen of Swords marker AR scene created successfully at " + ScenePath + ". The marker is registered as '" + MarkerName + "'.");
        }
        else
        {
            Debug.LogWarning("Scene created, but the marker image is still missing. Save the exact Queen of Swords image as Assets/Marker/QueenOfSwords.png, then run MAMN60 > Setup Queen of Swords Marker AR once more.");
        }
    }

    [MenuItem("MAMN60/Register Queen of Swords Marker")]
    public static void RegisterMarkerOnly()
    {
        EnsureFolder("Assets/Marker");
        EnsureFolder("Assets/ReferenceImages");

        XRReferenceImageLibrary library = GetOrCreateReferenceLibrary();
        if (!TryRegisterMarker(library))
        {
            Debug.LogError("Marker image not found. Put the image at Assets/Marker/QueenOfSwords.png first.");
            return;
        }

        ARTrackedImageManager manager = Object.FindFirstObjectByType<ARTrackedImageManager>();
        if (manager != null)
            manager.referenceLibrary = library;

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("Queen of Swords marker registered in the reference image library.");
    }

    [MenuItem("MAMN60/Configure iOS + ARKit for Tarot")]
    public static void ConfigureIOS()
    {
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.dongjieru.mamn60.artarot");
        PlayerSettings.iOS.cameraUsageDescription = "The camera is used to recognize the Queen of Swords tarot card and display AR interpretations.";
        PlayerSettings.iOS.targetOSVersionString = "15.0";

        var buildTargetSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.iOS);
        if (buildTargetSettings == null || buildTargetSettings.AssignedSettings == null)
        {
            Debug.LogWarning("XR Plug-in Management settings are not ready yet. Open Project Settings > XR Plug-in Management once, then run this menu item again.");
            return;
        }

        bool assigned = XRPackageMetadataStore.AssignLoader(
            buildTargetSettings.AssignedSettings,
            typeof(ARKitLoader).FullName,
            BuildTargetGroup.iOS);

        AssetDatabase.SaveAssets();

        if (assigned)
            Debug.Log("ARKit loader enabled for iOS. Switch the active platform to iOS in Build Profiles before building.");
        else
            Debug.LogWarning("Unity did not report a new ARKit assignment. Check Project Settings > XR Plug-in Management > iOS and make sure ARKit is enabled.");
    }

    private static XRReferenceImageLibrary GetOrCreateReferenceLibrary()
    {
        XRReferenceImageLibrary library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(LibraryPath);
        if (library != null)
            return library;

        library = ScriptableObject.CreateInstance<XRReferenceImageLibrary>();
        AssetDatabase.CreateAsset(library, LibraryPath);
        AssetDatabase.SaveAssets();
        return library;
    }

    private static bool TryRegisterMarker(XRReferenceImageLibrary library)
    {
        Texture2D markerTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(MarkerTexturePath);
        if (markerTexture == null)
            return false;

        int markerIndex = FindMarkerIndex(library);
        if (markerIndex < 0)
        {
            library.Add();
            markerIndex = library.count - 1;
        }

        float aspect = markerTexture.height > 0 ? (float)markerTexture.height / markerTexture.width : 1.7f;
        Vector2 physicalSize = new Vector2(DefaultMarkerWidthMeters, DefaultMarkerWidthMeters * aspect);

        library.SetName(markerIndex, MarkerName);
        library.SetTexture(markerIndex, markerTexture, false);
        library.SetSpecifySize(markerIndex, true);
        library.SetSize(markerIndex, physicalSize);

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        return true;
    }

    private static int FindMarkerIndex(XRReferenceImageLibrary library)
    {
        for (int i = 0; i < library.count; i++)
        {
            if (library[i].name == MarkerName)
                return i;
        }

        return -1;
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        EditorBuildSettingsScene[] currentScenes = EditorBuildSettings.scenes;
        if (currentScenes.Any(scene => scene.path == scenePath))
            return;

        EditorBuildSettings.scenes = currentScenes
            .Concat(new[] { new EditorBuildSettingsScene(scenePath, true) })
            .ToArray();
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}

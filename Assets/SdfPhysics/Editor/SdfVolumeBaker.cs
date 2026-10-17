using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SdfBakeSettings))]
public sealed class SdfBakeSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        if (GUILayout.Button("Bake SDF Volume"))
        {
            SdfVolumeBaker.Bake((SdfBakeSettings)target);
        }
    }
}

/// <summary>
/// Editor-only bake step for this task's static default primitives.
/// The player never calls these analytic functions; it reads only the serialized samples.
/// </summary>
public static class SdfVolumeBaker
{
    private const string DEFAULT_ASSET_PATH = "Assets/SdfPhysics/Generated/SdfVolume.asset";
    private const float UNIFORM_SCALE_TOLERANCE = 0.0001f;

    [MenuItem("Tools/SDF Physics/Bake SDF Volume")]
    private static void BakeFirstSettingsInOpenScene()
    {
        var settings = UnityEngine.Object.FindFirstObjectByType<SdfBakeSettings>();
        if (settings == null)
        {
            Debug.LogError("No SdfBakeSettings component exists in the open scene.");
            return;
        }

        Bake(settings);
    }

    public static void Bake(SdfBakeSettings settings)
    {
        if (!TryCreateSources(settings, out var plane, out var cylinder, out var error))
        {
            Debug.LogError($"SDF bake aborted: {error}", settings);
            return;
        }

        var bounds = settings.VolumeBounds;
        var resolution = settings.Resolution;
        var sampleCount = checked(resolution.x * resolution.y * resolution.z);
        var encodedDistances = new ushort[sampleCount];
        var minimumDistance = float.PositiveInfinity;
        var maximumDistance = float.NegativeInfinity;

        try
        {
            for (var z = 0; z < resolution.z; z++)
            {
                var zFraction = z / (float)(resolution.z - 1);
                for (var y = 0; y < resolution.y; y++)
                {
                    var yFraction = y / (float)(resolution.y - 1);
                    for (var x = 0; x < resolution.x; x++)
                    {
                        var xFraction = x / (float)(resolution.x - 1);
                        var samplePosition = new Vector3(
                            Mathf.Lerp(bounds.min.x, bounds.max.x, xFraction),
                            Mathf.Lerp(bounds.min.y, bounds.max.y, yFraction),
                            Mathf.Lerp(bounds.min.z, bounds.max.z, zFraction));

                        var distance = Mathf.Min(plane.SignedDistance(samplePosition), cylinder.SignedDistance(samplePosition));
                        var index = x + resolution.x * (y + resolution.y * z);
                        encodedDistances[index] = (ushort)math.f32tof16(distance);
                        minimumDistance = Mathf.Min(minimumDistance, distance);
                        maximumDistance = Mathf.Max(maximumDistance, distance);
                    }
                }

                EditorUtility.DisplayProgressBar(
                    "Baking signed distance field",
                    $"Sampling slice {z + 1} of {resolution.z}",
                    (z + 1) / (float)resolution.z);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        var volume = GetOrCreateOutputAsset(settings);
        volume.SetBakedData(bounds, resolution, encodedDistances);
        settings.SetBakedVolume(volume);

        EditorUtility.SetDirty(volume);
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        Selection.activeObject = volume;
        Debug.Log(
            $"Baked {sampleCount:N0} SDF samples to '{AssetDatabase.GetAssetPath(volume)}'. " +
            $"Distance range: {minimumDistance:F3} to {maximumDistance:F3} metres.",
            volume);
    }

    private static SdfVolumeAsset GetOrCreateOutputAsset(SdfBakeSettings settings)
    {
        if (settings.BakedVolume != null)
        {
            return settings.BakedVolume;
        }

        var existingAsset = AssetDatabase.LoadAssetAtPath<SdfVolumeAsset>(DEFAULT_ASSET_PATH);
        if (existingAsset != null)
        {
            return existingAsset;
        }

        var newAsset = ScriptableObject.CreateInstance<SdfVolumeAsset>();
        AssetDatabase.CreateAsset(newAsset, DEFAULT_ASSET_PATH);
        return newAsset;
    }

    private static bool TryCreateSources(
        SdfBakeSettings settings,
        out PlaneSource plane,
        out CylinderSource cylinder,
        out string error)
    {
        plane = default;
        cylinder = default;

        if (settings.Ground == null || settings.Cylinder == null)
        {
            error = "Assign both Ground and Cylinder MeshFilters in SdfBakeSettings.";
            return false;
        }

        if (settings.Ground.sharedMesh == null || settings.Cylinder.sharedMesh == null)
        {
            error = "Both SDF sources need a shared mesh.";
            return false;
        }

        if (!TryGetUniformScale(settings.Ground.transform, out var planeScale) ||
            !TryGetUniformScale(settings.Cylinder.transform, out var cylinderScale))
        {
            error = "The analytic SDF baker requires uniform source scaling. Reset the Plane and Cylinder scale to (1, 1, 1).";
            return false;
        }

        var planeBounds = settings.Ground.sharedMesh.bounds;
        var cylinderBounds = settings.Cylinder.sharedMesh.bounds;
        if (!Mathf.Approximately(cylinderBounds.extents.x, cylinderBounds.extents.z))
        {
            error = "Cylinder source must have equal X and Z mesh extents.";
            return false;
        }

        plane = new PlaneSource(settings.Ground.transform, planeBounds, planeScale);
        cylinder = new CylinderSource(settings.Cylinder.transform, cylinderBounds, cylinderScale);
        error = null;
        return true;
    }

    private static bool TryGetUniformScale(Transform transform, out float uniformScale)
    {
        var scale = transform.lossyScale;
        uniformScale = scale.x;
        return scale.x > 0f &&
               Mathf.Abs(scale.x - scale.y) < UNIFORM_SCALE_TOLERANCE &&
               Mathf.Abs(scale.x - scale.z) < UNIFORM_SCALE_TOLERANCE;
    }

    private readonly struct PlaneSource
    {
        private readonly Transform _transform;
        private readonly Vector3 _localCenter;
        private readonly Vector2 _halfExtents;
        private readonly float _scale;

        public PlaneSource(Transform transform, Bounds meshBounds, float scale)
        {
            _transform = transform;
            _localCenter = meshBounds.center;
            _halfExtents = new Vector2(meshBounds.extents.x, meshBounds.extents.z);
            _scale = scale;
        }

        public float SignedDistance(Vector3 worldPosition)
        {
            var localPosition = _transform.InverseTransformPoint(worldPosition) - _localCenter;
            var horizontal = new Vector2(Mathf.Abs(localPosition.x), Mathf.Abs(localPosition.z)) - _halfExtents;
            var horizontalDistance = new Vector2(Mathf.Max(horizontal.x, 0f), Mathf.Max(horizontal.y, 0f)).magnitude +
                                     Mathf.Min(Mathf.Max(horizontal.x, horizontal.y), 0f);

            // This is the intersection of the finite plane footprint and the half-space below it.
            // Above the interior it is exactly the height above the visible plane.
            return Mathf.Max(horizontalDistance, localPosition.y) * _scale;
        }
    }

    private readonly struct CylinderSource
    {
        private readonly Transform _transform;
        private readonly Vector3 _localCenter;
        private readonly float _radius;
        private readonly float _halfHeight;
        private readonly float _scale;

        public CylinderSource(Transform transform, Bounds meshBounds, float scale)
        {
            _transform = transform;
            _localCenter = meshBounds.center;
            _radius = meshBounds.extents.x;
            _halfHeight = meshBounds.extents.y;
            _scale = scale;
        }

        public float SignedDistance(Vector3 worldPosition)
        {
            var localPosition = _transform.InverseTransformPoint(worldPosition) - _localCenter;
            var radialDistance = new Vector2(localPosition.x, localPosition.z).magnitude - _radius;
            var verticalDistance = Mathf.Abs(localPosition.y) - _halfHeight;
            var outside = new Vector2(Mathf.Max(radialDistance, 0f), Mathf.Max(verticalDistance, 0f)).magnitude;
            var inside = Mathf.Min(Mathf.Max(radialDistance, verticalDistance), 0f);
            return (outside + inside) * _scale;
        }
    }
}

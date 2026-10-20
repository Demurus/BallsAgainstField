using UnityEditor;
using UnityEngine;

namespace SdfPhysics.Editor
{
    /// <summary>
    /// Creates the 20-triangle sphere used by the ball renderer
    /// At 10,000 instances this is 200,000 source triangles per frame
    /// </summary>
    public static class LowPolyBallMeshGenerator
    {
        private const string AssetPath = "Assets/SdfPhysics/Generated/BallIcosphere.mesh";

        [MenuItem("Tools/SDF Physics/Create Low Poly Ball Mesh")]
        private static void CreateMesh()
        {
            var existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(AssetPath);
            if (existingMesh != null)
            {
                Selection.activeObject = existingMesh;
                Debug.Log($"Low-poly ball mesh already exists at '{AssetPath}'.", existingMesh);
                return;
            }

            var goldenRatio = (1f + Mathf.Sqrt(5f)) * 0.5f;
            var vertices = new[]
            {
                new Vector3(-1f, goldenRatio, 0f).normalized,
                new Vector3(1f, goldenRatio, 0f).normalized,
                new Vector3(-1f, -goldenRatio, 0f).normalized,
                new Vector3(1f, -goldenRatio, 0f).normalized,
                new Vector3(0f, -1f, goldenRatio).normalized,
                new Vector3(0f, 1f, goldenRatio).normalized,
                new Vector3(0f, -1f, -goldenRatio).normalized,
                new Vector3(0f, 1f, -goldenRatio).normalized,
                new Vector3(goldenRatio, 0f, -1f).normalized,
                new Vector3(goldenRatio, 0f, 1f).normalized,
                new Vector3(-goldenRatio, 0f, -1f).normalized,
                new Vector3(-goldenRatio, 0f, 1f).normalized
            };

            var triangles = new[]
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
                1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
                4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
            };

            var mesh = new Mesh { name = "BallIcosphere" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);

            AssetDatabase.CreateAsset(mesh, AssetPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = mesh;
            Debug.Log($"Created 20-triangle ball mesh at '{AssetPath}'.", mesh);
        }
    }
}

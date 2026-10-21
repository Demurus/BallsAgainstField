using SdfPhysics.Runtime.Sdf;
using UnityEditor;
using UnityEngine;

namespace SdfPhysics.Editor
{
    [CustomEditor(typeof(SdfBakeSettings))]
    public class SdfBakeSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Bake SDF Volume"))
            {
                SdfPlaneAndCylinderVolumeBaker.Bake((SdfBakeSettings)target);
            }
        }
    }
}
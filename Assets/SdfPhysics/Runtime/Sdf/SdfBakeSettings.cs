using UnityEngine;

namespace SdfPhysics.Runtime.Sdf
{
    [DisallowMultipleComponent]
    public sealed class SdfBakeSettings : MonoBehaviour
    {
        [Header("Required scene sources")]
        [SerializeField] private MeshFilter _ground;
        [SerializeField] private MeshFilter _cylinder;

        [Header("Baked volume")]
        [SerializeField] private Vector3 _volumeMin = new(-5.25f, -0.5f, -5.25f);
        [SerializeField] private Vector3 _volumeMax = new(5.25f, 6f, 5.25f);
        [SerializeField] private Vector3Int _resolution = new(192, 128, 192);
        [SerializeField, Min(0.001f)] private float _ballRadius = 0.09f;

        [Header("Bake output")]
        [SerializeField] private SdfVolumeAsset _bakedVolume;

        public MeshFilter Ground => _ground;
        public MeshFilter Cylinder => _cylinder;
        public Bounds VolumeBounds => new((_volumeMin + _volumeMax) * 0.5f, _volumeMax - _volumeMin);
        public Vector3Int Resolution => _resolution;
        public float BallRadius => _ballRadius;
        public SdfVolumeAsset BakedVolume => _bakedVolume;

        public void SetBakedVolume(SdfVolumeAsset volume)
        {
            _bakedVolume = volume;
        }

        /// <summary>
        /// Guard against incorrect resolution. Grid can not have resolution lower than 2
        /// </summary>
        private void OnValidate()
        {
            _resolution.x = Mathf.Max(2, _resolution.x);
            _resolution.y = Mathf.Max(2, _resolution.y);
            _resolution.z = Mathf.Max(2, _resolution.z);
            _ballRadius = Mathf.Max(0.001f, _ballRadius);
        }
    }
}

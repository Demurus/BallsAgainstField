using System;
using UnityEngine;

namespace SdfPhysics.Runtime.Sdf
{
    [CreateAssetMenu(menuName = "SDF Physics/SDF Volume", fileName = "SdfVolume")]
    public sealed class SdfVolumeAsset : ScriptableObject
    {
        [SerializeField] private Bounds _bounds;
        [SerializeField] private Vector3Int _resolution;
        [SerializeField, HideInInspector] private ushort[] _encodedDistances = Array.Empty<ushort>();

        public Bounds Bounds => _bounds;
        public Vector3Int Resolution => _resolution;
        public ushort[] EncodedDistances => _encodedDistances;

        public Vector3 CellSize => new(
            _bounds.size.x / (_resolution.x - 1),
            _bounds.size.y / (_resolution.y - 1),
            _bounds.size.z / (_resolution.z - 1));

        public bool HasValidData =>
            _resolution.x >= 2 && _resolution.y >= 2 && _resolution.z >= 2 &&
            _encodedDistances != null &&
            _encodedDistances.Length == _resolution.x * _resolution.y * _resolution.z;
    
        public void SetBakedData(Bounds newBounds, Vector3Int newResolution, ushort[] newEncodedDistances)
        {
            if (newResolution.x < 2 || newResolution.y < 2 || newResolution.z < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(newResolution), "Each SDF dimension must contain at least two samples.");
            }

            var expectedCount = newResolution.x * newResolution.y * newResolution.z;
            if (newEncodedDistances == null || newEncodedDistances.Length != expectedCount)
            {
                throw new ArgumentException($"Expected {expectedCount} distance samples.", nameof(newEncodedDistances));
            }

            _bounds = newBounds;
            _resolution = newResolution;
            _encodedDistances = newEncodedDistances;
        }
    }
}

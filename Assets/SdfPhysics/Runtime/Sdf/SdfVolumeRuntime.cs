using System;
using Unity.Collections;
using Unity.Mathematics;

namespace SdfPhysics.Runtime.Sdf
{
    public sealed class SdfVolumeRuntime : IDisposable
    {
        private NativeArray<ushort> encodedDistances;

        public SdfGridData Data { get; private set; }

        public SdfVolumeRuntime(SdfVolumeAsset asset, Allocator allocator)
        {
            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            if (!asset.HasValidData)
            {
                throw new ArgumentException("The assigned SDF asset has no valid baked data.", nameof(asset));
            }

            encodedDistances = new NativeArray<ushort>(asset.EncodedDistances.Length, allocator, NativeArrayOptions.UninitializedMemory);
            encodedDistances.CopyFrom(asset.EncodedDistances);

            var bounds = asset.Bounds;
            var size = bounds.size;
            var resolution = asset.Resolution;
            Data = new SdfGridData
            {
                EncodedDistances = encodedDistances,
                Min = new float3(bounds.min.x, bounds.min.y, bounds.min.z),
                Max = new float3(bounds.max.x, bounds.max.y, bounds.max.z),
                CellSize = new float3(
                    size.x / (resolution.x - 1),
                    size.y / (resolution.y - 1),
                    size.z / (resolution.z - 1)),
                Resolution = new int3(resolution.x, resolution.y, resolution.z)
            };
        }

        public void Dispose()
        {
            if (encodedDistances.IsCreated)
            {
                encodedDistances.Dispose();
            }
        }
    }
}

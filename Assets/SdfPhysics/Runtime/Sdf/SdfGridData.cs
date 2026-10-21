using Unity.Collections;
using Unity.Mathematics;

namespace SdfPhysics.Runtime.Sdf
{
    public struct SdfGridData
    {
        //We don't want to mutate the baked data
        [ReadOnly] public NativeArray<ushort> EncodedDistances;
        public float3 Min;
        public float3 Max;
        public float3 CellSize;
        public int3 Resolution;

        public bool TrySample(float3 worldPosition, out SDFSample sample)
        {
            //Checks if worldPosition is within our bounds
            if (math.any(worldPosition < Min) || math.any(worldPosition > Max))
            {
                sample = default;
                return false;
            }

            //Converting world coordinates into coordinates inside grid
            var gridPosition = (worldPosition - Min) / CellSize;
            var flooredGridPosition = (int3)math.floor(gridPosition);
            //Protect from going out of grid boundaries 0...191
            var cell = math.clamp(flooredGridPosition, int3.zero, Resolution - 2);
            //Getting position inside the cell
            var fraction = math.clamp(gridPosition - cell, 0f, 1f);

            var distanceX0Y0Z0= Decode(cell.x, cell.y, cell.z);
            var distanceX1Y0Z0 = Decode(cell.x + 1, cell.y, cell.z);
            var distanceX0Y1Z0 = Decode(cell.x, cell.y + 1, cell.z);
            var distanceX1Y1Z0 = Decode(cell.x + 1, cell.y + 1, cell.z);
            var distanceX0Y0Z1 = Decode(cell.x, cell.y, cell.z + 1);
            var distanceX1Y0Z1 = Decode(cell.x + 1, cell.y, cell.z + 1);
            var distanceX0Y1Z1 = Decode(cell.x, cell.y + 1, cell.z + 1);
            var distanceX1Y1Z1 = Decode(cell.x + 1, cell.y + 1, cell.z + 1);

            var distanceY0Z0 = math.lerp(distanceX0Y0Z0, distanceX1Y0Z0, fraction.x);
            var distanceY1Z0 = math.lerp(distanceX0Y1Z0, distanceX1Y1Z0, fraction.x);
            var distanceY0Z1 = math.lerp(distanceX0Y0Z1, distanceX1Y0Z1, fraction.x);
            var distanceY1Z1 = math.lerp(distanceX0Y1Z1, distanceX1Y1Z1, fraction.x);
            var distanceZ0 = math.lerp(distanceY0Z0, distanceY1Z0, fraction.y);
            var distanceZ1 = math.lerp(distanceY0Z1, distanceY1Z1, fraction.y);

            sample.Distance = math.lerp(distanceZ0, distanceZ1, fraction.z);
            sample.OutDirection = new float3(
                math.lerp(
                    math.lerp(distanceX1Y0Z0 - distanceX0Y0Z0, distanceX1Y1Z0 - distanceX0Y1Z0, fraction.y),
                    math.lerp(distanceX1Y0Z1 - distanceX0Y0Z1, distanceX1Y1Z1 - distanceX0Y1Z1, fraction.y),
                    fraction.z) / CellSize.x,
                math.lerp(
                    math.lerp(distanceX0Y1Z0 - distanceX0Y0Z0, distanceX1Y1Z0 - distanceX1Y0Z0, fraction.x),
                    math.lerp(distanceX0Y1Z1 - distanceX0Y0Z1, distanceX1Y1Z1 - distanceX1Y0Z1, fraction.x),
                    fraction.z) / CellSize.y,
                math.lerp(
                    math.lerp(distanceX0Y0Z1 - distanceX0Y0Z0, distanceX1Y0Z1 - distanceX1Y0Z0, fraction.x),
                    math.lerp(distanceX0Y1Z1 - distanceX0Y1Z0, distanceX1Y1Z1 - distanceX1Y1Z0, fraction.x),
                    fraction.y) / CellSize.z);

            return true;
        }

        private float Decode(int x, int y, int z)
        {
            var index = x + Resolution.x * (y + Resolution.y * z);
            return math.f16tof32(EncodedDistances[index]);
        }
    }
}

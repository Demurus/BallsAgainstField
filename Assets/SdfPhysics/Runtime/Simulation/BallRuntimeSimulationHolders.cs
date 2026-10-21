using System;
using Unity.Collections;
using Unity.Mathematics;

namespace SdfPhysics.Runtime.Simulation
{
    /// <summary>
    /// Keeps state of all balls
    /// </summary>
    public sealed class BallRuntimeSimulationHolders : IDisposable
    {
        public NativeArray<float3> Positions { get; }
        public NativeArray<float3> Velocities { get; }
        public NativeArray<float3> SpawnPositions { get; }
        public NativeArray<float> RestTimes { get; }
        public NativeArray<byte> Sleeping { get; }

        public int Count => Positions.Length;

        public BallRuntimeSimulationHolders(int count, Allocator allocator)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Ball count must be positive.");
            }

            Positions = new NativeArray<float3>(count, allocator, NativeArrayOptions.UninitializedMemory); //no need to initialize memory when its gonna be rewritten right away
            Velocities = new NativeArray<float3>(count, allocator, NativeArrayOptions.ClearMemory); //initialize here. Because the starting velocity should be 0
            SpawnPositions = new NativeArray<float3>(count, allocator, NativeArrayOptions.UninitializedMemory);
            RestTimes = new NativeArray<float>(count, allocator, NativeArrayOptions.ClearMemory);
            Sleeping = new NativeArray<byte>(count, allocator, NativeArrayOptions.ClearMemory);
        }

        public void Dispose()
        {
            if (Positions.IsCreated) Positions.Dispose();
            if (Velocities.IsCreated) Velocities.Dispose();
            if (SpawnPositions.IsCreated) SpawnPositions.Dispose();
            if (RestTimes.IsCreated) RestTimes.Dispose();
            if (Sleeping.IsCreated) Sleeping.Dispose();
        }
    }
}

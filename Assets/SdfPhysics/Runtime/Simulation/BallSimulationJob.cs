using SdfPhysics.Runtime.Sdf;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SdfPhysics.Runtime.Simulation
{
    /// <summary>
    /// Job that the ball does in one simulation step
    /// </summary>
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
    public struct BallSimulationJob : IJobFor
    {
        public NativeArray<float3> Positions;
        public NativeArray<float3> Velocities;
        public NativeArray<float> RestTimes;
        public NativeArray<byte> Sleeping;

        [ReadOnly] public NativeArray<float3> SpawnPositions;
        [ReadOnly] public SdfGridData Grid;

        public float3 Gravity;
        public float DeltaTime;
        public float BallRadius;
        public float Bounce;
        public float Friction;
        public float BounceSpeedThreshold;
        public float RestSpeedThreshold;
        public float SleepDelay;
        public int MaximumCollisionSubsteps;
        //Tiny empty space between floor and ball, to not let the ball be treated as penetrated when lying on the ground 
        public float SurfaceSkin;

        public void Execute(int index)
        {
            if (Sleeping[index] != 0)
            {
                return;
            }

            var position = Positions[index];
            var velocity = Velocities[index];
            var restTime = RestTimes[index];
            var sleep = (byte)0;
            var endVelocity = velocity + Gravity * DeltaTime;
            var travel = math.max(math.length(velocity), math.length(endVelocity)) * DeltaTime;
            var maximumTravelPerStep = math.max(0.001f, math.min(BallRadius, math.cmin(Grid.CellSize)));
            var stepCount = math.clamp((int)math.ceil(travel / maximumTravelPerStep), 1, MaximumCollisionSubsteps);
            var stepDuration = DeltaTime / stepCount;

            for (var step = 0; step < stepCount; step++)
            {
                velocity += Gravity * stepDuration;
                position += velocity * stepDuration;

                if (!Grid.TrySample(position, out var sample))
                {
                    // The baked field has no reliable distance or gradient beyond its bounds
                    // If a ball is beyond the edge of the volume - reset it to spawn
                    Positions[index] = SpawnPositions[index];
                    Velocities[index] = float3.zero;
                    RestTimes[index] = 0f;
                    Sleeping[index] = 0;
                    return;
                }

                var penetration = BallRadius - sample.Distance;
                if (penetration < -SurfaceSkin)
                {
                    restTime = 0f;
                    continue;
                }

                // Treat the small skin gap as contact, so a resting ball keeps its sleep timer
                var normal = math.normalizesafe(sample.OutDirection, new float3(0f, 1f, 0f));
                position += normal * math.max(0f, penetration + SurfaceSkin);

                var inwardSpeed = math.dot(velocity, normal);
                if (inwardSpeed < 0f)
                {
                    var bounce = inwardSpeed < -BounceSpeedThreshold ? Bounce : 0f;
                    velocity -= (1f + bounce) * inwardSpeed * normal;
                }

                var tangentialVelocity = velocity - math.dot(velocity, normal) * normal;
                velocity -= tangentialVelocity * math.saturate(Friction * stepDuration);

                if (math.abs(inwardSpeed) <= RestSpeedThreshold &&
                    math.lengthsq(tangentialVelocity) <= RestSpeedThreshold * RestSpeedThreshold)
                {
                    restTime += stepDuration;
                    if (restTime >= SleepDelay)
                    {
                        velocity = float3.zero;
                        sleep = 1;
                        break;
                    }
                }
                else
                {
                    restTime = 0f;
                }
            }

            Positions[index] = position;
            Velocities[index] = velocity;
            RestTimes[index] = restTime;
            Sleeping[index] = sleep;
        }
    }
}

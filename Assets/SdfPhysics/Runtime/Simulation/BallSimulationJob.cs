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
        //Tiny empty space between floor and ball, to not let the ball be treated as penetrated when lying on the ground 
        public float SurfaceSkin;

        public void Execute(int index)
        {
            if (Sleeping[index] != 0)
            {
                return;
            }

            var velocity = Velocities[index] + Gravity * DeltaTime;
            var position = Positions[index] + velocity * DeltaTime;

            if (!Grid.TrySample(position, out var sample))
            {
                //If a ball is beyond the edge of the volume - reset it to spawn
                Positions[index] = SpawnPositions[index];
                Velocities[index] = float3.zero;
                RestTimes[index] = 0f;
                Sleeping[index] = 0;
                return;
            }

            var penetration = BallRadius - sample.Distance;
            var restTime = 0f;
            var sleep = (byte)0;

            if (penetration > 0f)
            {
                var groundUpVector = math.normalizesafe(sample.OutDirection, new float3(0f, 1f, 0f));//TODO try create float 3 once
                position += groundUpVector * (penetration + SurfaceSkin);
                
                var upSpeed = math.dot(velocity, groundUpVector); //what is current up (normal) speed?
                if (upSpeed < 0f) // Ball is moving towards the inside of ground
                {
                    var bounceOut = upSpeed < -BounceSpeedThreshold ? Bounce : 0f; //If ball's down speed (velocity) was fast - let it bounce
                    velocity -= (1f + bounceOut) * upSpeed * groundUpVector;
                }

                var dragVelocity = velocity - math.dot(velocity, groundUpVector) * groundUpVector; //Calculating the velocity of possible horizontal movement
                velocity -= dragVelocity * math.saturate(Friction * DeltaTime);
                
                if (math.abs(upSpeed) <= RestSpeedThreshold && math.lengthsq(dragVelocity) <= RestSpeedThreshold * RestSpeedThreshold) //Stopping the ball if its up speed and rest time are below grades 
                {
                    restTime = RestTimes[index] + DeltaTime;
                    if (restTime >= SleepDelay)
                    {
                        velocity = float3.zero;
                        sleep = 1;
                    }
                }
            }

            Positions[index] = position;
            Velocities[index] = velocity;
            RestTimes[index] = restTime;
            Sleeping[index] = sleep;
        }
    }
}

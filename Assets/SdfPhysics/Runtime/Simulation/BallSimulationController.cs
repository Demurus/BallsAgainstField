using SdfPhysics.Runtime.Sdf;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace SdfPhysics.Runtime.Simulation
{
    /// <summary>
    /// Controls jobs and handles data creation and disposal
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BallSimulationController : MonoBehaviour
    {
        
        [Header("Baked collision")]
        [SerializeField] private SdfBakeSettings _bakeSettings;

        [Header("Ball population")]
        [SerializeField, Min(1)] private int _ballCount = 500;
        [SerializeField] private uint _spawnSeed = 12345;
        [SerializeField] private Vector2 _spawnHalfExtents = new(4.4f, 4.4f);
        [SerializeField] private Vector2 _spawnHeightRange = new(2.3f, 5.4f);

        [Header("Fixed-step simulation")]
        [SerializeField, Min(0.001f)] private float _substepDuration = 1f / 120f;
        [SerializeField, Min(1)] private int _maximumSubstepsPerFrame = 4;
        //Size of the chunks to divide the jobs like 0-127, 128-255...
        [SerializeField, Min(1)] private int _innerLoopBatchSize = 128;
        [SerializeField] private float _gravity = -9.81f;
        [SerializeField, Range(0f, 1f)] private float _restitution = 0.05f;
        [SerializeField, Min(0f)] private float _friction = 8f;
        [SerializeField, Min(0f)] private float _bounceSpeedThreshold = 0.75f;
        [SerializeField, Min(0f)] private float _restSpeedThreshold = 0.15f;
        //ball has to be fully or almost fully stable within this time to mark it out of physics 
        [SerializeField, Min(0f)] private float _sleepDelay = 0.25f;
        [SerializeField, Min(0f)] private float _surfaceSkin = 0.001f;

        private BallRuntimeSimulationHolders _holders;
        private SdfVolumeRuntime _sdfVolume;
        private JobHandle _scheduledHandle;
        private float _accumulator;

        public int BallCount => _holders?.Count ?? 0;
        public bool IsInitialized => _holders != null;

        /// <summary>
        /// Positions for the renderer.
        /// Valid only after the controller has completed its jobs for the current frame.
        /// </summary>
        public NativeArray<float3> RendererPositions => _holders?.Positions ?? default;

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                Initialize();
            }
        }

        private void Update()
        {
            if (_holders == null)
            {
                return;
            }

            // Improving the collision resolution by creating 2 physics substeps at one frame
            _accumulator = math.min(
                _accumulator + Time.deltaTime,
                _substepDuration * _maximumSubstepsPerFrame);

            var substepCount = 0;
            var handle = default(JobHandle);
            while (_accumulator >= _substepDuration && substepCount < _maximumSubstepsPerFrame)
            {
                handle = CreateJob().ScheduleParallel(_holders.Count, _innerLoopBatchSize, handle);
                _accumulator -= _substepDuration;
                substepCount++;
            }

            if (substepCount > 0)
            {
                _scheduledHandle = handle;
                _scheduledHandle.Complete();
                _scheduledHandle = default;
            }
        }

        private BallSimulationJob CreateJob()
        {
            return new BallSimulationJob
            {
                Positions = _holders.Positions,
                Velocities = _holders.Velocities,
                RestTimes = _holders.RestTimes,
                Sleeping = _holders.Sleeping,
                SpawnPositions = _holders.SpawnPositions,
                Grid = _sdfVolume.Data,
                Gravity = new float3(0f, _gravity, 0f),
                DeltaTime = _substepDuration,
                BallRadius = _bakeSettings.BallRadius,
                Bounce = _restitution,
                Friction = _friction,
                BounceSpeedThreshold = _bounceSpeedThreshold,
                RestSpeedThreshold = _restSpeedThreshold,
                SleepDelay = _sleepDelay,
                SurfaceSkin = _surfaceSkin
            };
        }

        private void Initialize()
        {
            DisposeResources();

            if (_bakeSettings == null || _bakeSettings.BakedVolume == null)
            {
                Debug.LogError("Ball simulation requires SdfBakeSettings with a baked volume.", this);
                enabled = false;
                return;
            }

            _sdfVolume = new SdfVolumeRuntime(_bakeSettings.BakedVolume, Allocator.Persistent);
            _holders = new BallRuntimeSimulationHolders(_ballCount, Allocator.Persistent);
            InitializeSpawnPositions();

            Debug.Log($"Initialized {_ballCount:N0} SDF balls with {_substepDuration:F5}s simulation substeps.", this);
        }

        private void InitializeSpawnPositions()
        {
            var random = new Unity.Mathematics.Random(_spawnSeed == 0 ? 1u : _spawnSeed);
            var positions = _holders.Positions;
            var spawnPositions = _holders.SpawnPositions;
            for (var index = 0; index < _holders.Count; index++)
            {
                var position = new float3(
                    random.NextFloat(-_spawnHalfExtents.x, _spawnHalfExtents.x),
                    random.NextFloat(_spawnHeightRange.x, _spawnHeightRange.y),
                    random.NextFloat(-_spawnHalfExtents.y, _spawnHalfExtents.y));

                positions[index] = position;
                spawnPositions[index] = position;
            }
        }

        private void OnDisable()
        {
            DisposeResources();
        }

        private void DisposeResources()
        {
            _scheduledHandle.Complete();
            _scheduledHandle = default;

            _holders?.Dispose();
            _holders = null;

            _sdfVolume?.Dispose();
            _sdfVolume = null;

            _accumulator = 0f;
        }

        private void OnValidate()
        {
            _ballCount = Mathf.Max(1, _ballCount);
            _spawnSeed = _spawnSeed == 0 ? 1u : _spawnSeed;
            _spawnHalfExtents.x = Mathf.Max(0.01f, _spawnHalfExtents.x);
            _spawnHalfExtents.y = Mathf.Max(0.01f, _spawnHalfExtents.y);
            _spawnHeightRange.y = Mathf.Max(_spawnHeightRange.x, _spawnHeightRange.y);
            _substepDuration = Mathf.Max(0.001f, _substepDuration);
            _maximumSubstepsPerFrame = Mathf.Max(1, _maximumSubstepsPerFrame);
            _innerLoopBatchSize = Mathf.Max(1, _innerLoopBatchSize);
        }
    }
}

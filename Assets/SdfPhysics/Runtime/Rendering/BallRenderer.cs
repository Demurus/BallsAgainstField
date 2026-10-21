using SdfPhysics.Runtime.Simulation;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace SdfPhysics.Runtime.Rendering
{
    /// <summary>
    /// Packs completed simulation positions into one GPU
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BallRenderer : MonoBehaviour
    {
        [SerializeField] private BallSimulationController _simulation;
        [SerializeField] private Mesh _ballMesh;
        [SerializeField] private Material _ballMaterial;

        private NativeArray<float4> _instanceData;
        private GraphicsBuffer _instanceBuffer;
        private GraphicsBuffer _commandBuffer;
        private MaterialPropertyBlock _properties;
        private int _capacity;

        private void LateUpdate()
        {
            if (_simulation == null || !_simulation.IsInitialized || _ballMesh == null || _ballMaterial == null)
            {
                return;
            }

            CheckResources(_simulation.BallCount);
            UploadInstanceData(_simulation.RendererPositions, _simulation.BallRadius);

            var bounds = _simulation.SimulationBounds;
            bounds.Expand(_simulation.BallRadius * 2f);
            var renderParameters = new RenderParams(_ballMaterial)
            {
                worldBounds = bounds,
                matProps = _properties,
                layer = gameObject.layer
            };

            Graphics.RenderMeshIndirect(renderParameters, _ballMesh, _commandBuffer);
        }

        private void CheckResources(int count)
        {
            if (_capacity == count && _instanceBuffer != null && _commandBuffer != null)
            {
                return;
            }

            ReleaseResources();

            _capacity = count;
            _instanceData = new NativeArray<float4>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _instanceBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, sizeof(float) * 4);
            _commandBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.IndirectArguments,
                1,
                GraphicsBuffer.IndirectDrawIndexedArgs.size);

            var command = new GraphicsBuffer.IndirectDrawIndexedArgs
            {
                indexCountPerInstance = _ballMesh.GetIndexCount(0),
                instanceCount = (uint)count,
                startIndex = _ballMesh.GetIndexStart(0),
                baseVertexIndex = _ballMesh.GetBaseVertex(0),
                startInstance = 0
            };
            _commandBuffer.SetData(new[] { command });

            _properties ??= new MaterialPropertyBlock();
            _properties.SetBuffer("_BallInstanceData", _instanceBuffer);
            _ballMaterial.enableInstancing = true;
        }

        private void UploadInstanceData(NativeArray<float3> positions, float radius)
        {
            for (var index = 0; index < _instanceData.Length; index++)
            {
                _instanceData[index] = new float4(positions[index], radius);
            }

            _instanceBuffer.SetData(_instanceData);
        }

        private void OnDisable()
        {
            ReleaseResources();
        }

        private void ReleaseResources()
        {
            if (_instanceData.IsCreated)
            {
                _instanceData.Dispose();
            }

            _instanceBuffer?.Release();
            _instanceBuffer = null;
            _commandBuffer?.Release();
            _commandBuffer = null;
            _capacity = 0;
        }
    }
}

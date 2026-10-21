using Unity.Mathematics;

namespace SdfPhysics.Runtime.Sdf
{
    public struct SDFSample
    {
        //Signed distance to geometry 
        public float Distance;
        //Direction outwards of geometry
        public float3 OutDirection;
    }
}
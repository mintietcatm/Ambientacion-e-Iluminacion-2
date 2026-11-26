#ifndef FAKE_INTERIOR_MAPPING_INCLUDED
#define FAKE_INTERIOR_MAPPING_INCLUDED

// UV0 describes one window. Tangent +Z points out of its surface.
void FakeInteriorDirection_float(float2 UV, float3 ViewDirection, float Depth, out float3 Direction)
{
    float roomDepth = max(Depth, 0.001);
    float3 origin = float3(UV * 2.0 - 1.0, 1.0);
    float3 ray = -ViewDirection;
    ray.z /= roomDepth;

    // Parallel rays must not produce infinities (or 0 * infinity at an edge).
    float3 raySign = step(0.0, ray) * 2.0 - 1.0;
    float3 safeRay = raySign * max(abs(ray), 0.00001);
    float3 distances = (raySign - origin) / safeRay;
    float distanceToWall = min(min(distances.x, distances.y), distances.z);
    Direction = origin + safeRay * max(distanceToWall, 0.0);
}

void FakeInteriorDirection_half(half2 UV, half3 ViewDirection, half Depth, out half3 Direction)
{
    float3 direction;
    FakeInteriorDirection_float(UV, ViewDirection, Depth, direction);
    Direction = direction;
}

#endif

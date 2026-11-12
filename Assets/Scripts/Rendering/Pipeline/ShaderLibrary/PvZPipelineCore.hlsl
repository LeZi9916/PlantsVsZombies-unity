// Minimal shader library for PvZRenderPipeline. It only declares the engine-provided transforms that unlit
// 2D shaders need, so shaders do not depend on any render pipeline package.
#ifndef PVZ_PIPELINE_CORE_INCLUDED
#define PVZ_PIPELINE_CORE_INCLUDED

#define CBUFFER_START(name) cbuffer name {
#define CBUFFER_END };

// Per-draw values filled by the engine. The SRP Batcher requires these built-ins in a buffer named UnityPerDraw.
CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
    float4 unity_LODFade;
    float4 unity_WorldTransformParams;
CBUFFER_END

// Per-camera value set by ScriptableRenderContext.SetupCameraProperties, including the render-texture flip.
float4x4 unity_MatrixVP;

#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_VP unity_MatrixVP

// Transforms an object-space position to homogeneous clip space.
float4 TransformObjectToHClip(float3 positionOS)
{
    float3 positionWS = mul(UNITY_MATRIX_M, float4(positionOS, 1.0)).xyz;
    return mul(UNITY_MATRIX_VP, float4(positionWS, 1.0));
}

#endif

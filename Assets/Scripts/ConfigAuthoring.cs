using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ConfigAuthoring : MonoBehaviour
{
    public GameObject Prefab;
    public uint Size; // Number of cubes in depth and width
    public float WaveAmplitude;
    public float WaveFrequency;
    public float WaveDamping;
    public float WaveSpeed;
    public float CubePositionMultiplier; // Gap between cubes
    public float LifetimeOffset = 0.1f; // adds % more time

    class Baker : Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            // Calculate the grid length based on the number of cubes and the position multiplier
            float gridLength = authoring.Size * authoring.CubePositionMultiplier;

            // Calculate the diagonal distance across the grid (the longest distance a wave might travel)
            float diagonalDistance = math.sqrt(2f) * gridLength;

            // Calculate the wave lifetime based on diagonal distance and wave speed, with an offset
            float calculatedLifetime = (diagonalDistance / authoring.WaveSpeed) * (1.0f + authoring.LifetimeOffset);

            AddComponent(entity, new Config
            {
                Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
                Size = authoring.Size,
                WaveAmplitude = authoring.WaveAmplitude,
                WaveFrequency = authoring.WaveFrequency,
                WaveDamping = authoring.WaveDamping,
                WaveSpeed = authoring.WaveSpeed,
                WaveLifetime = calculatedLifetime,
                CubePositionMultiplier = authoring.CubePositionMultiplier
            });
            AddComponent<Hit>(entity);
        }
    }
}

public struct Config : IComponentData
{
    public Entity Prefab;
    public uint Size;
    public float WaveAmplitude;
    public float WaveFrequency;
    public float WaveDamping;
    public float WaveSpeed;
    public float WaveLifetime;
    public float CubePositionMultiplier;
}

public struct Hit : IComponentData
{
    public float3 Value;
    public bool HitChanged;
    public float HitTime;
}
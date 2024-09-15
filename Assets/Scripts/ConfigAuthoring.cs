using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ConfigAuthoring : MonoBehaviour
{
    // Public variables for configuring the wave system in the Unity inspector
    public GameObject Prefab;
    public uint Size; // Number of cubes in depth and width (for the grid)
    public float WaveAmplitude; // Height of the wave
    public float WaveFrequency; // Frequency of the wave (oscillations per second)
    public float WaveDamping; // How fast the wave diminishes over time
    public float WaveSpeed; // Speed at which the wave travels
    public float CubePositionMultiplier; // Spacing between cubes in the grid
    public float LifetimeOffset = 0.1f; // Additional percentage of lifetime for the waves

    public Color TopColor; // Color at the peak of the wave
    public Color BottomColor; // Color at the base of the wave

    // Baker class to convert authoring data into entity components
    class Baker : Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            // Calculate the grid's total length (the side length of the cube grid)
            float gridLength = authoring.Size * authoring.CubePositionMultiplier;

            // Calculate the diagonal distance across the grid (the maximum distance a wave might travel)
            float diagonalDistance = math.sqrt(2f) * gridLength;

            // Calculate wave lifetime based on the diagonal distance and wave speed
            float calculatedLifetime = (diagonalDistance / authoring.WaveSpeed) * (1.0f + authoring.LifetimeOffset);

            // Add the Config component to the entity
            AddComponent(entity, new Config
            {
                Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
                Size = authoring.Size,
                WaveAmplitude = authoring.WaveAmplitude,
                WaveFrequency = authoring.WaveFrequency,
                WaveDamping = authoring.WaveDamping,
                WaveSpeed = authoring.WaveSpeed,
                WaveLifetime = calculatedLifetime, // Calculated lifetime of the wave
                CubePositionMultiplier = authoring.CubePositionMultiplier,
                TopColor = new float3(authoring.TopColor.r, authoring.TopColor.g, authoring.TopColor.b),
                BottomColor = new float3(authoring.BottomColor.r, authoring.BottomColor.g, authoring.BottomColor.b)
            });

            // Add a Hit component to track input interactions
            AddComponent<Hit>(entity);
        }
    }
}

// Data structure to store wave configuration data for the system
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

    public float3 TopColor;
    public float3 BottomColor;
}

// Component to store data about input hits
public struct Hit : IComponentData
{
    public float3 Value; // The position of the hit
    public bool HitChanged; // Whether a new hit was detected
    public float HitTime; // The time the hit occurred
}

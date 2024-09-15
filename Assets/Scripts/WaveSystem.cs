using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

public partial struct WaveSystem : ISystem
{
    private NativeList<WaveData> activeWaves; // List storing data for active waves

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Initialize the list to store waves and ensure necessary components are available
        activeWaves = new NativeList<WaveData>(Allocator.Persistent);
        state.RequireForUpdate<Config>(); // Require configuration component
        state.RequireForUpdate<Hit>(); // Require hit detection component
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        // Dispose of the list when the system is destroyed to free memory
        if (activeWaves.IsCreated)
        {
            activeWaves.Dispose();
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>(); // Get configuration data
        var hit = SystemAPI.GetSingleton<Hit>(); // Get hit data
        float currentTime = (float)SystemAPI.Time.ElapsedTime; // Get the current time

        // If a new hit is detected, add a new wave to the active wave list
        if (hit.HitChanged)
        {
            activeWaves.Add(new WaveData
            {
                HitTime = (float)SystemAPI.Time.ElapsedTime,
                HitPosition = hit.Value
            });
        }

        // Remove waves that have surpassed their lifetime
        float waveLifetime = config.WaveLifetime;
        for (int i = activeWaves.Length - 1; i >= 0; i--)
        {
            if (currentTime - activeWaves[i].HitTime > waveLifetime)
            {
                activeWaves.RemoveAtSwapBack(i); // Efficient removal from the list
            }
        }

        // Schedule a job to animate the waves and update the system's dependency
        var jobHandle = new WaveAnimationJob
        {
            Time = currentTime,
            Waves = activeWaves.AsDeferredJobArray(),
            WaveAmplitude = config.WaveAmplitude,
            WaveFrequency = config.WaveFrequency,
            WaveDamping = config.WaveDamping,
            WaveSpeed = config.WaveSpeed,
            TopColor = config.TopColor,
            BottomColor = config.BottomColor
        }.ScheduleParallel(state.Dependency);

        state.Dependency = jobHandle; // Update the system's job dependency
    }
}

// Data structure to store information about each wave
struct WaveData
{
    public float HitTime; // The time the wave was created
    public float3 HitPosition; // The position of the hit that initiated the wave
}

[BurstCompile]
partial struct WaveAnimationJob : IJobEntity
{
    [ReadOnly] public float Time; // The current time
    [ReadOnly] public NativeArray<WaveData> Waves; // Array of active waves
    [ReadOnly] public float WaveAmplitude; // Amplitude of the waves
    [ReadOnly] public float WaveFrequency; // Frequency of the waves
    [ReadOnly] public float WaveDamping; // Damping factor reducing wave strength over time
    [ReadOnly] public float WaveSpeed; // Speed at which the waves propagate
    [ReadOnly] public float3 TopColor; // Color at the top of the wave
    [ReadOnly] public float3 BottomColor; // Color at the bottom of the wave

    void Execute(ref LocalTransform transform, ref URPMaterialPropertyBaseColor baseColor, in WaveObjectTag waveObjectTag)
    {
        float totalWaveHeight = 0f; // Accumulated wave height from all waves

        // Loop through all waves to combine their effects
        for (int i = 0; i < Waves.Length; i++)
        {
            var wave = Waves[i];
            float2 horizontalDistance = transform.Position.xz - wave.HitPosition.xz; // Distance from the wave origin to the object
            float distance = math.length(horizontalDistance);
            float timeSinceHit = Time - wave.HitTime;

            // Calculate the time required for the wave to reach the object
            float timeToReachCube = distance / WaveSpeed;
            float adjustedTime = timeSinceHit - timeToReachCube;

            // If the wave has reached the object, calculate its effect
            if (adjustedTime > 0)
            {
                float phase = WaveFrequency * adjustedTime;
                float damping = math.exp(-WaveDamping * adjustedTime); // Apply damping over time
                float waveHeight = WaveAmplitude * damping * math.sin(phase);

                // Accumulate the total wave height
                totalWaveHeight += waveHeight;
            }
        }

        // Update the object's y position based on the combined wave effects
        transform.Position.y = math.abs(totalWaveHeight) < 0.001f ? 0 : totalWaveHeight;

        // Calculate the interpolated color based on the wave height
        float normalizedHeight = math.clamp((transform.Position.y / WaveAmplitude) * 0.5f + 0.5f, 0f, 1f);
        float3 interpolatedColor = math.lerp(BottomColor, TopColor, normalizedHeight);

        // Set the color of the object
        baseColor.Value = new float4(interpolatedColor, 1f); // Set the RGBA color
    }
}

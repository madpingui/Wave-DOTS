using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct WaveSystem : ISystem
{
    private NativeList<WaveData> activeWaves; // Native list for active waves

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Initialize the NativeList to store wave data
        activeWaves = new NativeList<WaveData>(Allocator.Persistent);
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<Hit>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        // Dispose of the NativeList when the system is destroyed
        if (activeWaves.IsCreated)
        {
            activeWaves.Dispose();
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>();
        var hit = SystemAPI.GetSingleton<Hit>();
        float currentTime = (float)SystemAPI.Time.ElapsedTime;

        // If a new wave is initiated, add it to the native list
        if (hit.HitChanged)
        {
            activeWaves.Add(new WaveData
            {
                HitTime = (float)SystemAPI.Time.ElapsedTime,
                HitPosition = hit.Value
            });
        }

        // Remove old waves beyond their lifetime
        float waveLifetime = config.WaveLifetime;
        for (int i = activeWaves.Length - 1; i >= 0; i--)
        {
            if (currentTime - activeWaves[i].HitTime > waveLifetime)
            {
                activeWaves.RemoveAtSwapBack(i);
            }
        }

        // Schedule the wave animation job
        var jobHandle = new WaveAnimationJob
        {
            Time = currentTime,
            Waves = activeWaves.AsDeferredJobArray(),
            WaveAmplitude = config.WaveAmplitude,
            WaveFrequency = config.WaveFrequency,
            WaveDamping = config.WaveDamping,
            WaveSpeed = config.WaveSpeed
        }.ScheduleParallel(state.Dependency);

        // Update the system's dependency
        state.Dependency = jobHandle;
    }
}

// Struct to store wave data
struct WaveData
{
    public float HitTime;
    public float3 HitPosition;
}

[BurstCompile]
partial struct WaveAnimationJob : IJobEntity
{
    [ReadOnly] public float Time;
    [ReadOnly] public NativeArray<WaveData> Waves;
    [ReadOnly] public float WaveAmplitude;
    [ReadOnly] public float WaveFrequency;
    [ReadOnly] public float WaveDamping;
    [ReadOnly] public float WaveSpeed;

    void Execute(ref LocalTransform transform, in WaveObjectTag waveObjectTag)
    {
        float totalWaveHeight = 0f;

        // Iterate over all waves and combine their effects
        for (int i = 0; i < Waves.Length; i++)
        {
            var wave = Waves[i];
            float timeSinceHit = Time - wave.HitTime;
            float2 horizontalDistance = transform.Position.xz - wave.HitPosition.xz;
            float distance = math.length(horizontalDistance);

            // Skip if entity is beyond the wave's reach
            if (distance > WaveSpeed * timeSinceHit)
            {
                continue;
            }

            // Delay the wave based on distance from the hit position
            float adjustedTime = timeSinceHit - (distance / WaveSpeed);

            // Only apply the wave if it has reached the cube
            if (adjustedTime > 0)
            {
                float phase = WaveFrequency * adjustedTime;
                float damping = math.exp(-WaveDamping * adjustedTime);
                float waveHeight = WaveAmplitude * damping * math.sin(phase);

                // Accumulate the wave heights to combine the effects
                totalWaveHeight += waveHeight;
            }
        }

        // Update only the y position, combining the effects of all waves
        // Reset to the original position if the combined effect is small
        transform.Position.y = math.abs(totalWaveHeight) < 0.001f ? 0 : totalWaveHeight;
    }
}
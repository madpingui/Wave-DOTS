using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct CubeSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;
        var config = SystemAPI.GetSingleton<Config>();

        // Use EntityCommandBuffer for better performance
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        // Use job to set positions in parallel
        new SetPositionsJob
        {
            ecb = ecb,
            config = config
        }.ScheduleParallel();
    }

    [BurstCompile]
    partial struct SetPositionsJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public Config config;

        public void Execute([ChunkIndexInQuery]int chunkIndex)
        {
            var center = (config.Size - 1) / 2f;

            for (int i = 0; i < (int)(config.Size * config.Size); i++)
            {
                var newEntity = ecb.Instantiate(chunkIndex, config.Prefab);
                var position = new float3((i % config.Size - center) * config.CubePositionMultiplier, 0, (i / config.Size - center) * config.CubePositionMultiplier);

                ecb.SetComponent(chunkIndex, newEntity, new LocalTransform
                {
                    Position = position,
                    Scale = 1,
                    Rotation = quaternion.identity
                });

                ecb.AddComponent<WaveObjectTag>(chunkIndex, newEntity);
            }
        }
    }
}

public struct WaveObjectTag : IComponentData
{
}


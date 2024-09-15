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
        // Ensure the system updates only when the 'Config' component exists
        state.RequireForUpdate<Config>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Disable the system after running once (since cubes are spawned only once)
        state.Enabled = false;

        // Retrieve the Config singleton, which holds configuration data for spawning cubes
        var config = SystemAPI.GetSingleton<Config>();

        // Get the EntityCommandBuffer for efficient entity operations (instantiation, setting components, etc.)
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                            .CreateCommandBuffer(state.WorldUnmanaged)
                            .AsParallelWriter();

        // Schedule the job to set cube positions in parallel using the job system
        new SetPositionsJob
        {
            ecb = ecb, // Pass the command buffer for entity commands
            config = config // Pass the configuration data for grid size, spacing, and prefab
        }.ScheduleParallel(); // Schedule the job to run across multiple threads
    }

    [BurstCompile]
    partial struct SetPositionsJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb; // Command buffer for parallel entity operations
        public Config config; // Configuration data for cube size, prefab, and spacing

        // Executes the job for each entity chunk in the query
        public void Execute([ChunkIndexInQuery] int chunkIndex)
        {
            // Calculate the center point of the grid for cube placement
            var center = (config.Size - 1) / 2f;

            // Loop through the grid to spawn entities (cubes) at each grid position
            for (int i = 0; i < (int)(config.Size * config.Size); i++)
            {
                // Instantiate a new cube entity at the current grid index
                var newEntity = ecb.Instantiate(chunkIndex, config.Prefab);

                // Calculate the position of the cube based on the grid index and center
                var position = new float3((i % config.Size - center) * config.CubePositionMultiplier,
                                           0,
                                           (i / config.Size - center) * config.CubePositionMultiplier);

                // Set the position, scale, and rotation for the new cube entity
                ecb.SetComponent(chunkIndex, newEntity, new LocalTransform
                {
                    Position = position, // Position the cube in the grid
                    Scale = 1, // Set the cube scale
                    Rotation = quaternion.identity // Set the cube rotation to no rotation (identity)
                });

                // Add a tag component to mark this entity as part of the wave system
                ecb.AddComponent<WaveObjectTag>(chunkIndex, newEntity);
            }
        }
    }
}

// Tag component to mark entities as part of the wave system
public struct WaveObjectTag : IComponentData
{
}
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

public partial struct InputSystem : ISystem
{
    private bool wasMouseDown; // Tracks whether the mouse was pressed in the last frame

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Require 'Hit' and 'Config' components for the system to run
        state.RequireForUpdate<Hit>();
        state.RequireForUpdate<Config>();
        wasMouseDown = false; // Initialize mouse state
    }

    public void OnUpdate(ref SystemState state)
    {
        var hit = SystemAPI.GetSingletonRW<Hit>(); // Get the Hit component
        hit.ValueRW.HitChanged = false; // Reset the hit changed flag

        // Exit if there's no main camera (which is essential for raycasting)
        if (Camera.main == null)
        {
            return;
        }

        // Check if the left mouse button is pressed
        bool isMouseDown = Input.GetMouseButton(0);

        // Trigger a hit event if the mouse button is pressed for the first time
        if (isMouseDown && !wasMouseDown)
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition); // Create a ray from the camera to the mouse position
            if (new Plane(Vector3.up, 0f).Raycast(ray, out var dist)) // Check if the ray hits the ground plane
            {
                // Update the Hit component with the hit position and time
                hit.ValueRW.HitChanged = true;
                hit.ValueRW.Value = ray.GetPoint(dist); // Get the hit point
                hit.ValueRW.HitTime = (float)SystemAPI.Time.ElapsedTime; // Record the time of the hit
            }
        }

        // Store the current mouse state for the next frame
        wasMouseDown = isMouseDown;
    }
}

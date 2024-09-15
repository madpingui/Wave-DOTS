using Unity.Burst;
using Unity.Entities;
using UnityEngine;

public partial struct InputSystem : ISystem
{
    private bool wasMouseDown;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Hit>();
        state.RequireForUpdate<Config>();
        wasMouseDown = false;
    }

    public void OnUpdate(ref SystemState state)
    {
        var hit = SystemAPI.GetSingletonRW<Hit>();
        hit.ValueRW.HitChanged = false;

        if (Camera.main == null)
        {
            return;
        }

        bool isMouseDown = Input.GetMouseButton(0);

        // Only create a wave on the frame when the mouse button is first pressed
        if (isMouseDown && !wasMouseDown)
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (new Plane(Vector3.up, 0f).Raycast(ray, out var dist))
            {
                hit.ValueRW.HitChanged = true;
                hit.ValueRW.Value = ray.GetPoint(dist);
                hit.ValueRW.HitTime = (float)SystemAPI.Time.ElapsedTime;
            }
        }

        wasMouseDown = isMouseDown;
    }
}

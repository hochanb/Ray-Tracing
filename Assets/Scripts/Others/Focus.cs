using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RayTracingManager))]
public class Focus : MonoBehaviour, ITickUpdate
{
    [SerializeField] Transform target;

    RayTracingManager manager;

    private void Start()
    {
        manager ??= GetComponent<RayTracingManager>();
        SetFocus();
    }
    private void Update()
    {
        SetFocus();

    }

    public void LateTickUpdate(float dt, bool skip)
    {
        SetFocus();
    }

    [Button]
    public void SetFocus()
    {
        Ray ray = new Ray();
        ray.origin = transform.position;
        if(target != null )
            ray.direction = target.position - ray.origin;
        else
            ray.direction = transform.forward;
        ray.direction.Normalize();
        if(Physics.Raycast(ray,out var hitInfo))
        {
            manager ??= GetComponent<RayTracingManager>();
            manager.FocusDistance = Vector3.Dot(transform.forward, hitInfo.distance * ray.direction);

        }
    }
}

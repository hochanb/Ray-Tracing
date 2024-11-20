using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RayTracingManager))]
public class Focus : MonoBehaviour
{
    [SerializeField] Transform target;

    private void Start()
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
            var manager = GetComponent<RayTracingManager>();
            manager.FocusDistance = Vector3.Dot(transform.forward, hitInfo.distance * ray.direction);

        }
    }
}

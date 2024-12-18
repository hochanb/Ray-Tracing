using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VCamTrackRide : MonoBehaviour, ITickUpdate
{
    [SerializeField] CinemachineVirtualCamera vcam;
    [SerializeField] float playTime = 1f;
    [SerializeField] bool startFromZero = true;

    private CinemachineTrackedDolly dolly;
    private CinemachinePathBase dollyPath;

    float cachedZ;
    private void Awake()
    {
        dolly = vcam.GetCinemachineComponent<CinemachineTrackedDolly>();
        dollyPath = dolly.m_Path;
        if (startFromZero)
            dolly.m_PathPosition = 0;
    }
    public void TickUpdate(float dt)
    {
        if (playTime <= 0.01f || dolly == null || dollyPath == null) return;

        // Map the normalized value (0 to 1) to the path's total length
        float z = dolly.m_PathPosition;  // normalized
        z += dt / playTime;
        dolly.m_PathPosition = z;

    }


}

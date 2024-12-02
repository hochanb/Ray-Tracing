using UnityEngine;
using System.IO;

public class Recorder : MonoBehaviour,ITickUpdate
{
    [SerializeField] RayTracingManager rayTracingManager;

    string outputFolder = "Assets/Captures/Frames/"; // ���� ���� �̸�

    private int frameCount = 0;

    bool wait;

    public void EarlyTickUpdate(float dt)
    {
        CaptureFrame();
    }


    void CaptureFrame()
    {
        string fileName = $"frame_{frameCount:D4}.png"; // 0000, 0001, 0002 ������ ���� �̸�
        string filePath = outputFolder + fileName ;

        // ���� ȭ�� ĸó
        ScreenCapture.CaptureScreenshot(filePath);
        Debug.Log($"Captured frame saved at: {filePath}");
        frameCount++;

        wait = true;
    }

    private void Update()
    {
        if(!wait) return;

        wait = false;
        rayTracingManager.ResetRender();
    }
}
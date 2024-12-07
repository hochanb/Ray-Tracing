using UnityEngine;
using System.IO;

public class Recorder : MonoBehaviour,ITickUpdate
{
    [SerializeField] RayTracingManager rayTracingManager;
    [SerializeField] CanvasController[] canvas;

    string outputFolder = "Assets/Captures/Frames/"; // 저장 폴더 이름

    private int frameCount = 0;

    bool wait;

    private void Awake()
    {
        if (GameSettings.Instance != null)
        {
            outputFolder = GameSettings.Instance.SavePath;
        }
    }

    public void EarlyTickUpdate(float dt, bool skip)
    {
        if (skip)
        {
            frameCount++;
            return;
        }
        CaptureFrame();
    }


    void CaptureFrame()
    {
        string fileName = $"frame_{frameCount:D4}.png"; // 0000, 0001, 0002 형식의 파일 이름
        string filePath = outputFolder + fileName ;

        // 현재 화면 캡처
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
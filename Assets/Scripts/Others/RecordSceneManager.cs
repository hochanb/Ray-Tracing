using UnityEngine;

public class RecordSceneManager : MonoBehaviour
{
    [SerializeField] TickManager tickManager;
    [SerializeField] RayTracingManager rayTracingManager;
    [SerializeField] GameObject doneTMP;
    private void OnEnable()
    {
        tickManager.onTickAllDone += OnTickDone;
    }
    private void OnDisable()
    {
        
        tickManager.onTickAllDone -= OnTickDone;
    }

    void OnTickDone()
    {
        doneTMP.gameObject.SetActive(true);
        rayTracingManager.enabled = false;
        if(GameSettings.Instance != null)
        {
            FFmpeg.BuildVideo(GameSettings.Instance.SavePath, GameSettings.Instance.SavePath + "output.mp4", GameSettings.Instance.framerate);
        }
    }
} 

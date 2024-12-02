using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using System.Threading.Tasks;

public class FFmpegEditorWindow : EditorWindow
{
    private string captureFolder = "Assets/Captures/Frames";
    private string outputVideoPath = "Assets/Captures/output.mp4";
    private int framerate = 30;

    // FFmpeg 경로
    private string ffmpegPath = "Assets/Scripts/Editor/ffmpeg/bin/ffmpeg.exe";
    // 프리뷰 관련 변수
    private Object videoAsset;
    private Vector2 scrollPosition;


    [MenuItem("Tools/FFmpeg Video Builder")]
    public static void ShowWindow()
    {
        GetWindow<FFmpegEditorWindow>("FFmpeg Video Builder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Video Builder Settings", EditorStyles.boldLabel);

        // 캡처 폴더 설정
        GUILayout.Label("Capture Folder");
        captureFolder = EditorGUILayout.TextField("Folder Path", captureFolder);

        // 출력 파일 경로
        GUILayout.Label("Output Video Path");
        outputVideoPath = EditorGUILayout.TextField("Video Path", outputVideoPath);

        // 프레임 레이트 설정
        GUILayout.Label("Framerate");
        framerate = EditorGUILayout.IntField("Framerate", framerate);

        GUILayout.Space(10);

        // 영상 빌드 버튼
        if (GUILayout.Button("Build Video"))
        {
            BuildVideo();
        }
        // 프리뷰 버튼
        if (GUILayout.Button("Load Preview"))
        {
            LoadPreview();
        }

        GUILayout.Space(20);

        // 프리뷰 패널
        GUILayout.Label("Video Preview", EditorStyles.boldLabel);
        if (videoAsset != null)
        {


            Texture previewTexture = AssetPreview.GetAssetPreview(videoAsset);

            // 프리뷰 텍스처가 null인지 확인
            if (previewTexture != null)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));

                // Inspector 스타일로 프리뷰를 표시
                EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(256, 256), AssetPreview.GetAssetPreview(videoAsset));

                EditorGUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label("No preview available for this video format.");
            }
        }
        else
        {
            GUILayout.Label("No video loaded. Click 'Load Preview' to load a video.");
        }
    }


    private void LoadPreview()
    {
        if (File.Exists(outputVideoPath))
        {
            // 비디오 파일 Unity 프로젝트로 로드
            string relativePath = "Assets" + outputVideoPath;
            videoAsset = AssetDatabase.LoadAssetAtPath<Object>(outputVideoPath);

            if (videoAsset == null)
            {
                UnityEngine.Debug.LogError("Failed to load video asset for preview.");
            }
        }
        else
        {
            UnityEngine.Debug.LogError($"Video file does not exist: {outputVideoPath}");
        }
    }

    private async void BuildVideo()
    {
        if (!Directory.Exists(captureFolder))
        {
            UnityEngine.Debug.LogError($"Capture folder does not exist: {captureFolder}");
            return;
        }

        if (string.IsNullOrEmpty(outputVideoPath))
        {
            UnityEngine.Debug.LogError("Output video path is not set.");
            return;
        }

        if (!File.Exists(ffmpegPath))
        {
            UnityEngine.Debug.LogError($"FFmpeg not found at: {ffmpegPath}");
            return;
        }

        if (!File.Exists($"{captureFolder}/frame_0000.png"))
        {
            UnityEngine.Debug.LogError("There should be at least one frame, starting with \"frame_0000.png\"");
            return;
        }
        AssetDatabase.Refresh(); // Unity 프로젝트 갱신

        string ffmpegCommand = $"-framerate {framerate} -i \"{captureFolder}/frame_%04d.png\" -c:v libx264 -profile:v baseline -preset fast  -pix_fmt yuv420p \"{outputVideoPath}\"";

        UnityEngine.Debug.Log("Starting FFmpeg process...");
        bool success = await RunFFmpegAsync(ffmpegPath, ffmpegCommand);

        if (success)
        {
            UnityEngine.Debug.Log($"Video built successfully: {outputVideoPath}");
            AssetDatabase.Refresh(); // Unity 프로젝트 갱신
        }
        else
        {
            UnityEngine.Debug.LogError("FFmpeg process failed.");
        }
    }

    private Task<bool> RunFFmpegAsync(string ffmpegPath, string arguments)
    {
        return Task.Run(() =>
        {
            try
            {
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    UnityEngine.Debug.Log($"FFmpeg Output: {output}");
                    return true;
                }
                else
                {
                    UnityEngine.Debug.LogError($"FFmpeg Error: {error}");
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"FFmpeg execution failed: {ex.Message}");
                return false;
            }
        });
    }
}

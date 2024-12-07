using UnityEditor;
using UnityEngine;
using System.IO;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using System.Threading.Tasks;

public class FFmpegEditorWindow : EditorWindow
{
    private string captureFolder = "Assets/Captures/Frames";
    private string outputVideoPath = "Assets/Captures/output.mp4";
    private int framerate = 30;

    // FFmpeg 경로
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
            FFmpeg.BuildVideo(captureFolder,outputVideoPath,framerate);
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

}

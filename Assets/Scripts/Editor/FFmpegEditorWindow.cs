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

    // FFmpeg ���
    // ������ ���� ����
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

        // ĸó ���� ����
        GUILayout.Label("Capture Folder");
        captureFolder = EditorGUILayout.TextField("Folder Path", captureFolder);

        // ��� ���� ���
        GUILayout.Label("Output Video Path");
        outputVideoPath = EditorGUILayout.TextField("Video Path", outputVideoPath);

        // ������ ����Ʈ ����
        GUILayout.Label("Framerate");
        framerate = EditorGUILayout.IntField("Framerate", framerate);

        GUILayout.Space(10);

        // ���� ���� ��ư
        if (GUILayout.Button("Build Video"))
        {
            FFmpeg.BuildVideo(captureFolder,outputVideoPath,framerate);
        }
        // ������ ��ư
        if (GUILayout.Button("Load Preview"))
        {
            LoadPreview();
        }

        GUILayout.Space(20);

        // ������ �г�
        GUILayout.Label("Video Preview", EditorStyles.boldLabel);
        if (videoAsset != null)
        {


            Texture previewTexture = AssetPreview.GetAssetPreview(videoAsset);

            // ������ �ؽ�ó�� null���� Ȯ��
            if (previewTexture != null)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));

                // Inspector ��Ÿ�Ϸ� �����並 ǥ��
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
            // ���� ���� Unity ������Ʈ�� �ε�
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

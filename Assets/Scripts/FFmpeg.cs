using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using System.IO;

public static class FFmpeg
{
    private const string ffmpegPath = "Assets/Scripts/Editor/ffmpeg/bin/ffmpeg.exe";

    public static async void BuildVideo(string captureFolder, string outputPath, int framerate)
    {
        if (!Directory.Exists(captureFolder))
        {
            UnityEngine.Debug.LogError($"Capture folder does not exist: {captureFolder}");
            return;
        }

        if (string.IsNullOrEmpty(outputPath))
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
#if UNITY_EDITOR
        AssetDatabase.Refresh(); // Unity 프로젝트 갱신
#endif
        string ffmpegCommand = $"-framerate {framerate} -i \"{captureFolder}/frame_%04d.png\" -c:v libx264 -profile:v baseline -preset fast  -pix_fmt yuv420p \"{outputPath}\"";

        UnityEngine.Debug.Log("Starting FFmpeg process...");
        bool success = await RunFFmpegAsync(ffmpegPath, ffmpegCommand);

        if (success)
        {
            UnityEngine.Debug.Log($"Video built successfully: {outputPath}");
#if UNITY_EDITOR

            AssetDatabase.Refresh(); // Unity 프로젝트 갱신
#endif
        }
        else
        {
            UnityEngine.Debug.LogError("FFmpeg process failed.");
        }
    }

    private static Task<bool> RunFFmpegAsync(string ffmpegPath, string arguments)
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

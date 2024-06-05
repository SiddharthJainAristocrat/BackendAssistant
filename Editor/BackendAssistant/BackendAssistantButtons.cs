using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityToolbarExtender;
using Debug = UnityEngine.Debug;

namespace BlueprintStudios.Editor.Tools
{
    static class ToolbarStyles
    {
        public static readonly GUIStyle commandButtonStyle;

        static ToolbarStyles()
        {
            commandButtonStyle = new GUIStyle("Command")
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                imagePosition = ImagePosition.ImageAbove,
                fontStyle = FontStyle.Bold,
                fixedWidth = 60,
                fixedHeight = 20
            };
        }
    }

    [InitializeOnLoad]
    public class BuildAndRunButtons
    {
        static readonly string ProcessIdKey = "BuildAndRun_ProcessId";

        static BuildAndRunButtons()
        {
            ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            bool startServerOnPlay = EditorPrefs.GetBool("BuildAndRun_StartServerOnPlay", false);
            bool stopServerOnStop = EditorPrefs.GetBool("BuildAndRun_StopServerOnStop", false);

            if (state == PlayModeStateChange.ExitingEditMode && startServerOnPlay)
            {
                if (!IsServerRunning())
                {
                    EditorApplication.isPlaying = false;
                    StartServerAndPlay();
                }
            }

            if (state == PlayModeStateChange.ExitingPlayMode && stopServerOnStop)
            {
                StopServer();
            }
        }

        static void OnToolbarGUI()
        {
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent("Build", "Build Solution"), ToolbarStyles.commandButtonStyle))
            {
                string solutionPath = EditorPrefs.GetString("BuildAndRun_SolutionPath", "");
                if (string.IsNullOrEmpty(solutionPath))
                {
                    Debug.LogError("Solution path is not set.");
                    return;
                }

                string solutionDirectory = Path.GetDirectoryName(solutionPath);
                RunCommand($"dotnet build \"{solutionPath}\"", solutionDirectory, true, redirectOutput: true);
            }

            if (GUILayout.Button(new GUIContent("Rebuild", "Rebuild Solution"), ToolbarStyles.commandButtonStyle))
            {
                string solutionPath = EditorPrefs.GetString("BuildAndRun_SolutionPath", "");
                if (string.IsNullOrEmpty(solutionPath))
                {
                    Debug.LogError("Solution path is not set.");
                    return;
                }

                string solutionDirectory = Path.GetDirectoryName(solutionPath);
                RunCommand($"dotnet build --no-incremental \"{solutionPath}\"", solutionDirectory, true, redirectOutput: true);
            }

            if (GUILayout.Button(new GUIContent("Run", "Run Project"), ToolbarStyles.commandButtonStyle))
            {
                StartServer();
            }

            if (GUILayout.Button(new GUIContent("Stop", "Stop Project"), ToolbarStyles.commandButtonStyle))
            {
                StopServer();
            }
        }

        private static async void StartServerAndPlay()
        {
            StartServer();

            int delay = EditorPrefs.GetInt("BuildAndRun_ServerStartDelay", 2000);

            // Wait until the server is running
            await Task.Delay(delay); // Use the configurable delay value
            if (IsServerRunning())
            {
                EditorApplication.isPlaying = true;
            }
            else
            {
                Debug.LogError("Failed to start the server.");
            }
        }

        private static void StopServer()
        {
            int processId = EditorPrefs.GetInt(ProcessIdKey, -1);
            if (processId != -1 && IsProcessRunning(processId))
            {
                KillProcessTree(processId);
                EditorPrefs.SetInt(ProcessIdKey, -1);
            }
            else
            {
                Debug.LogWarning("The project is not running.");
            }
        }

        private static void StartServer()
        {
            string projectPath = EditorPrefs.GetString("BuildAndRun_ProjectPath", "");
            if (string.IsNullOrEmpty(projectPath))
            {
                Debug.LogError("Project path is not set.");
                return;
            }

            string projectDirectory = Path.GetDirectoryName(projectPath);
            int processId = EditorPrefs.GetInt(ProcessIdKey, -1);
            if (processId == -1 || !IsProcessRunning(processId))
            {
                Process process = RunCommand($"dotnet run --project \"{projectPath}\"", projectDirectory, false);
                EditorPrefs.SetInt(ProcessIdKey, process.Id);
            }
            else
            {
                Debug.LogWarning("The project is already running.");
            }
        }

        private static bool IsServerRunning()
        {
            int processId = EditorPrefs.GetInt(ProcessIdKey, -1);
            return processId != -1 && IsProcessRunning(processId);
        }

        private static bool IsProcessRunning(int processId)
        {
            try
            {
                Process process = Process.GetProcessById(processId);
                return !process.HasExited;
            }
            catch
            {
                return false;
            }
        }

        private static void KillProcessTree(int processId)
        {
            var startInfo = new ProcessStartInfo("taskkill", $"/T /F /PID {processId}")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (var killProcess = Process.Start(startInfo))
            {
                killProcess.WaitForExit();
            }
        }

        private static Process RunCommand(string command, string workingDirectory, bool isBuild, bool redirectOutput = false)
        {
            var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = redirectOutput,
                RedirectStandardError = redirectOutput,
                UseShellExecute = !redirectOutput,
                CreateNoWindow = redirectOutput
            };

            var process = new Process
            {
                StartInfo = processInfo
            };

            process.OutputDataReceived += (sender, args) =>
            {
                if (args != null)
                {
                    ProcessOutput(args.Data, isBuild, redirectOutput);
                }
            };
            process.ErrorDataReceived += (sender, args) =>
            {
                if (args != null && args.Data != null)
                {
                    Debug.LogError(args.Data);
                }
            };

            process.Start();
            if (redirectOutput)
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            return process;
        }

        private static void ProcessOutput(string data, bool isBuild, bool redirectOutput)
        {
            if (data != null)
            {
                if (redirectOutput)
                {
                    Debug.Log(data);
                }

                if (isBuild)
                {
                    // Check for build failures
                    if (Regex.IsMatch(data, @"\b(failed|fail)\b", RegexOptions.IgnoreCase))
                    {
                        Debug.LogError("Build was unsuccessful.");
                    }
                }
            }
        }
    }
}

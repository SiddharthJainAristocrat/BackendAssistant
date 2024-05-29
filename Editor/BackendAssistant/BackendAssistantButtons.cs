using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
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
                fixedWidth = 40,
                fixedHeight = 20
            };
        }
    }

    [InitializeOnLoad]
    public class BuildAndRunButtons
    {
        static Process process;

        static BuildAndRunButtons()
        {
            ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);
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

            if (GUILayout.Button(new GUIContent("Run", "Run Project"), ToolbarStyles.commandButtonStyle))
            {
                string projectPath = EditorPrefs.GetString("BuildAndRun_ProjectPath", "");
                if (string.IsNullOrEmpty(projectPath))
                {
                    Debug.LogError("Project path is not set.");
                    return;
                }

                string projectDirectory = Path.GetDirectoryName(projectPath);
                if (process == null || process.HasExited)
                {
                    process = RunCommand($"dotnet run --project \"{projectPath}\"", projectDirectory, false);
                }
                else
                {
                    Debug.LogWarning("The project is already running.");
                }
            }

            if (GUILayout.Button(new GUIContent("Stop", "Stop Project"), ToolbarStyles.commandButtonStyle))
            {
                if (process != null && !process.HasExited)
                {
                    KillProcessTree(process.Id);
                    process = null;
                }
                else
                {
                    Debug.LogWarning("The project is not running.");
                }
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
                if (args != null && args.Data!=null)
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
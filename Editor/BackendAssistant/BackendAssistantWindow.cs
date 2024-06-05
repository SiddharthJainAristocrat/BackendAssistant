using UnityEditor;
using UnityEngine;

public class BackendAssistantWindow : EditorWindow
{
    private string solutionPath;
    private string projectPath;
    private bool startServerOnPlay;
    private bool stopServerOnStop;
    private int serverStartDelay;

    [MenuItem("Tools/Backend Assistant Settings")]
    public static void ShowWindow()
    {
        GetWindow<BackendAssistantWindow>("Build and Run Settings");
    }

    private void OnEnable()
    {
        solutionPath = EditorPrefs.GetString("BuildAndRun_SolutionPath", "");
        projectPath = EditorPrefs.GetString("BuildAndRun_ProjectPath", "");
        startServerOnPlay = EditorPrefs.GetBool("BuildAndRun_StartServerOnPlay", false);
        stopServerOnStop = EditorPrefs.GetBool("BuildAndRun_StopServerOnStop", false);
        serverStartDelay = EditorPrefs.GetInt("BuildAndRun_ServerStartDelay", 2000);
    }

    private void OnGUI()
    {
        GUILayout.Label("Build and Run Settings", EditorStyles.boldLabel);

        solutionPath = EditorGUILayout.TextField("Solution Path", solutionPath);
        projectPath = EditorGUILayout.TextField("Project Path", projectPath);
        startServerOnPlay = EditorGUILayout.Toggle("Start Server on Play", startServerOnPlay);
        stopServerOnStop = EditorGUILayout.Toggle("Stop Server on Stop", stopServerOnStop);
        serverStartDelay = EditorGUILayout.IntField("Server Start Delay (ms)", serverStartDelay);

        if (GUILayout.Button("Save"))
        {
            EditorPrefs.SetString("BuildAndRun_SolutionPath", solutionPath);
            EditorPrefs.SetString("BuildAndRun_ProjectPath", projectPath);
            EditorPrefs.SetBool("BuildAndRun_StartServerOnPlay", startServerOnPlay);
            EditorPrefs.SetBool("BuildAndRun_StopServerOnStop", stopServerOnStop);
            EditorPrefs.SetInt("BuildAndRun_ServerStartDelay", serverStartDelay);
            Debug.Log("Paths and settings saved.");
        }
    }
}
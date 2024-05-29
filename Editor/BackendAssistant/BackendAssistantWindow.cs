using UnityEditor;
using UnityEngine;

public class BackendAssistantWindow : EditorWindow
{
    private string solutionPath;
    private string projectPath;

    [MenuItem("Tools/Backend Assistant Settings")]
    public static void ShowWindow()
    {
        GetWindow<BackendAssistantWindow>("Build and Run Settings");
    }

    private void OnEnable()
    {
        solutionPath = EditorPrefs.GetString("BuildAndRun_SolutionPath", "");
        projectPath = EditorPrefs.GetString("BuildAndRun_ProjectPath", "");
    }

    private void OnGUI()
    {
        GUILayout.Label("Build and Run Settings", EditorStyles.boldLabel);

        solutionPath = EditorGUILayout.TextField("Solution Path", solutionPath);
        projectPath = EditorGUILayout.TextField("Project Path", projectPath);

        if (GUILayout.Button("Save"))
        {
            EditorPrefs.SetString("BuildAndRun_SolutionPath", solutionPath);
            EditorPrefs.SetString("BuildAndRun_ProjectPath", projectPath);
            Debug.Log("Paths saved.");
        }
    }
}
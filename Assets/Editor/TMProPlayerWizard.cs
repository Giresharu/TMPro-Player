using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

public class TMProPlayerWizard : EditorWindow {

    [MenuItem("Window/TMPro Player/Wizard")]
    static void OpenWindow() {
        EditorWindow window = GetWindow<TMProPlayerWizard>();
        window.titleContent = new GUIContent("TMPro Player Wizard");
        window.Show();
    }

    string packagePath = "Packages/com.gsr.tmproplayer/Example/Examples.unitypackage";
    void OnGUI() {
        EditorGUILayout.Space();
        GUILayout.BeginHorizontal();
        {
            GUILayout.BeginVertical();
            {
                GUILayout.Label("安装范例", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter,fontSize = 14});
                GUILayout.Label("Install Examples", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
                if (GUILayout.Button("Install")) Install();
            }
            GUILayout.EndVertical();
        }
        GUILayout.EndHorizontal();
        
    }

    void Install() {
        AssetDatabase.ImportPackage(packagePath, true);
    }

}

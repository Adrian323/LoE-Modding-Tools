using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;


public class EditorSteamWorkshopManagerStarter : EditorWindow
{

    public static void StartWorkshopManager()
    {
        // Get existing open window or if none, make a new one:
        EditorSteamWorkshopManagerStarter window = (EditorSteamWorkshopManagerStarter)EditorWindow.GetWindow(typeof(EditorSteamWorkshopManagerStarter));
        window.position = new Rect(Vector2.zero, new Vector2(400, 400));
        window.Show();
    }

    void OnGUI()
    {

        UMod.ModTools.Export.ExportSettings exportSettings = UMod.ModTools.Export.ExportSettings.Active;

        
        Debug.Log("Test");
        return;
        GUILayout.Label("Loading Workshop Manager...", EditorStyles.boldLabel);

        // if workshop scene is closed, open it
        try
        {
            Scene desiredScene = SceneManager.GetSceneByBuildIndex(0);

            if (desiredScene == null)
            {
                throw new System.Exception("No scene was found on index 0");
            }

            if (SceneManager.GetActiveScene() != desiredScene)
            {
                if (Application.isPlaying == true)
                {
                    Application.Quit();
                    return;
                }

                EditorSceneManager.OpenScene(UnityEditor.EditorBuildSettings.scenes[0].path);
            }
        }
        catch (System.Exception ex)
        {
            GUI.color = Color.red;
            EditorGUILayout.LabelField("Could not load scene: " + ex.Message);
            return;
        }

        if (Application.isPlaying == false)
        {
            EditorApplication.ExecuteMenuItem("Edit/Play");
        }

        this.Close();
    }
}

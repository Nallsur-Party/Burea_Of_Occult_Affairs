using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NPCArchiveService))]
public class NPCArchiveServiceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        NPCArchiveService archiveService = (NPCArchiveService)target;

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Archive Debug", EditorStyles.boldLabel);

        if (GUILayout.Button("Print Archived NPCs"))
        {
            archiveService.PrintArchivedNpcs();
        }

        if (GUILayout.Button("Clear Archived NPCs"))
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Clear Archived NPCs",
                "This will remove all NPCs from the archive and overwrite the save file. Continue?",
                "Clear",
                "Cancel");

            if (confirm)
            {
                archiveService.ClearArchivedNpcs();
                EditorUtility.SetDirty(archiveService);
            }
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Archive File", archiveService.ArchiveFilePath);
    }
}

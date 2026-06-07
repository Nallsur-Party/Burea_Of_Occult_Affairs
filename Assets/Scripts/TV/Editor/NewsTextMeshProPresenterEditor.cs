using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NewsTextMeshProPresenter))]
public class NewsTextMeshProPresenterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        NewsTextMeshProPresenter presenter = (NewsTextMeshProPresenter)target;

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);

        if (GUILayout.Button("Refresh News Text"))
        {
            presenter.Refresh();
        }

        if (GUILayout.Button("Pin Latest Archived NPC To TV"))
        {
            Undo.RecordObject(presenter, "Pin Latest Archived NPC To TV");

            string reason;
            if (presenter.PinLatestArchivedNpcToTv(out reason))
            {
                EditorUtility.SetDirty(presenter);
            }
            else if (!string.IsNullOrWhiteSpace(reason))
            {
                Debug.LogWarning(reason, presenter);
            }
        }
    }
}

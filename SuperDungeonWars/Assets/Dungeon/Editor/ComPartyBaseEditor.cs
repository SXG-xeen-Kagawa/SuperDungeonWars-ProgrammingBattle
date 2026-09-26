using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ComPartyBase), true)]
[CanEditMultipleObjects]
public class ComPartyBaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;

        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (property.name == "m_creatorDisplayName")
            {
                EditorGUILayout.HelpBox(
                    "挑戦者名・所属・パーティー名など、観戦時に紹介する情報を入力してください。所属は任意です。",
                    MessageType.Info);
            }

            GUIContent label = GetJapaneseLabel(property);

            using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script"))
            {
                EditorGUILayout.PropertyField(property, label, true);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static GUIContent GetJapaneseLabel(SerializedProperty property)
    {
        string label;

        switch (property.name)
        {
            case "m_memberPrefabs":
                label = "メンバー用プレハブ";
                break;
            case "m_creatorDisplayName":
                label = "制作者名";
                break;
            case "m_affiliation":
                label = "所属（学校・団体名）";
                break;
            case "m_teamDisplayName":
                label = "パーティー名";
                break;
            case "m_teamSimpleDescription":
                label = "パーティーの紹介";
                break;
            case "m_teamIconSprite":
                label = "パーティーアイコン";
                break;
            default:
                label = property.displayName;
                break;
        }

        return new GUIContent(label, property.tooltip);
    }
}
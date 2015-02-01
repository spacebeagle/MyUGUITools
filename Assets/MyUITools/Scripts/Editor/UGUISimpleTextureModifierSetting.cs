using UnityEngine;
using UnityEditor;
using System.Collections;

public class UGUISimpleTextureModifierSetting {
	[PreferenceItem("TextureModifier")]
    public static void ShowPreference() {
        bool bValue;
		bValue = EditorPrefs.GetBool(UGUISimpleTextureModifier.KEY, false);
		bValue = EditorGUILayout.Toggle(UGUISimpleTextureModifier.KEY, bValue);

        if (GUI.changed)
			EditorPrefs.SetBool(UGUISimpleTextureModifier.KEY, bValue);
    }
}

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;


namespace ParadoxNotion.Design{

	///A generic popup editor for all reference types
	public class GenericInspectorWindow : EditorWindow{

		private object targetObject;
		private System.Type targetType;
		private Vector2 scrollPos;

		void OnEnable(){
			title = "Object Editor";
			GUI.skin.label.richText = true;
		}

		void OnGUI(){

			if (EditorApplication.isCompiling || targetType == null){
				Close();
				return;
			}

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Label(string.Format("<size=14><b>{0}</b></size>", targetType.FriendlyName()) );
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			GUILayout.Space(10);
			scrollPos = GUILayout.BeginScrollView(scrollPos);
			targetObject = EditorUtils.GenericField(targetType.FriendlyName(), targetObject, targetType, null);
			GUILayout.EndScrollView();
			Repaint();
		}

		public static void Show(object o, System.Type t){

			var window = CreateInstance<GenericInspectorWindow>();
			window.targetObject = o;
			window.targetType = t;
			window.ShowUtility();
		}
	}
}

#endif
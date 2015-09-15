#if UNITY_EDITOR

using System.Collections.Generic;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEditor;
using UnityEngine;


namespace NodeCanvas.Editor{

	public class PreferedTypesEditorWindow : EditorWindow {

		private List<System.Type> typeList;
		private Vector2 scrollPos;

		void OnEnable(){
	        #if UNITY_5_1
	        titleContent = new GUIContent("Preferred Types");
	        #else
	        title = "Preferred Types";
	        #endif

			typeList = UserTypePrefs.GetPreferedTypesList(typeof(object), true);
		}

		void OnGUI(){
			
			EditorGUILayout.HelpBox("Here you can specify frequently used types for your game for easier access wherever you need to select a type.\nFor example when you create a new blackboard variable or using any refelection based actions.", MessageType.Info);

			if (GUILayout.Button("Add New Type")){
				GenericMenu.MenuFunction2 Selected = delegate(object t){
					if (!typeList.Contains( (System.Type)t)){
						typeList.Add( (System.Type)t);
						Save();
					} else {
						ShowNotification(new GUIContent("Type already in list") );
					}
				};	

				var menu = new UnityEditor.GenericMenu();
				menu.AddItem(new GUIContent("Classes/System/Object"), false, Selected, typeof(object));
				foreach(var t in EditorUtils.GetAssemblyTypes(typeof(object))){
					var friendlyName = (string.IsNullOrEmpty(t.Namespace)? "No Namespace/" : t.Namespace.Replace(".", "/") + "/") + t.FriendlyName();
					var category = "Classes/";
					if (t.IsInterface) category = "Interfaces/";
					if (t.IsEnum) category = "Enumerations/";
					menu.AddItem(new GUIContent( category + friendlyName), false, Selected, t);
				}
				menu.ShowAsContext();
				Event.current.Use();
			}

			if (GUILayout.Button("RESET DEFAULTS")){
				UserTypePrefs.ResetTypeConfiguration();
				typeList = UserTypePrefs.GetPreferedTypesList(typeof(object), true);
				Save();
			}

			scrollPos = GUILayout.BeginScrollView(scrollPos);

			for (int i = 0; i < typeList.Count; i++){
				GUILayout.BeginHorizontal("box");
				EditorGUILayout.LabelField(typeList[i].Name, typeList[i].Namespace);
				if (GUILayout.Button("X", GUILayout.Width(18))){
					typeList.RemoveAt(i);
					Save();
				}
				GUILayout.EndHorizontal();
			}

			GUILayout.EndScrollView();

			Repaint();
		}

		void Save(){
			UserTypePrefs.SetPreferedTypesList(typeList);
			ShowNotification(new GUIContent("Configuration Saved!"));
		}


		[MenuItem("Window/NodeCanvas/Preferred Types Editor")]
		public static void ShowWindow(){
			var window = GetWindow<PreferedTypesEditorWindow>();
			window.Show();
		}
	}
}

#endif
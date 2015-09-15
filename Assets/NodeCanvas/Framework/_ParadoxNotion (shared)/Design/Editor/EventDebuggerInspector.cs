#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;


namespace ParadoxNotion.Design{

	[CustomEditor(typeof(EventHandlerDebugComponent))]
	public class EventHandlerDebuggerInspector : UnityEditor.Editor{

		private EventHandlerDebugComponent debugger{
			get {return target as EventHandlerDebugComponent;}
		}

		public int totalMembers;

		override public void OnInspectorGUI(){

			debugger.logEvents = EditorGUILayout.Toggle("Log Events?", debugger.logEvents);

			GUI.color = Color.yellow;
			EditorGUILayout.LabelField("Total Events: " + debugger.subscribedMembers.Count);
			EditorGUILayout.LabelField("Total Members: " + totalMembers);
			GUI.color = Color.white;

			totalMembers = 0;

			foreach (var subscribedMember in debugger.subscribedMembers){

				if (subscribedMember.Value.Count == 0)
					continue;

				totalMembers += subscribedMember.Value.Count;

				GUILayout.BeginVertical("box");

				GUI.color = Color.yellow;
				EditorGUILayout.LabelField(subscribedMember.Key);
				GUI.color = Color.white;

				foreach (var member in subscribedMember.Value){

					GUILayout.BeginVertical("textfield");
					
					if (member.subscribedObject != null)
						EditorGUILayout.LabelField("Member", member.subscribedObject.ToString());
					
					if (member.subscribedFunction != null)
						EditorGUILayout.LabelField("Function", member.subscribedFunction.ToString());
					
					EditorGUILayout.LabelField("Invoke Priority", member.invokePriority.ToString());
					EditorGUILayout.LabelField("Unsubscribe After Receive", member.unsubscribeWhenReceive.ToString());
					GUILayout.EndVertical();
				}

				GUILayout.EndVertical();
			}

			if (GUI.changed)
				EditorUtility.SetDirty(debugger);
		}
	}
}

#endif
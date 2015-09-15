#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NodeCanvas.Framework;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace ParadoxNotion.Design{

    /// <summary>
    /// BBParameter editor field
    /// </summary>

	partial class EditorUtils {

		//a special object field for the BBParameter class to let user choose either a real value or enter a string to read data from a Blackboard
		public static BBParameter BBParameterField(string prefix, BBParameter bbParam, bool blackboardOnly = false, MemberInfo member = null){

			if (bbParam == null){
				EditorGUILayout.LabelField(prefix, "Non Set Variable");
				return null;
			}

			GUILayout.BeginVertical();
			GUILayout.BeginHorizontal();

			//override if we have a memeber info
			if (member != null){
				blackboardOnly = member.RTGetAttribute<BlackboardOnlyAttribute>(false) != null;
			}

			//Direct assignement
			if (!blackboardOnly && !bbParam.useBlackboard){

				bbParam.value = GenericField(prefix, bbParam.value, bbParam.varType, member);
			
			//Dropdown variable selection
			} else {

				GUI.color = new Color(0.9f,0.9f,1f,1f);
				var varNames = new List<string>();
				
				//Local
				if (bbParam.bb != null)
					varNames.AddRange(bbParam.bb.GetVariableNames(bbParam.varType));


				//Globals
				foreach (var globalBB in GlobalBlackboard.allGlobals.Where(globalBB => globalBB != bbParam.bb)) {
				    varNames.Add(globalBB.name + "/");
				    var globalVars = globalBB.GetVariableNames(bbParam.varType);
				    for (var i = 0; i < globalVars.Length; i++)
				        globalVars[i] = globalBB.name + "/" + globalVars[i];
				    varNames.AddRange( globalVars );
				}


				//Dynamic
				varNames.Add("(DynamicVar)");
				
				var isDynamic = !string.IsNullOrEmpty(bbParam.name) && !varNames.Contains(bbParam.name);
				if (!isDynamic){

					bbParam.name = StringPopup(prefix, bbParam.name, varNames, false, true);
					if (bbParam.name == "(DynamicVar)"){
						bbParam.name = "_";
					}
				
				} else {
					
					bbParam.name = EditorGUILayout.TextField(prefix + " (" + bbParam.varType.FriendlyName() + ")", bbParam.name);
				}
			}


			GUI.color = Color.white;
			GUI.backgroundColor = Color.white;

			if (!blackboardOnly)
				bbParam.useBlackboard = EditorGUILayout.Toggle(bbParam.useBlackboard, EditorStyles.radioButton, GUILayout.Width(18));

			GUILayout.EndHorizontal();
		
			if (bbParam.useBlackboard && string.IsNullOrEmpty(bbParam.name)){	
				
				GUI.backgroundColor = new Color(0.8f,0.8f,1f,0.5f);
				GUI.color = new Color(1f,1f,1f,0.5f);
				GUILayout.BeginVertical("textfield");
				
				GUILayout.BeginHorizontal();

				if ( bbParam.bb != null && bbParam.varType != typeof(object) ){
					if (GUILayout.Button("<b>+</b>", (GUIStyle)"label", GUILayout.Width(20) )){
						if (bbParam.bb.AddVariable(prefix, bbParam.varType) != null)
							bbParam.name = prefix;
					}
					EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.ArrowPlus);
				}

				if (bbParam.bb != null){
					GUILayout.Label("Select a '" + bbParam.varType.FriendlyName() + "' Blackboard Variable");
				} else {
					GUILayout.Label("<i>No current Blackboard reference</i>");
				}

				GUILayout.EndHorizontal();
				GUILayout.EndVertical();
				GUILayout.Space(2);
			}

			GUILayout.EndVertical();
			GUI.backgroundColor = Color.white;
			GUI.color = Color.white;			
			return bbParam;
		}


	}
}

#endif
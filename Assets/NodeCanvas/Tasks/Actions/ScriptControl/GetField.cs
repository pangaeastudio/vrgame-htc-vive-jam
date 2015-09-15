using System.Reflection;
using NodeCanvas.Framework;
using NodeCanvas.Framework.Internal;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;


namespace NodeCanvas.Tasks.Actions{

	[Category("✫ Script Control/Common")]
	[Description("Get a variable of a script and save it to the blackboard")]
	public class GetField : ActionTask {

		[SerializeField] [BlackboardOnly]
		private BBObjectParameter saveAs;
		[SerializeField]
		private System.Type targetType;
		[SerializeField]
		private string fieldName;

		private FieldInfo field;

		public override System.Type agentType{
			get {return targetType?? typeof(Transform);}
		}

		protected override string info{
			get
			{
				if (string.IsNullOrEmpty(fieldName))
					return "No Field Selected";
				return string.Format("{0} = {1}.{2}", saveAs.ToString(), agentInfo, fieldName);
			}
		}

		protected override string OnInit(){
			field = agentType.RTGetField(fieldName);
			if (field == null)
				return "Missing Field";
			return null;
		}

		protected override void OnExecute(){
			saveAs.value = field.GetValue(agent);
			EndAction();
		}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		protected override void OnTaskInspectorGUI(){

			if (!Application.isPlaying && GUILayout.Button("Select Field")){
				
				System.Action<FieldInfo> FieldSelected = (field)=> {
					targetType = field.DeclaringType;
					fieldName = field.Name;
					saveAs.SetType(field.FieldType);
				};

				if (agent != null){
					EditorUtils.ShowGameObjectFieldSelectionMenu(agent.gameObject, typeof(object), FieldSelected);
				} else {
					var menu = new UnityEditor.GenericMenu();
					foreach (var t in UserTypePrefs.GetPreferedTypesList(typeof(Component), true))
						menu = EditorUtils.GetFieldSelectionMenu(t, typeof(object), FieldSelected, menu);
					menu.ShowAsContext();
					Event.current.Use();
				}
			}


			if (agentType != null && !string.IsNullOrEmpty(fieldName)){
				GUILayout.BeginVertical("box");
				UnityEditor.EditorGUILayout.LabelField("Type", agentType.Name);
				UnityEditor.EditorGUILayout.LabelField("Field", fieldName);
				UnityEditor.EditorGUILayout.LabelField("Field Type", saveAs.varType.FriendlyName() );
				GUILayout.EndVertical();
				EditorUtils.BBParameterField("Save As", saveAs, true);
			}
		}

		#endif
	}
}

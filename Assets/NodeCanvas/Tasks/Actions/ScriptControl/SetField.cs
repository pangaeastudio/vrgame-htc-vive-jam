using System.Reflection;
using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;


namespace NodeCanvas.Tasks.Actions{

	[Category("✫ Script Control/Common")]
	[Description("Set a variable on a script")]
	public class SetField : ActionTask {

		[SerializeField]
		private BBParameter setValue;
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

				return string.Format("{0}.{1} = {2}", agentInfo, fieldName, setValue);
			}
		}

		protected override string OnInit(){
			field = agentType.RTGetField(fieldName);
			if (field == null)
				return "Missing Field Info";
			return null;
		}

		protected override void OnExecute(){
			field.SetValue(agent, setValue.value);
			EndAction();
		}


		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		protected override void OnTaskInspectorGUI(){

			if (!Application.isPlaying && GUILayout.Button("Select Field")){

				System.Action<FieldInfo> FieldSelected = (field)=>{
					targetType = field.DeclaringType;
					fieldName = field.Name;
					setValue = BBParameter.CreateInstance(field.FieldType, blackboard);
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
				UnityEditor.EditorGUILayout.LabelField("Field Type", setValue.varType.FriendlyName() );
				GUILayout.EndVertical();
				EditorUtils.BBParameterField("Set Value", setValue);
			}
		}

		#endif
	}
}
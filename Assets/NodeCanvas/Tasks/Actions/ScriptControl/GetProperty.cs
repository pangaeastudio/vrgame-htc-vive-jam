using System.Reflection;
using NodeCanvas.Framework;
using NodeCanvas.Framework.Internal;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;


namespace NodeCanvas.Tasks.Actions{

	[Category("✫ Script Control/Standalone Only")]
	[Description("Get a property of a script and save it to the blackboard")]
	public class GetProperty : ActionTask {

		[SerializeField] [IncludeParseVariables]
		private ReflectedFunctionWrapper functionWrapper;

		private MethodInfo targetMethod{
			get {return functionWrapper != null && functionWrapper.GetMethod() != null? functionWrapper.GetMethod() : null; }
		}

		public override System.Type agentType{
			get {return targetMethod != null? targetMethod.RTReflectedType() : typeof(Transform);}
		}

		protected override string info{
			get
			{
				if (functionWrapper == null)
					return "No Property Selected";
				if (targetMethod == null)
					return string.Format("<color=#ff6457>* {0} *</color>", functionWrapper.GetMethodString() );
				return string.Format("{0} = {1}.{2}", functionWrapper.GetVariables()[0], agentInfo, targetMethod.Name);
			}
		}

		//store the method info on init for performance
		protected override string OnInit(){

			if (targetMethod == null)
				return "GetProperty Error";

			try
			{
				functionWrapper.Init(agent);
				return null;
			}
			catch {return "GetProperty Error";}
		}

		//do it by invoking method
		protected override void OnExecute(){

			if (functionWrapper == null){
				EndAction(false);
				return;
			}

			functionWrapper.Call();
			EndAction();
		}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		protected override void OnTaskInspectorGUI(){

			if (!Application.isPlaying && GUILayout.Button("Select Property")){
				System.Action<MethodInfo> MethodSelected = (method)=>{
					functionWrapper = ReflectedFunctionWrapper.Create(method, blackboard);
				};

				if (agent != null){
					EditorUtils.ShowGameObjectMethodSelectionMenu(agent.gameObject, typeof(object), typeof(object), MethodSelected, 0, true, true);
				} else {
					var menu = new UnityEditor.GenericMenu();
					foreach (var t in UserTypePrefs.GetPreferedTypesList(typeof(Component), true))
						menu = EditorUtils.GetMethodSelectionMenu(t, typeof(object), typeof(object), MethodSelected, 0, true, true, menu);
					menu.ShowAsContext();
					Event.current.Use();
				}
			}


			if (targetMethod != null){
				GUILayout.BeginVertical("box");
				UnityEditor.EditorGUILayout.LabelField("Type", agentType.FriendlyName());
				UnityEditor.EditorGUILayout.LabelField("Property", targetMethod.Name);
				UnityEditor.EditorGUILayout.LabelField("Property Type", targetMethod.ReturnType.FriendlyName() );
				GUILayout.EndVertical();
				EditorUtils.BBParameterField("Save As", functionWrapper.GetVariables()[0], true);
			}
		}

		#endif
	}
}

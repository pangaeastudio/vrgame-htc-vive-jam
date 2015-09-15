using System.Reflection;
using NodeCanvas.Framework;
using NodeCanvas.Framework.Internal;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;


namespace NodeCanvas.Tasks.Actions{

	[Category("✫ Script Control/Standalone Only")]
	[Description("Calls a function that has signature of 'public Status NAME()' or 'public Status NAME(T)'. You should return Status.Success, Failure or Running within that function.")]
	public class ImplementedAction : ActionTask {

		[SerializeField] [IncludeParseVariables]
		private ReflectedFunctionWrapper functionWrapper;

		private Status actionStatus = Status.Resting;

		private MethodInfo targetMethod{
			get {return functionWrapper != null && functionWrapper.GetMethod() != null? functionWrapper.GetMethod() : null;}
		}

		public override System.Type agentType{
			get {return targetMethod != null? targetMethod.RTReflectedType() : typeof(Transform);}
		}

		protected override string info{
			get
			{
				if (functionWrapper == null)
					return "No Action Selected";
				if (targetMethod == null)
					return string.Format("<color=#ff6457>* {0} *</color>", functionWrapper.GetMethodString() );
				return string.Format("[ {0}.{1}({2}) ]", agentInfo, targetMethod.Name, functionWrapper.GetVariables().Length == 2? functionWrapper.GetVariables()[1].ToString() : "" );
			}
		}


		protected override string OnInit(){

			if (targetMethod == null)
				return "ImplementedAction Error";

			try
			{
				functionWrapper.Init(agent);
				return null;
			}
			catch {return "ImplementedAction Error";}
		}

		protected override void OnExecute(){ Forward(); }
		protected override void OnUpdate(){	Forward(); }

		void Forward(){

			if (functionWrapper == null){
				EndAction(false);
				return;
			}

			actionStatus = (Status)functionWrapper.Call();

			if (actionStatus == Status.Success){
				EndAction(true);
				return;
			}

			if (actionStatus == Status.Failure){
				EndAction(false);
				return;
			}
		}

		protected override void OnStop(){
			actionStatus = Status.Resting;
		}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		protected override void OnTaskInspectorGUI(){

			if (!Application.isPlaying && GUILayout.Button("Select Action Method")){

				System.Action<MethodInfo> MethodSelected = (method)=>{
					functionWrapper = ReflectedFunctionWrapper.Create(method, blackboard);
				};

				if (agent != null){
					EditorUtils.ShowGameObjectMethodSelectionMenu(agent.gameObject, typeof(Status), typeof(object), MethodSelected, 1, false, true);
				} else {
					var menu = new UnityEditor.GenericMenu();
					foreach (var t in UserTypePrefs.GetPreferedTypesList(typeof(Component), true))
						menu = EditorUtils.GetMethodSelectionMenu(t, typeof(Status), typeof(object), MethodSelected, 1, false, true, menu);
					menu.ShowAsContext();
					Event.current.Use();
				}
			}

			if (targetMethod != null){
				GUILayout.BeginVertical("box");
				UnityEditor.EditorGUILayout.LabelField("Type", agentType.FriendlyName());
				UnityEditor.EditorGUILayout.LabelField("Selected Action Method:", targetMethod.Name);
				GUILayout.EndVertical();
				
				if (targetMethod.GetParameters().Length == 1){
					var paramName = targetMethod.GetParameters()[0].Name.SplitCamelCase();
					EditorUtils.BBParameterField(paramName, functionWrapper.GetVariables()[1]);
				}
			}
		}
		
		#endif
	}
}
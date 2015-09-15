using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NodeCanvas.Framework;
using NodeCanvas.Framework.Internal;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;


namespace NodeCanvas.Tasks.Actions{

	[Category("✫ Script Control/Standalone Only")]
	[Description("Execute a function on a script, of up to 3 parameters and save the return if any. If function is an IEnumerator it will execute as a coroutine.")]
	public class ExecuteFunction : ActionTask {

		[SerializeField] [IncludeParseVariables]
		private ReflectedWrapper functionWrapper;

		private bool routineRunning;

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
					return "No Method Selected";
				if (targetMethod == null)
					return string.Format("<color=#ff6457>* {0} *</color>", functionWrapper.GetMethodString() );

				var variables = functionWrapper.GetVariables();
				var returnInfo = "";
				var paramInfo = "";
				if (targetMethod.ReturnType == typeof(void)){
					for (var i = 0; i < variables.Length; i++)
						paramInfo += (i != 0? ", " : "") + variables[i].ToString();
				} else {
					returnInfo = targetMethod.ReturnType == typeof(void) || targetMethod.ReturnType == typeof(IEnumerator) || variables[0].isNone? "" : variables[0] + " = ";
					for (var i = 1; i < variables.Length; i++)
						paramInfo += (i != 1? ", " : "") + variables[i].ToString();
				}

				return string.Format("{0}{1}.{2}({3})", returnInfo, agentInfo, targetMethod.Name, paramInfo );
			}
		}

		//store the method info on init
		protected override string OnInit(){

			if (targetMethod == null)
				return "ExecuteFunction Error";

			try
			{
				functionWrapper.Init(agent);
				return null;
			}
			catch {return "ExecuteFunction Error";}
		}

		//do it by calling delegate or invoking method
		protected override void OnExecute(){

			if (targetMethod == null){
				EndAction(false);
				return;
			}

			try
			{
				if (targetMethod.ReturnType == typeof(IEnumerator)){
					StartCoroutine( InternalCoroutine( (IEnumerator)((ReflectedFunctionWrapper)functionWrapper).Call() ));
					return;
				}

				if (targetMethod.ReturnType == typeof(void)){
					((ReflectedActionWrapper)functionWrapper).Call();
				} else {
					((ReflectedFunctionWrapper)functionWrapper).Call();
				}
				
				EndAction(true);
			}

			catch (System.Exception e)
			{
				Debug.LogError(string.Format("{0}\n{1}", e.Message, e.StackTrace));
				EndAction(false);
			}
		}

		protected override void OnStop(){
			routineRunning = false;
		}

		IEnumerator InternalCoroutine(IEnumerator routine){
			routineRunning = true;
			while(routineRunning && routine.MoveNext()){
				if (routineRunning == false)
					yield break;
				yield return routine.Current;
			}

			EndAction();
		}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR
		
		protected override void OnTaskInspectorGUI(){

			if (!Application.isPlaying && GUILayout.Button("Select Method")){

				System.Action<MethodInfo> MethodSelected = (method) => {
					functionWrapper = ReflectedWrapper.Create(method, blackboard);
				};
				
				if (agent != null){
					
					EditorUtils.ShowGameObjectMethodSelectionMenu(agent.gameObject, typeof(object), typeof(object), MethodSelected, 3, false, false);

				} else {
					var menu = new UnityEditor.GenericMenu();
					foreach (var t in UserTypePrefs.GetPreferedTypesList(typeof(Component), true))
						menu = EditorUtils.GetMethodSelectionMenu(t, typeof(object), typeof(object), MethodSelected, 3, false, false, menu);
					menu.ShowAsContext();
					Event.current.Use();
				}
			}


			if (targetMethod != null){

				GUILayout.BeginVertical("box");
				UnityEditor.EditorGUILayout.LabelField("Type", agentType.FriendlyName());
				UnityEditor.EditorGUILayout.LabelField("Method", targetMethod.Name);
				UnityEditor.EditorGUILayout.LabelField("Returns", targetMethod.ReturnType.FriendlyName());

				if (targetMethod.ReturnType == typeof(IEnumerator))
					GUILayout.Label("<b>This will execute as a Coroutine</b>");

				GUILayout.EndVertical();

				var paramNames = targetMethod.GetParameters().Select(p => p.Name.SplitCamelCase() ).ToArray();
				var variables = functionWrapper.GetVariables();
				if (targetMethod.ReturnType == typeof(void)){
					for (var i = 0; i < paramNames.Length; i++){
						EditorUtils.BBParameterField(paramNames[i], variables[i]);
					}
				} else {
					for (var i = 0; i < paramNames.Length; i++){
						EditorUtils.BBParameterField(paramNames[i], variables[i+1]);
					}
					
					if (targetMethod.ReturnType != typeof(IEnumerator)){
						EditorUtils.BBParameterField("Save Return Value", variables[0], true);
					}
				}
			}
		}

		#endif
	}
}
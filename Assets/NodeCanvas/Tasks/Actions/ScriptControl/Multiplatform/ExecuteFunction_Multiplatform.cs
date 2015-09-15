using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NodeCanvas.Framework;
using NodeCanvas.Framework.Internal;
using ParadoxNotion;
using ParadoxNotion.Design;
using ParadoxNotion.Serialization;
using UnityEngine;


namespace NodeCanvas.Tasks.Actions{

	[Name("Execute Function (mp)")]
	[Category("✫ Script Control/Multiplatform")]
	[Description("Execute a function on a script, of up to 3 parameters and save the return if any. If function is an IEnumerator it will execute as a coroutine.")]
	public class ExecuteFunction_Multiplatform : ActionTask {

		[SerializeField]
		private SerializedMethodInfo method;
		[SerializeField]
		private List<BBObjectParameter> parameters = new List<BBObjectParameter>();
		[SerializeField] [BlackboardOnly]
		private BBObjectParameter returnValue;

		private bool routineRunning;

		private MethodInfo targetMethod{
			get {return method != null && method.Get() != null? method.Get() : null;}
		}

		public override System.Type agentType{
			get {return targetMethod != null? targetMethod.RTReflectedType() : typeof(Transform);}
		}

		protected override string info{
			get
			{
				if (method == null)
					return "No Method Selected";
				if (targetMethod == null)
					return string.Format("<color=#ff6457>* {0} *</color>", method.GetMethodString() );

				var returnInfo = targetMethod.ReturnType == typeof(void) || targetMethod.ReturnType == typeof(IEnumerator)? "" : returnValue.ToString() + " = ";
				var paramInfo = "";
				for (var i = 0; i < parameters.Count; i++)
					paramInfo += (i != 0? ", " : "") + parameters[i].ToString();
				return string.Format("{0}{1}.{2}({3})", returnInfo, agentInfo, targetMethod.Name, paramInfo );
			}
		}

		//store the method info on init
		protected override string OnInit(){
			if (targetMethod == null)
				return "ExecuteFunction Error";
			return null;
		}

		//do it by calling delegate or invoking method
		protected override void OnExecute(){

			var args = parameters.Select(p => p.value).ToArray();

			if (targetMethod.ReturnType == typeof(IEnumerator)){
				StartCoroutine( InternalCoroutine( (IEnumerator)targetMethod.Invoke(agent, args) ));
				return;
			}

			returnValue.value = targetMethod.Invoke(agent, args);
			EndAction();
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
					this.method = new SerializedMethodInfo(method);
					this.parameters.Clear();
					foreach(var p in method.GetParameters()){
						var newParam = new BBObjectParameter{bb = blackboard};
						newParam.SetType(p.ParameterType);
						if (p.IsOptional){
							newParam.value = p.DefaultValue;
						}
						parameters.Add(newParam);
					}

					if (method.ReturnType != typeof(void) && targetMethod.ReturnType != typeof(IEnumerator)){
						this.returnValue = new BBObjectParameter{bb = blackboard};
						this.returnValue.SetType(method.ReturnType);
					}
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
				for (var i = 0; i < paramNames.Length; i++){
					EditorUtils.BBParameterField(paramNames[i], parameters[i]);
				}

				if (targetMethod.ReturnType != typeof(void) && targetMethod.ReturnType != typeof(IEnumerator)){
					EditorUtils.BBParameterField("Save Return Value", returnValue, true);
				}
			}
		}

		#endif
	}
}
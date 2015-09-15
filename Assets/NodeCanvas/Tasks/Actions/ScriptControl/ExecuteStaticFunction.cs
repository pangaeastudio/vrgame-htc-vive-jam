using System.Collections;
using System.Linq;
using System.Reflection;
using NodeCanvas.Framework;
using NodeCanvas.Framework.Internal;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;


namespace NodeCanvas.Tasks.Actions{

	[Category("✫ Script Control/Standalone Only")]
	[Description("Execute a static function of up to 3 parameters and optionaly save the return value")]
	public class ExecuteStaticFunction : ActionTask {

		[SerializeField] [IncludeParseVariables]
		private ReflectedWrapper functionWrapper;

		private MethodInfo targetMethod{
			get {return functionWrapper != null && functionWrapper.GetMethod() != null? functionWrapper.GetMethod() : null;}
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
					returnInfo = variables[0].isNone? "" : variables[0] + " = ";
					for (var i = 1; i < variables.Length; i++)
						paramInfo += (i != 1? ", " : "") + variables[i].ToString();
				}

				return string.Format("{0}{1}.{2} ({3})", returnInfo, targetMethod.DeclaringType.FriendlyName(), targetMethod.Name, paramInfo );
			}
		}

		//store the method info on init
		protected override string OnInit(){

			if (targetMethod == null)
				return "ExecuteFunction Error";

			try
			{
				functionWrapper.Init(null);
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

			if (functionWrapper is ReflectedActionWrapper){
				(functionWrapper as ReflectedActionWrapper).Call();
			} else {
				(functionWrapper as ReflectedFunctionWrapper).Call();
			}

			EndAction();
		}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR
		
		protected override void OnTaskInspectorGUI(){

			if (!Application.isPlaying && GUILayout.Button("Select Static Method")){

				UnityEditor.GenericMenu.MenuFunction2 MethodSelected = (m) => {
					functionWrapper = ReflectedWrapper.Create((MethodInfo)m, blackboard);
				};
				
				var menu = new UnityEditor.GenericMenu();
				foreach (var t in UserTypePrefs.GetPreferedTypesList(typeof(object), true)){
					foreach(var m in t.GetMethods(BindingFlags.Static | BindingFlags.Public).OrderBy(m => !m.IsSpecialName) ){
						
						if (m.IsGenericMethod)
							continue;

						var parameters = m.GetParameters();
						if (parameters.Length > 3)
							continue;

						menu.AddItem(new GUIContent(t.FriendlyName() + "/" + m.SignatureName()), false, MethodSelected, m);

					}
				}
				menu.ShowAsContext();
				Event.current.Use();
			}


			if (targetMethod != null){
				GUILayout.BeginVertical("box");
				UnityEditor.EditorGUILayout.LabelField("Type", targetMethod.DeclaringType.FriendlyName());
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
					EditorUtils.BBParameterField("Save Return Value", variables[0], true);
				}
			}
		}

		#endif
	}
}
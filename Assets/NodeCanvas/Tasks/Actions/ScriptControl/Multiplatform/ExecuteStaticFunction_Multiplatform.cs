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

	[Name("Execute Static Function (mp)")]
	[Category("✫ Script Control/Multiplatform")]
	[Description("Execute a static function of up to 3 parameters and optionaly save the return value")]
	public class ExecuteStaticFunction_Multiplatform : ActionTask {

		[SerializeField]
		private SerializedMethodInfo method;
		[SerializeField]
		private List<BBObjectParameter> parameters = new List<BBObjectParameter>();
		[SerializeField] [BlackboardOnly]
		private BBObjectParameter returnValue;

		private MethodInfo targetMethod{
			get {return method != null && method.Get() != null? method.Get() : null;}
		}

		protected override string info{
			get
			{
				if (method == null)
					return "No Method Selected";
				if (targetMethod == null)
					return string.Format("<color=#ff6457>* {0} *</color>", method.GetMethodString() );

				var returnInfo = targetMethod.ReturnType == typeof(void)? "" : returnValue.ToString() + " = ";
				var paramInfo = "";
				for (var i = 0; i < parameters.Count; i++)
					paramInfo += (i != 0? ", " : "") + parameters[i].ToString();
				return string.Format("{0}{1}.{2} ({3})", returnInfo, targetMethod.DeclaringType.FriendlyName(), targetMethod.Name, paramInfo );
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
			returnValue.value = targetMethod.Invoke(agent, args);
			EndAction();
		}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR
		
		protected override void OnTaskInspectorGUI(){

			if (!Application.isPlaying && GUILayout.Button("Select Static Method")){

				UnityEditor.GenericMenu.MenuFunction2 MethodSelected = (m) => {
					var newMethod = (MethodInfo)m;
					this.method = new SerializedMethodInfo(newMethod);
					this.parameters.Clear();
					foreach(var p in newMethod.GetParameters()){
						var newParam = new BBObjectParameter{bb = blackboard};
						newParam.SetType(p.ParameterType);
						if (p.IsOptional){
							newParam.value = p.DefaultValue;
						}
						parameters.Add(newParam);
					}

					if (newMethod.ReturnType != typeof(void)){
						this.returnValue = new BBObjectParameter{bb = blackboard};
						this.returnValue.SetType(newMethod.ReturnType);
					}					
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
				GUILayout.EndVertical();

				var paramNames = targetMethod.GetParameters().Select(p => p.Name.SplitCamelCase() ).ToArray();
				for (var i = 0; i < paramNames.Length; i++){
					EditorUtils.BBParameterField(paramNames[i], parameters[i]);
				}

				if (targetMethod.ReturnType != typeof(void)){
					EditorUtils.BBParameterField("Save Return Value", returnValue, true);
				}
			}
		}

		#endif
	}
}
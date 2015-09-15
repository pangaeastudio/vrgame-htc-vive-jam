using System.Collections.Generic;
using System.Reflection;
using NodeCanvas.Framework;
using NodeCanvas.Framework.Internal;
using ParadoxNotion;
using ParadoxNotion.Design;
using ParadoxNotion.Serialization;
using UnityEngine;


namespace NodeCanvas.Tasks.Actions{

	[Name("Set Property (mp)")]
	[Category("✫ Script Control/Multiplatform")]
	[Description("Set a property on a script")]
	public class SetProperty_Multiplatform : ActionTask {

		[SerializeField]
		private SerializedMethodInfo method;
		[SerializeField]
		private BBObjectParameter parameter;

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
					return "No Property Selected";
				if (targetMethod == null)
					return string.Format("<color=#ff6457>* {0} *</color>", method.GetMethodString() );
				return string.Format("{0}.{1} = {2}", agentInfo, targetMethod.Name, parameter.ToString() );
			}
		}

		protected override string OnInit(){
			if (targetMethod == null)
				return "SetProperty Error";
			return null;
		}

		protected override void OnExecute(){
			targetMethod.Invoke(agent, new object[]{parameter.value});
			EndAction();
		}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		protected override void OnTaskInspectorGUI(){

			if (!Application.isPlaying && GUILayout.Button("Select Property")){

				System.Action<MethodInfo> MethodSelected = (method)=> {
					this.method = new SerializedMethodInfo(method);
					this.parameter.SetType(method.GetParameters()[0].ParameterType);
				};

				if (agent != null){
					EditorUtils.ShowGameObjectMethodSelectionMenu(agent.gameObject, typeof(void), typeof(object), MethodSelected, 1, true, false);
				} else {
					var menu = new UnityEditor.GenericMenu();
					foreach (var t in UserTypePrefs.GetPreferedTypesList(typeof(Component), true))
						menu = EditorUtils.GetMethodSelectionMenu(t, typeof(void), typeof(object), MethodSelected, 1, true, false, menu);
					menu.ShowAsContext();
					Event.current.Use();
				}				
			}

			if (targetMethod != null){
				GUILayout.BeginVertical("box");
				UnityEditor.EditorGUILayout.LabelField("Type", agentType.FriendlyName());
				UnityEditor.EditorGUILayout.LabelField("Property", targetMethod.Name);
				UnityEditor.EditorGUILayout.LabelField("Set Type", parameter.varType.FriendlyName() );
				GUILayout.EndVertical();
				EditorUtils.BBParameterField("Set Value", parameter);
			}
		}

		#endif
	}
}
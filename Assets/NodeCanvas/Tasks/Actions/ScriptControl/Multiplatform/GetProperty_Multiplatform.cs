using System.Reflection;
using NodeCanvas.Framework;
using NodeCanvas.Framework.Internal;
using ParadoxNotion;
using ParadoxNotion.Design;
using ParadoxNotion.Serialization;
using UnityEngine;


namespace NodeCanvas.Tasks.Actions{

	[Name("Get Property (mp)")]
	[Category("✫ Script Control/Multiplatform")]
	[Description("Get a property of a script and save it to the blackboard")]
	public class GetProperty_Multiplatform : ActionTask {

		[SerializeField]
		private SerializedMethodInfo method;
		[SerializeField] [BlackboardOnly]
		private BBObjectParameter returnValue;

		private MethodInfo targetMethod{
			get {return method != null && method.Get() != null? method.Get() : null; }
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
				return string.Format("{0} = {1}.{2}", returnValue.ToString(), agentInfo, targetMethod.Name);
			}
		}

		//store the method info on init for performance
		protected override string OnInit(){
			if (targetMethod == null)
				return "GetProperty Error";
			return null;
		}

		//do it by invoking method
		protected override void OnExecute(){
			returnValue.value = targetMethod.Invoke(agent, null);
			EndAction();
		}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		protected override void OnTaskInspectorGUI(){

			if (!Application.isPlaying && GUILayout.Button("Select Property")){
				System.Action<MethodInfo> MethodSelected = (method)=>{
					this.method = new SerializedMethodInfo(method);
					this.returnValue.SetType(method.ReturnType);
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
				EditorUtils.BBParameterField("Save As", returnValue, true);
			}
		}

		#endif
	}
}

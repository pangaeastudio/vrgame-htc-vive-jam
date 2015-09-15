#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using System.Linq;
using NodeCanvas.BehaviourTrees;
using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;


namespace NodeCanvas.StateMachines{

	[Name("BehaviourTree")]
	[Category("Nested")]
	[Description("Execute a Behaviour Tree OnEnter. OnExit that Behavior Tree will be stoped. You can optionaly specify a Success Event and a Failure Event which will be sent when the BT's root node status returns either of the two. If so, use alongside with a CheckEvent on a transition.")]
	public class NestedBTState : FSMState, IGraphAssignable{

		public enum BTExecutionMode
		{
			RunOnce,
			RunForever
		}

		[SerializeField]
		private BBParameter<BehaviourTree> _nestedBT;

		public BTExecutionMode executionMode = BTExecutionMode.RunForever;
		public float updateInterval;
		public string successEvent;
		public string failureEvent;
	
		private readonly Dictionary<BehaviourTree, BehaviourTree> instances = new Dictionary<BehaviourTree, BehaviourTree>();
		private bool BTIsFinished;

		public BehaviourTree nestedBT{
			get {return _nestedBT.value;}
			set
			{
				_nestedBT.value = value;
				if (_nestedBT.value != null){
					_nestedBT.value.agent = graphAgent;
					_nestedBT.value.blackboard = graphBlackboard;
				}
			}
		}

		public Graph nestedGraph{
			get {return nestedBT;}
			set {nestedBT = (BehaviourTree)value;}
		}

		protected override void OnInit(){
			if (nestedBT != null)
				CheckInstance();
		}

		protected override void OnEnter(){

			if (nestedBT == null){
				Finish(false);
				return;
			}

			CheckInstance();

			BTIsFinished = false;
			nestedBT.repeat = (executionMode == BTExecutionMode.RunForever);
			nestedBT.updateInterval = updateInterval;
			nestedBT.StartGraph(graphAgent, graphBlackboard, ()=> {BTIsFinished = true;});
		}

		protected override void OnUpdate(){

			if (!string.IsNullOrEmpty(successEvent) && nestedBT.rootStatus == Status.Success)
				SendEvent(new EventData(successEvent));

			if (!string.IsNullOrEmpty(failureEvent) && nestedBT.rootStatus == Status.Failure)
				SendEvent(new EventData(failureEvent));
			
			if (BTIsFinished)
				Finish();
		}

		protected override void OnExit(){
			if (nestedBT && nestedBT.isRunning)
				nestedBT.Stop();
		}

		protected override void OnPause(){
			if (nestedBT && nestedBT.isRunning)
				nestedBT.Pause();
		}

		void CheckInstance(){

			if (instances.Values.Contains(nestedBT))
				return;

			if (!instances.ContainsKey(nestedBT))
				instances[nestedBT] = ( nestedBT = Graph.Clone<BehaviourTree>(nestedBT) );
		}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR
		
		protected override void OnNodeGUI(){
			
			GUILayout.Label(string.Format("Nested BT\n{0}", _nestedBT) );
			if (nestedBT == null){
				if (!Application.isPlaying && GUILayout.Button("CREATE NEW"))
					Node.CreateNested<BehaviourTree>(this);
			}
		}

		protected override void OnNodeInspectorGUI(){

			ShowBaseFSMInspectorGUI();
			EditorUtils.BBParameterField("Behaviour Tree", _nestedBT);

			if (nestedBT == null)
				return;

			executionMode = (BTExecutionMode)EditorGUILayout.EnumPopup("Execution Mode", executionMode);
			if (executionMode == BTExecutionMode.RunForever)
				updateInterval = EditorGUILayout.FloatField("Update Interval", updateInterval);

			var alpha1 = string.IsNullOrEmpty(successEvent)? 0.5f : 1;
			var alpha2 = string.IsNullOrEmpty(failureEvent)? 0.5f : 1;
			GUILayout.BeginVertical("box");
			GUI.color = new Color(1,1,1,alpha1);
			successEvent = EditorGUILayout.TextField("Success Event", successEvent);
			GUI.color = new Color(1,1,1,alpha2);
			failureEvent = EditorGUILayout.TextField("Failure Event", failureEvent);
			GUILayout.EndVertical();
			GUI.color = Color.white;

			nestedBT.name = name;


	    	var defParams = nestedBT.GetDefinedParameters();
	    	if (defParams.Length != 0){

		    	EditorUtils.TitledSeparator("Defined Nested BT Parameters");
		    	GUI.color = Color.yellow;
		    	EditorGUILayout.LabelField("Name", "Type");
				GUI.color = Color.white;
		    	var added = new List<string>();
		    	foreach(var bbVar in defParams){
		    		if (!added.Contains(bbVar.name)){
			    		EditorGUILayout.LabelField(bbVar.name, bbVar.varType.FriendlyName());
			    		added.Add(bbVar.name);
			    	}
		    	}
		    	if (GUILayout.Button("Check/Create Blackboard Variables")){
		    		nestedBT.CreateDefinedParameterVariables(graphBlackboard);
		    	}
		    }
		}

		#endif
	}
}
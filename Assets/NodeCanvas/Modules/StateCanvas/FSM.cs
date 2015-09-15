using System.Linq;
using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;


namespace NodeCanvas.StateMachines{

	/// <summary>
	/// Use FSMs to create state like behaviours
	/// </summary>
	public class FSM : Graph{

		private FSMState currentState;
		private FSMState previousState;
		private IUpdatable[] updatableNodes;

		private event System.Action<IState> CallbackEnter;
		private event System.Action<IState> CallbackStay;
		private event System.Action<IState> CallbackExit;

		///The current state name. Null if none
		public string currentStateName{
			get {return currentState != null? currentState.name : null; }
		}

		///The previous state name. Null if none
		public string previousStateName{
			get	{return previousState != null? previousState.name : null; }
		}

		public override System.Type baseNodeType{ get {return typeof(FSMState);} }
		public override bool requiresAgent{	get {return true;} }
		public override bool requiresPrimeNode { get {return true;} }
		public override bool autoSort{ get {return false;} }


		protected override void OnGraphStarted(){

			GatherDelegates();

			//collection AnyStates and ConcurentStates
			updatableNodes = allNodes.OfType<IUpdatable>().ToArray();
			foreach (var conc in updatableNodes.OfType<ConcurrentState>()){
				conc.Execute(agent, blackboard);
			}

			EnterState(previousState == null? (FSMState)primeNode : previousState);
		}

		protected override void OnGraphUpdate(){

			if (currentState == null){
				Debug.LogError("Current FSM State is or became null. Stopping FSM...");
				Stop();
			}

			//do this first. This automaticaly stops the graph if the current state is finished and has no transitions
			if (currentState.status != Status.Running && currentState.outConnections.Count == 0){
				Stop();
				return;
			}

			//Update AnyStates and ConcurentStates
			for (var i = 0; i < updatableNodes.Length; i++)
				updatableNodes[i].Update();

			//Update current state
			currentState.Update();
			
			if (CallbackStay != null && currentState != null && currentState.status == Status.Running)
				CallbackStay(currentState);
		}

		protected override void OnGraphStoped(){
			previousState = null;
			currentState = null;
		}

		protected override void OnGraphPaused(){
			previousState = currentState;
			currentState = null;
		}

		///Enter a state providing the state itself
		public bool EnterState(FSMState newState){

			if (!isRunning){
				Debug.LogWarning("Tried to EnterState on an FSM that was not running", this);
				return false;
			}

			if (newState == null){
				Debug.LogWarning("Tried to Enter Null State");
				return false;
			}
/*
			if (currentState == newState){
				Debug.Log("Trying entering the same state");
				return false;
			}
*/
			if (currentState != null){	
				currentState.Finish();
				currentState.Reset();
				if (CallbackExit != null)
					CallbackExit(currentState);
				
				//for editor..
				foreach (var inConnection in currentState.inConnections)
					inConnection.connectionStatus = Status.Resting;
				///
			}

			previousState = currentState;
			currentState = newState;
			currentState.Execute(agent, blackboard);
			if (CallbackEnter != null)
				CallbackEnter(currentState);
			return true;
		}

		///Trigger a state to enter by it's name. Returns the state found and entered if any
		public FSMState TriggerState(string stateName){

			var state = GetStateWithName(stateName);
			if (state != null){
				EnterState(state);
				return state;
			}

			Debug.LogWarning("No State with name '" + stateName + "' found on FSM '" + name + "'");
			return null;
		}

		///Get all State Names
		public string[] GetStateNames(){
			return allNodes.Where(n => n.allowAsPrime).Select(n => n.name).ToArray();
		}

		///Get a state by it's name
		public FSMState GetStateWithName(string name){
			return (FSMState)allNodes.Find(n => n.allowAsPrime && n.name == name);
		}

		//Gather and creates delegates from MonoBehaviours on agents tht implement required methods
		void GatherDelegates(){

			foreach (var mono in agent.gameObject.GetComponents<MonoBehaviour>()){
                
				var enterMethod = mono.GetType().RTGetMethod("OnStateEnter");
				var stayMethod = mono.GetType().RTGetMethod("OnStateUpdate");
				var exitMethod = mono.GetType().RTGetMethod("OnStateExit");

				if (enterMethod != null){
					#if !UNITY_IOS
					CallbackEnter += enterMethod.RTCreateDelegate<System.Action<IState>>(mono);
					#else
					CallbackEnter += (s)=>{ enterMethod.Invoke(mono, new object[]{s}); };
					#endif
				}

				if (stayMethod != null){
					#if !UNITY_IOS
					CallbackStay += stayMethod.RTCreateDelegate<System.Action<IState>>(mono);
					#else
					CallbackStay += (s)=>{ stayMethod.Invoke(mono, new object[]{s}); };
					#endif
				}

				if (exitMethod != null){
					#if !UNITY_IOS
					CallbackExit += exitMethod.RTCreateDelegate<System.Action<IState>>(mono);
					#else
					CallbackExit += (s)=>{ exitMethod.Invoke(mono, new object[]{s}); };
					#endif
				}
			}
		}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR
		
		[UnityEditor.MenuItem("Window/NodeCanvas/Create/Graph/FSM")]
		public static void Editor_CreateGraph(){
			var newGraph = EditorUtils.CreateAsset<FSM>(true);
			UnityEditor.Selection.activeObject = newGraph;
		}

		[UnityEditor.MenuItem("Assets/Create/NodeCanvas/FSM")]
		public static void Editor_CreateGraphFix(){
			var path = EditorUtils.GetAssetUniquePath("FSM.asset");
			var newGraph = EditorUtils.CreateAsset<FSM>(path);
			UnityEditor.Selection.activeObject = newGraph;
		}
		
		#endif
	}
}
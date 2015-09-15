using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;


namespace NodeCanvas.BehaviourTrees{

	/// <summary>
	/// BehaviourTrees are used to create advanced AI and logic based on simple rules.
	/// </summary>
	public class BehaviourTree : Graph {

		///Should the tree repeat forever?
		public bool repeat = true;
		///The frequency in seconds for the tree to repeat if set to repeat.
		public float updateInterval = 0;
		
		///This event is called when the root status of the behaviour is changed
		public event System.Action<BehaviourTree, Status> onRootStatusChanged;

		private float intervalCounter = 0;
		private Status _rootStatus = Status.Resting;

		///The last status of the root
		public Status rootStatus{
			get{return _rootStatus;}
			private set
			{
				if (_rootStatus != value){
					_rootStatus = value;
					if (onRootStatusChanged != null)
						onRootStatusChanged(this, value);
				}
			}
		}

		public override System.Type baseNodeType{ get {return typeof(BTNode);} }
		public override bool requiresAgent{	get {return true;} }
		public override bool requiresPrimeNode { get {return true;} }
		public override bool autoSort{ get {return true;} }

		protected override void OnGraphStarted(){

			intervalCounter = updateInterval;
			rootStatus = primeNode.status;
		}

		protected override void OnGraphUpdate(){

			if (intervalCounter >= updateInterval){
				intervalCounter = 0;
				if ( Tick(agent, blackboard) != Status.Running && !repeat)
					Stop();
			}

			if (updateInterval > 0)
				intervalCounter += Time.deltaTime;
		}

		///Tick the tree once for the provided agent and with the provided blackboard
		public Status Tick(Component agent, IBlackboard blackboard){

			if (rootStatus != Status.Running)
				primeNode.Reset();

			rootStatus = primeNode.Execute(agent, blackboard);
			return rootStatus;
		}



		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR
		
		[UnityEditor.MenuItem("Window/NodeCanvas/Create/Graph/Behaviour Tree")]
		public static void Editor_CreateGraph(){
			var newGraph = EditorUtils.CreateAsset<BehaviourTree>(true);
			UnityEditor.Selection.activeObject = newGraph;
		}

		[UnityEditor.MenuItem("Assets/Create/NodeCanvas/Behaviour Tree")]
		public static void Editor_CreateGraphFix(){
			var path = EditorUtils.GetAssetUniquePath("BehaviourTree.asset");
			var newGraph = EditorUtils.CreateAsset<BehaviourTree>(path);
			UnityEditor.Selection.activeObject = newGraph;
		}

		#endif
	}
}
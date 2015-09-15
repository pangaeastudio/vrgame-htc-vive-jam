using System.Collections.Generic;
using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;


namespace NodeCanvas.BehaviourTrees{

	[Category("Composites")]
	[Description("Execute all child nodes once but simultaneously and return Success or Failure depending on the selected ParallelPolicy.\nIf is Dynamic higher priority chilren status are revaluated")]
	[Icon("Parallel")]
	public class Parallel : BTComposite{

		public enum ParallelPolicy
		{
			FirstFailure,
			FirstSuccess,
			FirstSuccessOrFailure
		}

		public ParallelPolicy policy = ParallelPolicy.FirstFailure;
		public bool dynamic;

		private readonly List<Connection> finishedConnections = new List<Connection>();

		public override string name{
			get {return string.Format("<color=#ff64cb>{0}</color>", base.name.ToUpper());}
		}

		protected override Status OnExecute(Component agent, IBlackboard blackboard){

			for ( var i= 0; i < outConnections.Count; i++){

				if (!dynamic && finishedConnections.Contains(outConnections[i]))
					continue;

				status = outConnections[i].Execute(agent, blackboard);

				if (status == Status.Failure && (policy == ParallelPolicy.FirstFailure || policy == ParallelPolicy.FirstSuccessOrFailure) ){
					ResetRunning();
					return Status.Failure;
				}

				if (status == Status.Success && (policy == ParallelPolicy.FirstSuccess || policy == ParallelPolicy.FirstSuccessOrFailure) ){
					ResetRunning();
					return Status.Success;
				}

				if (status != Status.Running && !finishedConnections.Contains(outConnections[i]))
					finishedConnections.Add(outConnections[i]);
			}

		    if ( finishedConnections.Count != outConnections.Count ) return Status.Running;
		    switch(policy) 
            {
		        case ParallelPolicy.FirstFailure:
		            return Status.Success;
		        case ParallelPolicy.FirstSuccess:
		            return Status.Failure;
		    }

		    return Status.Running;
		}

		protected override void OnReset(){
			finishedConnections.Clear();
		}

		void ResetRunning(){
			for (var i = 0; i < outConnections.Count; i++){
				if (outConnections[i].connectionStatus == Status.Running)
					outConnections[i].Reset();
			}
		}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR
		
		protected override void OnNodeGUI(){
			GUILayout.Label( (dynamic? "<b>DYNAMIC</b>\n" : "") + policy.ToString().SplitCamelCase() );
		}

		#endif
	}
}
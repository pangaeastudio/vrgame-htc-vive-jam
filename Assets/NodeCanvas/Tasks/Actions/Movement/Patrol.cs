using System.Collections.Generic;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;


namespace NodeCanvas.Tasks.Actions{

	[Category("Movement")]
	[Description("Move randomly between various game object positions taken from the list provided")]
	public class Patrol : ActionTask<NavMeshAgent> {

		[RequiredField]
		public BBParameter<List<GameObject>> targetList;
		public BBParameter<float> speed = 3;
		public float keepDistance = 0.1f;

		private int index;
		private Vector3? lastRequest;

		protected override string info{
			get {return "Random Patrol " + targetList;}
		}

		protected override void OnExecute(){

			var newIndex = Random.Range(0, targetList.value.Count);
			while(newIndex == index)
				newIndex = Random.Range(0, targetList.value.Count);
			index = newIndex;

			var targetGo = targetList.value[index];
			if (targetGo == null){
				Debug.LogWarning("List's game object is null on MoveToFromList Action");
				EndAction(false);
				return;
			}

			var targetPos = targetGo.transform.position;

			agent.speed = speed.value;
			if ( (agent.transform.position - targetPos).magnitude < agent.stoppingDistance + keepDistance){
				EndAction(true);
				return;
			}

			Go();
		}

		protected override void OnUpdate(){
			Go();
		}

		void Go(){

			var targetPos = targetList.value[index].transform.position;
			if (lastRequest != targetPos){
				if ( !agent.SetDestination( targetPos) ){
					EndAction(false);
					return;
				}
			}

			lastRequest = targetPos;

			if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + keepDistance)
				EndAction(true);			
		}

		protected override void OnStop(){

			lastRequest = null;
			if (agent.gameObject.activeSelf)
				agent.ResetPath();
		}

		protected override void OnPause(){
			OnStop();
		}

		public override void OnDrawGizmosSelected(){
			if (agent && targetList.value != null){
				foreach (var go in targetList.value){
					if (go)	Gizmos.DrawSphere(go.transform.position, 0.1f);
				}
			}
		}
	}
}
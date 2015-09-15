using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;


namespace NodeCanvas.Tasks.Conditions{

	[Category("GameObject")]
	[Description("A combination of line of sight and view angle check")]
	public class CanSeeTarget : ConditionTask<Transform> {

		[RequiredField]
		public BBParameter<GameObject> target;
		public BBParameter<float> maxDistance = 50;
		[SliderField(1, 180)]
		public BBParameter<float> viewAngle = 70f;
		public Vector3 offset;

		private RaycastHit hit;

		protected override string info{
			get {return "Can See " + target.ToString();}
		}

		protected override bool OnCheck(){
			
			var t = target.value.transform;
			if ( (agent.position - t.position).magnitude > maxDistance.value )
				return false;

			if (Physics.Linecast(agent.position + offset, t.position + offset, out hit)){
				if (hit.collider != t.GetComponent<Collider>())
					return false;
			}

			return Vector3.Angle(t.position - agent.position, agent.forward) < viewAngle.value;
		}
	}
}
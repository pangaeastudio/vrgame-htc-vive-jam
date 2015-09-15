using System;
using System.Collections;
using UnityEngine;


namespace NodeCanvas.Framework{

    ///Base class for all Conditions. Conditions dont span multiple frames like actions and return true or false immediately on execution. Derive this to create your own.
    ///Generic version to define the AgentType instead of using the [AgentType] attribute. T is the agentType required by the Condition.
	abstract public class ConditionTask<T> : ConditionTask where T:Component{
	
		public override Type agentType{
			get {return typeof(T);}
		}

		new public T agent{
			get
			{
				try { return (T)base.agent; }
				catch {return null;}
			}
		}		
	}

	///Base class for all Conditions. Conditions dont span multiple frames like actions and return true or false immediately on execution. Derive this to create your own
	abstract public class ConditionTask : Task{

		[SerializeField]
		private bool _invert;

		[NonSerialized]
		private int yieldReturn = -1;

		public bool invert{
			get {return _invert;}
			set {_invert = value;}
		}

		///Check the condition for the provided agent and with the provided blackboard
		public bool CheckCondition(Component agent, IBlackboard blackboard){

			if (!isActive)
				return false;

			if (!Set(agent, blackboard)){
				isActive = false;
				return false;
			}

			if (yieldReturn != -1){
				var b = invert? !(yieldReturn == 1) : (yieldReturn == 1);
				yieldReturn = -1;
				return b;
			}

			return invert? !OnCheck() : OnCheck();
		}

		///Override in your own conditions and return whether the condition is true or false. The result will be inverted if Invert is checked.
		virtual protected bool OnCheck(){ return true; }

		///Helper method that holds the return value provided for one frame, for the condition to return.
		protected void YieldReturn(bool value){
			yieldReturn = value? 1 : 0;
			StartCoroutine(Flip());
		}

		IEnumerator Flip(){
			yield return null;
			yieldReturn = -1;
		}
	}
}
using NodeCanvas.Framework;


namespace NodeCanvas.BehaviourTrees{

	/// <summary>
	/// Base class for BehaviourTree Decorator nodes.
	/// </summary>
	abstract public class BTDecorator : BTNode{

		sealed public override int maxOutConnections{ get{return 1;}}
		sealed public override bool showCommentsBottom{ get{return false;}}

		///The decorated connection object
		protected Connection decoratedConnection{
			get
			{
				try { return outConnections[0]; }
				catch {return null;}
			}
		}

		///The decorated node object
		protected Node decoratedNode{
			get
			{
				try {return outConnections[0].targetNode;}
				catch {return null;}
			}
		}
	}
}
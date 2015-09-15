using System.Collections.Generic;
using UnityEngine;


namespace NodeCanvas.Framework.Internal{

	///The object used to serialize and deserialize graphs. This class serves no other purpose
	public class GraphSerializationData {

		private readonly float SerializationVersion = 2.2f;

		public float version;
		public System.Type type;
		public string name;
		public string comments;
		public Vector2 translation;
		public List<Node> nodes;
		public List<Connection> connections;
		public Node primeNode;
		public List<CanvasGroup> canvasGroups;
		public IBlackboard localBlackboard;

		//required
		public GraphSerializationData(){}

		//Construct
		public GraphSerializationData(Graph graph){

			this.version         = SerializationVersion;
			this.type            = graph.GetType();
			this.name            = graph.name;
			this.comments        = graph.graphComments;
			this.translation     = graph.translation;
			this.nodes           = graph.allNodes;
			this.canvasGroups    = graph.canvasGroups;
			this.localBlackboard = graph.localBlackboard;

			var structConnections = new List<Connection>();
			foreach (var node in nodes){

				if (node is ISerializationCallbackReceiver)
					(node as ISerializationCallbackReceiver).OnBeforeSerialize();

				foreach (var c in node.outConnections)
					structConnections.Add(c);
			}

			this.connections = structConnections;
			this.primeNode   = graph.primeNode;
		}

		///MUST reconstruct before using the data
		public void Reconstruct(Graph graph){

			//check serialization versions here in the future

			foreach (var connection in this.connections){
				connection.sourceNode.outConnections.Add(connection);
				connection.targetNode.inConnections.Add(connection);
			}

			//re-set the node's owner and on after deserialize for nodes that need it
			foreach (var node in nodes){
				node.graph = graph;
				if (node is ISerializationCallbackReceiver){
					(node as ISerializationCallbackReceiver).OnAfterDeserialize();
				}
			}
		}
	}
}
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;


namespace NodeCanvas.DialogueTrees{

	[Name("✫ Multiple Choice")]
	[Description("Prompt a Dialogue Multiple Choice. A choice will be available if the connection's condition is true or there is no condition on that connection. The Actor selected is used for the Condition checks as well as will Say the selection if the option is checked.")]
	public class MultipleChoiceNode : DTNode, ISubTasksContainer{

		[System.Serializable]
		public class Choice{

			public bool isUnfolded = true;
			public Statement statement;
			public ConditionTask condition;

			public Choice(){}
			public Choice(Statement statement){
				this.statement = statement;
			}
		}


		public Task[] GetTasks(){
			return availableChoices != null? availableChoices.Select(c => c.condition).ToArray() : null;
		}



		public float availableTime = 0;
		public bool saySelection = false;
		public List<Choice> availableChoices = new List<Choice>();
		
		private bool isWaitingChoice = false;

		public override int maxOutConnections{ get{return -1;} }

		public override void OnChildConnected(int index){
			if (availableChoices.Count < outConnections.Count)
				availableChoices.Insert(index, new Choice(new Statement("...")));
		}

		public override void OnChildDisconnected(int index){
			if (availableChoices.Count < outConnections.Count)
				availableChoices.RemoveAt(index);
		}

		protected override Status OnExecute(Component agent, IBlackboard bb){

			if (outConnections.Count == 0)
				return Error("There are no connections to the Multiple Choice Node!");

			var finalOptions = new Dictionary<IStatement, int>();

			for (var i = 0; i < availableChoices.Count; i++){
				var condition = availableChoices[i].condition;
				if (condition == null || condition.CheckCondition(finalActor.transform, bb)){
					var tempStatement = availableChoices[i].statement.BlackboardReplace(bb);
					finalOptions[tempStatement] = i;					
				}
			}

			if (finalOptions.Count == 0){
				Debug.Log("Multiple Choice Node has no available options. Dialogue Ends");
				DLGTree.Stop();
				return Status.Failure;
			}

			if (availableTime > 0)
				StartCoroutine(CountDown());

			var optionsInfo = new MultipleChoiceRequestInfo(finalOptions, availableTime, OnOptionSelected);
			DialogueTree.RequestMultipleChoices( optionsInfo );
			return Status.Running;
		}

		IEnumerator CountDown(){

			isWaitingChoice = true;
			float timer = 0;
			while (timer < availableTime){
				
				if (!DLGTree.isRunning)
					yield break;

				if (!isWaitingChoice)
					yield break;

				timer += Time.deltaTime;
				yield return null;
			}

			for (var i = outConnections.Count - 1; i >= 0; i--){
				var condition = availableChoices[i].condition;
				if (condition == null || condition.CheckCondition(finalActor.transform, graphBlackboard)){
					OnOptionSelected(i);
					yield break;
				}
			}
		}

		void OnOptionSelected(int index){

			status = Status.Success;
			isWaitingChoice = false;

			System.Action Finalize = ()=> { DLGTree.Continue(index); };

			if (saySelection){
				var tempStatement = availableChoices[index].statement.BlackboardReplace(graphBlackboard);
				var speechInfo = new SubtitlesRequestInfo( finalActor, tempStatement, Finalize );
				DialogueTree.RequestSubtitles(speechInfo);
			} else {
				Finalize();
			}
		}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		//every label witin OnNodeGUI has it's alignment set to middle, but we need a left here.
		GUIStyle _leftLabelStyle;
		GUIStyle leftLabelStyle{
			get
			{
				if (_leftLabelStyle == null){
					_leftLabelStyle = new GUIStyle(GUI.skin.GetStyle("label"));
					_leftLabelStyle.alignment = TextAnchor.UpperLeft;
				}
				return _leftLabelStyle;
			}
		}

		public override void OnConnectionInspectorGUI(int i){
			EditorUtils.TaskField<ConditionTask>(availableChoices[i].condition, graph, (c)=> { availableChoices[i].condition = c; });
		}

		public override string GetConnectionInfo(int i){
			var text = availableChoices[i].statement.text;
			if (availableChoices[i].condition == null)
				return text;
			return string.Format("{0}\n{1}", text, availableChoices[i].condition.summaryInfo );
		}
	
		protected override void OnNodeGUI(){

			if (outConnections.Count == 0){
				GUILayout.Label("Connect Outcomes");
				return;
			}

			for (var i= 0; i < outConnections.Count; i++){
				GUILayout.BeginHorizontal("box");
				GUILayout.Label("#" + outConnections[i].targetNode.ID.ToString() + ") " + availableChoices[i].statement.text, leftLabelStyle );
				GUILayout.EndHorizontal();
			}

			GUILayout.BeginHorizontal();
			if (availableTime > 0)
				GUILayout.Label("Choose in '" + availableTime + "' seconds");
			if (saySelection)
				GUILayout.Label("Say Selection");
			GUILayout.EndHorizontal();
		}

		protected override void OnNodeInspectorGUI(){

			base.OnNodeInspectorGUI();

			if (outConnections.Count == 0){
				GUILayout.Label("<b>No Children Connected</b>");
				return;
			}

			var e = Event.current;

			for (var i= 0; i < outConnections.Count; i++){

				GUILayout.BeginHorizontal("box");

					var arrowDir = availableChoices[i].isUnfolded? "▼ " : "► ";
					var condition = availableChoices[i].condition;
					if (condition != null){
						GUILayout.Label(arrowDir + condition.summaryInfo);
					} else {
						GUILayout.Label(arrowDir + "Available Choice");
					}

					var lastRect = GUILayoutUtility.GetLastRect();
					if (e.type == EventType.MouseUp && lastRect.Contains(e.mousePosition)){
						availableChoices[i].isUnfolded = !availableChoices[i].isUnfolded;
						e.Use();
					}
					
					if (GUILayout.Button("►", GUILayout.Width(20)))
						Graph.currentSelection = outConnections[i];

				GUILayout.EndHorizontal();

				if (!availableChoices[i].isUnfolded)
					continue;

				GUILayout.BeginVertical("box");

					availableChoices[i].statement.text = UnityEditor.EditorGUILayout.TextField(availableChoices[i].statement.text);
					availableChoices[i].statement.audio = UnityEditor.EditorGUILayout.ObjectField("Audio File", availableChoices[i].statement.audio, typeof(AudioClip), false) as AudioClip;
					availableChoices[i].statement.meta = UnityEditor.EditorGUILayout.TextField("Meta Data", availableChoices[i].statement.meta);
					UnityEditor.EditorGUILayout.Space();

				GUILayout.EndVertical();
				GUILayout.Space(10);
			}

			availableTime = UnityEditor.EditorGUILayout.Slider("Available Time", availableTime, 0, 10);
			saySelection = UnityEditor.EditorGUILayout.Toggle("Say Selection", saySelection);
		}

		#endif
	}
}
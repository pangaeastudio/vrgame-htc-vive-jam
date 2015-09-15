using UnityEngine;
using ParadoxNotion.Design;
using NodeCanvas.Framework;
using NodeCanvas.DialogueTrees;

namespace NodeCanvas.Tasks.Actions{

	[Category("Dialogue")]
	[AgentType(typeof(IDialogueActor))]
	[Description("Starts a Dialogue Tree with specified agent for 'Instigator'")]
	[Icon("Dialogue")]
	public class StartDialogueTree : ActionTask {

		[RequiredField]
		public BBParameter<DialogueTree> dialogueTree;
		public bool waitActionFinish = true;

		protected override string info{
			get {return string.Format("Start Dialogue {0}", dialogueTree.ToString());}
		}

		protected override void OnExecute(){
			
			var actor = (IDialogueActor)agent;
			if (waitActionFinish){
				dialogueTree.value.StartDialogue(actor, ()=> {EndAction();} );
			} else {
				dialogueTree.value.StartDialogue(actor);
				EndAction();
			}
		}


		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		protected override void OnTaskInspectorGUI(){
			dialogueTree.value = (DialogueTree)UnityEditor.EditorGUILayout.ObjectField("Dialogue Tree", dialogueTree.value, typeof(DialogueTree), true);
			waitActionFinish = UnityEditor.EditorGUILayout.Toggle("Wait Action Finish", waitActionFinish);
		}

		#endif
	}
}
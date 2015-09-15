using System;
using System.Collections.Generic;


namespace NodeCanvas.DialogueTrees{

	///Send along with a OnSubtitlesRequest event. Holds info about the actor speaking, the statement that being said as well as a callback to be called when dialogue is done showing
	public class SubtitlesRequestInfo{

		///The actor speaking
		public readonly IDialogueActor actor;
		///The statement said
		public readonly IStatement statement;
		///Call this to Continue the DialogueTree
		public readonly Action Continue;

		public SubtitlesRequestInfo(IDialogueActor actor, IStatement statement, Action callback){
			this.actor = actor;
			this.statement = statement;
			this.Continue = callback;
		}
	}

	///Send along with a OnMultipleChoiceRequest event. Holds information of the options, time available as well as a callback to be called providing the selected option
	public class MultipleChoiceRequestInfo{

		///The available choice option. Key: The statement, Value: the child index of the option
		public readonly Dictionary<IStatement, int> options = new Dictionary<IStatement, int>();
		///The available time for a choice
		public readonly float availableTime = 0;
		///Call this with to select the option to continue with in the DialogueTree
		public readonly Action<int> SelectOption;

		public MultipleChoiceRequestInfo(Dictionary<IStatement, int> options, float availableTime, Action<int> callback){
			this.options = options;
			this.availableTime = availableTime;
			this.SelectOption = callback;
		}
	}
}
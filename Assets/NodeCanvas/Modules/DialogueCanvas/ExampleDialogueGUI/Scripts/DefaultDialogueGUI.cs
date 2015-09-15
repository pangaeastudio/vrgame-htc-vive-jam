using System.Collections;
using UnityEngine;


namespace NodeCanvas.DialogueTrees.UI.Examples{

	public class DefaultDialogueGUI : MonoBehaviour{

		public GUISkin skin;
		public bool showSubtitlesOverActor = true;

		private IDialogueActor currentActor;
		private string displayText;

		private MultipleChoiceRequestInfo currentOptions;
		private float timer;


		//Subscribe to the events
		void Awake(){

			DialogueTree.OnDialogueStarted       += OnDialogueStarted;
			DialogueTree.OnDialoguePaused        += OnDialoguePaused;
			DialogueTree.OnDialogueFinished      += OnDialogueFinished;
			DialogueTree.OnSubtitlesRequest      += OnSubtitlesRequest;
			DialogueTree.OnMultipleChoiceRequest += OnMultipleChoiceRequest;

			enabled = false;
		}


		void OnDialogueStarted(DialogueTree dialogue){
			//We could do something here...
		}

		void OnDialoguePaused(DialogueTree dialogue){
			OnDialogueFinished(dialogue);
		}

		void OnDialogueFinished(DialogueTree dialogue){

			StopAllCoroutines();
			displayText = null;
			if (currentActor != null)
				currentActor.speech = null;

			currentOptions = null;
			StopCoroutine("GUICountDown");

			enabled = false;
		}


		//Function with same name as the event is called when the event is dispatched by the Dialogue Tree
		void OnSubtitlesRequest(SubtitlesRequestInfo info){
			
			enabled = true;
			currentActor = info.actor;
			StatementProcessor.ProcessStatement(info.statement, info.actor, info.Continue);
		}

		//A function with the same name as the subscribed Event is called when the event is dispatched
		void OnMultipleChoiceRequest(MultipleChoiceRequestInfo optionsInfo){
			
			enabled = true;
			currentOptions = optionsInfo;
			timer = optionsInfo.availableTime;
			StopCoroutine("GUICountDown");
			if (timer > 0)
				StartCoroutine("GUICountDown");
		}

		//Countdown for the available time. Picking a choice is done by the graph when it ends. All we need to do is to show and stop
		//showing the UI
		IEnumerator GUICountDown(){
			while (timer > 0){
				timer -= Time.deltaTime;
				yield return null;
			}
			currentOptions = null;
		}


		void OnGUI(){

			GUI.skin = skin;

			if (currentActor != null)
				DoSubtitlesGUI();

			if (currentOptions != null)
				DoMultipleChoiceGUI();
		}


		void DoSubtitlesGUI(){

			displayText = currentActor.speech;

			if (string.IsNullOrEmpty(displayText))
				return;

			//calculate the size needed
			var finalSize= new GUIStyle("box").CalcSize(new GUIContent(displayText));
			var speechRect= new Rect(0,0,0,0);
			speechRect.width = finalSize.x;
			speechRect.height = finalSize.y;

			var talkPos= Camera.main.WorldToScreenPoint(currentActor.dialoguePosition);
			talkPos.y = Screen.height - talkPos.y;

			//if show over actor and the actor's dialoguePosition is in screen, show the tet above the actor at that dialoguePosition
			if (showSubtitlesOverActor && Camera.main.rect.Contains( new Vector2(talkPos.x/Screen.width, talkPos.y/Screen.height) )){

				var newCenter = speechRect.center;
				newCenter.x = talkPos.x;
				newCenter.y = talkPos.y - speechRect.height/2;
				speechRect.center = newCenter;

			//else just show the subtitles at the bottom along with his portrait if any
			} else {

				speechRect = new Rect(10, Screen.height - 60, Screen.width - 20, 50);
				var nameRect = new Rect(0, 0, 200, 28);
				var newCenter = nameRect.center;
				newCenter.x = speechRect.center.x;
				newCenter.y = speechRect.y - 24;
				nameRect.center = newCenter;
				GUI.Box(nameRect, currentActor.name);

				if (currentActor.portrait){
					var portraitRect= new Rect(10, Screen.height - currentActor.portrait.height - 70, currentActor.portrait.width, currentActor.portrait.height);
					GUI.DrawTexture(portraitRect, currentActor.portrait);
				}
			}

			GUI.Box(speechRect, displayText);
		}


		void DoMultipleChoiceGUI(){

			//Calculate the y size needed
			var neededHeight = timer > 0? 20f : 0f;
			foreach (var statement in currentOptions.options.Keys)
				neededHeight += new GUIStyle("box").CalcSize(new GUIContent(statement.text)).y;

			//show the choices which are within a Dictionary of Statement and the int whic is the Index we need to 
			//callback when an option is selected
			var optionsRect= new Rect(10, Screen.height - neededHeight - 10, Screen.width - 20, neededHeight);
			GUILayout.BeginArea(optionsRect);
			foreach (var option in currentOptions.options){

				//When a choice is selected we need to Callback with the index of the statement choice selected
				if (GUILayout.Button(option.Key.text, new GUIStyle("box"), GUILayout.ExpandWidth(true))){
					StopCoroutine("GUICountDown");
					//we do the following caching in case another multiple choice is requested DUE to the callback immediately
					var callback = currentOptions.SelectOption;
					currentOptions = null;
					callback(option.Value);
					return;
				}
			}

			//show the countdown UI
			if (timer > 0){
				var blue = GUI.color.b;
				var green = GUI.color.g;
				blue = timer / currentOptions.availableTime * 0.5f;
				green = timer / currentOptions.availableTime * 0.5f;
				GUI.color = new Color(1f, green, blue);
				GUILayout.Box("...", GUILayout.Height(5), GUILayout.Width(timer / currentOptions.availableTime * optionsRect.width));
			}

			GUILayout.EndArea();

		}
	}
}
#if UNITY_EDITOR

using System.Collections.Generic;
using ParadoxNotion.Services;
using UnityEngine;


namespace ParadoxNotion.Design{

	///Place this on a game object to debug the EventHandler
	///An instance is needed only for when debugging EvenHandler events.
	public class EventHandlerDebugComponent : MonoBehaviour{

		public bool logEvents = false;

		public Dictionary<string, List<EventHandler.SubscribedMember>> subscribedMembers{
			get{return EventHandler.subscribedMembers;}
		}

		void Awake(){
			EventHandler.logEvents = logEvents;
		}
	}
}

#endif
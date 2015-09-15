using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace ParadoxNotion.Services{

	///Handles subscribers and dispatches messages.
	///If you want to debug events send and subscribers, add the EventDebugger component somewhere
	public static class EventHandler{

		public static bool logEvents;
		public static Dictionary<string, List<SubscribedMember>> subscribedMembers = new Dictionary<string, List<SubscribedMember>>();

		public static void Subscribe(object obj, Enum eventEnum, int invokePriority = 0, bool unsubscribeWhenReceive = false){
			Subscribe(obj, eventEnum.ToString(), invokePriority, unsubscribeWhenReceive);
		}

		///Subscribes a object to an Event along with options. When the Event is dispatched a funtion
		///with the same name as the Event will be called on the subscribed object. Events are provided by an Enum or string
		public static void Subscribe(object obj, string eventName, int invokePriority = 0, bool unsubscribeWhenReceive = false){

			var method = obj.GetType().RTGetMethod(eventName, true);
			if (method == null){
				Debug.LogError("EventHandler: No Method with name '" + eventName + "' exists on '" + obj.GetType().Name + "' Subscribed Type");
				return;
			}

			if (!subscribedMembers.ContainsKey(eventName))
				subscribedMembers[eventName] = new List<SubscribedMember>();

			foreach (var member in subscribedMembers[eventName]){
				if (member.subscribedObject == obj){
					Debug.LogWarning("obj " + obj + " is allready subscribed to " + eventName);
					return;
				}
			}

			if (logEvents)
				Debug.Log("@@@ " + obj + " subscribed to " + eventName);
			
			subscribedMembers[eventName].Add(new SubscribedMember(obj, invokePriority, unsubscribeWhenReceive));
			subscribedMembers[eventName] = subscribedMembers[eventName].OrderBy(member => -member.invokePriority).ToList();
		}


		//Subscribe a function to an Event by enum name
		public static void Subscribe(Enum eventEnum, Action<object> func){
			Subscribe(eventEnum.ToString(), func);
		}

		//Subscribe a function to an Event by string name
		public static void Subscribe(string eventName, Action<object> func){

			if (!subscribedMembers.ContainsKey(eventName))
				subscribedMembers[eventName] = new List<SubscribedMember>();

			foreach (var member in subscribedMembers[eventName]) {
				
				if (member.subscribedFunction == func){
					
					if (logEvents)
						Debug.Log("Function allready subscribed to " + eventName);
					
					return;
				}
			}

			subscribedMembers[eventName].Add(new SubscribedMember(func, 0, false));
		}


		///Unsubscribe a object member from all Events
		public static void Unsubscribe(object obj){

			if (obj == null)
				return;

			foreach (var eventName in subscribedMembers.Keys){
				foreach (var member in subscribedMembers[eventName].ToArray()){

					if (member.subscribedObject == obj){

						subscribedMembers[eventName].Remove(member);

						if (logEvents)
							Debug.Log("XXX " + obj + "Unsubscribed from everything!");
					}
				}
			}
		}

		public static void Unsubscribe(object obj, Enum eventEnum){
			Unsubscribe(obj, eventEnum.ToString());
		}		

		///Unsubscribes a object member from an Event
		public static void Unsubscribe(object obj, string eventName){

			if (obj == null || !subscribedMembers.ContainsKey(eventName))
				return;

			foreach (var member in subscribedMembers[eventName].ToArray()){

				if (member.subscribedObject == obj){

					subscribedMembers[eventName].Remove(member);

					if (logEvents)
						Debug.Log("XXX Member " + obj + " Unsubscribed from " + eventName);

					return;
				}
			}

			if (logEvents)
				Debug.Log("You tried to Unsubscribe " + obj + " from " + eventName + ", but it was never subscribed there!");
		}

		//Unsubscribes a Function member from everything
		public static void UnsubscribeFunction(Action<object> func){

			if (func == null)
				return;

			foreach (var eventName in subscribedMembers.Keys){
				foreach (var member in subscribedMembers[eventName].ToArray()){
					if (member.subscribedFunction != null && member.subscribedFunction.ToString() == func.ToString())
						subscribedMembers[eventName].Remove(member);
				}
			}

			if (logEvents)
				Debug.Log("XXX " + func.ToString() + " Unsubscribed from everything");
		}

		public static bool Dispatch(Enum eventEnum, object arg = null){
			return Dispatch(eventEnum.ToString(), arg);
		}

		///Dispatches a new Event. On any subscribers listening, a function of the same name as the Event will be called. An Object may be passed as an argument.
		public static bool Dispatch(string eventName, object arg = null){

			if (logEvents)
				Debug.Log(">>> Event " + eventName + " Dispatched. (" + (arg != null? arg.GetType().Name : "" ) + ") Argument");

			if (!subscribedMembers.ContainsKey(eventName)){
				Debug.LogWarning("EventHandler: Event '" + eventName + "' was not received by anyone!");
				return false;
			}

			foreach (var member in subscribedMembers[eventName].ToArray()){

				var obj = member.subscribedObject;

				//clean up by-product
				if (obj == null && member.subscribedFunction == null){
					subscribedMembers[eventName].Remove(member);
					continue;
				}

				if (logEvents)
					Debug.Log("<<< Event " + eventName + " Received by " + obj);

				if (member.unsubscribeWhenReceive)
					Unsubscribe(obj, eventName);

				if (member.subscribedFunction != null){
					member.subscribedFunction(arg);
					continue;
				}
				
				var method = obj.GetType().RTGetMethod(eventName, true);
				if (method == null){
					Debug.LogWarning("Method '" + eventName + "' not found on subscribed object '" + obj + "'");
					continue;
				}

				var parameters = method.GetParameters();
				if (parameters.Length > 1){
					Debug.LogError("Subscribed function to call '" + method.Name + "' has more than one parameter on " + obj + ". It should only have one.");
					continue;
				}

				var args = parameters.Length == 1? new object[]{arg} : null;
				if (method.ReturnType == typeof(IEnumerator)){
					MonoManager.current.StartCoroutine( (IEnumerator)method.Invoke(obj, args) );
				} else {
					method.Invoke(obj, args);
				}
			}

			return true;
		}

		///Describes a member to be handled by the EventHandler.
		public class SubscribedMember{

			public object subscribedObject;
			public Action<object> subscribedFunction;
			public int invokePriority = 0;
			public bool unsubscribeWhenReceive;

			public SubscribedMember(object obj, int invokePriority, bool unsubscribeWhenReceive){

				this.subscribedObject = obj;
				this.invokePriority = invokePriority;
				this.unsubscribeWhenReceive = unsubscribeWhenReceive;
			}

			public SubscribedMember(Action<object> func, int invokePriority, bool unsubscribeWhenReceive){

				this.subscribedFunction = func;
				this.invokePriority = invokePriority;
				this.unsubscribeWhenReceive = unsubscribeWhenReceive;
			}
		}

	}
}
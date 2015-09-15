using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_4_6 || UNITY_5
using UnityEngine.EventSystems;
#endif


namespace ParadoxNotion.Services{

	///Automaticaly added to a gameobject when needed.
	///Handles forwarding Unity event messages to listeners that need them as well as Custom event forwarding.
	public partial class MessageRouter : MonoBehaviour

		#if UNITY_4_6 || UNITY_5
			, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
			IDragHandler, IScrollHandler, IUpdateSelectedHandler, ISelectHandler, IDeselectHandler, IMoveHandler, ISubmitHandler
		#endif
	{

		private readonly Dictionary<string, List<object>> listeners = new Dictionary<string, List<object>>();


		#if UNITY_4_6 || UNITY_5

		public void OnPointerEnter(PointerEventData eventData){
			Send("OnPointerEnter", eventData);
		}

		public void OnPointerExit(PointerEventData eventData){
			Send("OnPointerExit", eventData);
		}

		public void OnPointerDown(PointerEventData eventData){
			Send("OnPointerDown", eventData);
		}

		public void OnPointerUp(PointerEventData eventData){
			Send("OnPointerUp", eventData);
		}

		public void OnPointerClick(PointerEventData eventData){
			Send("OnPointerClick", eventData);
		}

		public void OnDrag(PointerEventData eventData){
			Send("OnDrag", eventData);
		}

		public void OnDrop(BaseEventData eventData){
			Send("OnDrop", eventData);
		}

		public void OnScroll(PointerEventData eventData){
			Send("OnScroll", eventData);
		}

		public void OnUpdateSelected(BaseEventData eventData){
			Send("OnUpdateSelected", eventData);
		}

		public void OnSelect(BaseEventData eventData){
			Send("OnSelect", eventData);
		}

		public void OnDeselect(BaseEventData eventData){
			Send("OnDeselect", eventData);
		}

		public void OnMove(AxisEventData eventData){
			Send("OnMove", eventData);
		}

		public void OnSubmit(BaseEventData eventData){
			Send("OnSubmit", eventData);
		}

		#endif



		void OnAnimatorIK(int layerIndex){
			Send("OnAnimatorIK", layerIndex);
		}

		void OnBecameInvisible(){
			Send("OnBecameInvisible", null);
		}

		void OnBecameVisible(){
			Send("OnBecameVisible", null);
		}

		void OnCollisionEnter(Collision collisionInfo){
			Send("OnCollisionEnter", collisionInfo);
		}

		void OnCollisionExit(Collision collisionInfo){
			Send("OnCollisionExit", collisionInfo);
		}

		void OnCollisionStay(Collision collisionInfo){
			Send("OnCollisionStay", collisionInfo);
		}

		void OnCollisionEnter2D(Collision2D collisionInfo){
			Send("OnCollisionEnter2D", collisionInfo);
		}

		void OnCollisionExit2D(Collision2D collisionInfo){
			Send("OnCollisionExit2D", collisionInfo);
		}

		void OnCollisionStay2D(Collision2D collisionInfo){
			Send("OnCollisionStay2D", collisionInfo);
		}

		void OnTriggerEnter(Collider other){
			Send("OnTriggerEnter", other);
		}

		void OnTriggerExit(Collider other){
			Send("OnTriggerExit", other);
		}

		void OnTriggerStay(Collider other){
			Send("OnTriggerStay", other);
		}

		void OnTriggerEnter2D(Collider2D other){
			Send("OnTriggerEnter2D", other);
		}

		void OnTriggerExit2D(Collider2D other){
			Send("OnTriggerExit2D", other);
		}

		void OnTriggerStay2D(Collider2D other){
			Send("OnTriggerStay2D", other);
		}

		void OnMouseDown(){
			Send("OnMouseDown", null);
		}

		void OnMouseDrag(){
			Send("OnMouseDrag", null);
		}

		void OnMouseEnter(){
			Send("OnMouseEnter", null);
		}

		void OnMouseExit(){
			Send("OnMouseExit", null);
		}

		void OnMouseOver(){
			Send("OnMouseOver", null);
		}

		void OnMouseUp(){
			Send("OnMouseUp", null);
		}

		//This is used for internal local events
		public void OnCustomEvent(EventData eventData){
			Debug.Log(string.Format("<b>Event Send to ({0}): </b> '{1}'", this.name, eventData.name));
			Send("OnCustomEvent", eventData);
		}


		///Add a listener
		public void Listen(object target, string toMessage){

			if (!listeners.ContainsKey(toMessage))
				listeners[toMessage] = new List<object>();
			if (!listeners[toMessage].Contains(target))
				listeners[toMessage].Add(target);
		}

		///Remove a listener completely
		public void Forget(object target){

			if (target == null)
				return;

			foreach (var message in listeners.Keys){
				foreach (var o in listeners[message].ToArray()){
					if (o == target){
						listeners[message].Remove(target);
					}
				}
			}
		}

		///Remove a listener from a specified message
		public void Forget(object target, string forgetMessage){

			if (target == null || !listeners.ContainsKey(forgetMessage))
				return;

			foreach (var o in listeners[forgetMessage].ToArray()){
				if (o == target){
					listeners[forgetMessage].Remove(target);
				}
			}
		}

		///Call the functions
		public void Send(string message, object arg){

			if (!listeners.ContainsKey(message))
				return;

			for (var i = 0; i < listeners[message].Count; i++){
				var target = listeners[message][i];
				if (target == null){
					continue;
				}

				var method = target.GetType().RTGetMethod(message, true);
				if (method == null){
					continue;
				}

				var args = method.GetParameters().Length == 1? new object[]{arg} : null;
				if (method.ReturnType == typeof(IEnumerator)){
					MonoManager.current.StartCoroutine( (IEnumerator)method.Invoke(target, args) );
				} else {
					method.Invoke(target, args);
				}				
			}
		}
	}
}
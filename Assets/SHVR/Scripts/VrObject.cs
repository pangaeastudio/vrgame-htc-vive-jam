using UnityEngine;
using System.Collections;

public class VrObject : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void OnTriggerStay(Collider other) {

		// get the controller position

		//Debug.Log ("is this working?");
		if (other.transform.tag == "Controller") {

			GetComponent<Renderer>().material.color = Color.yellow;

			if (other.GetComponent<BaseController>().isPressed()) {

				transform.SetParent(other.transform);
				Debug.Log ("grabbed");
			}
			else {
				transform.SetParent(null);
			}
		}
	}

	public void OnTriggerExit(Collider other) {
		
		// get the controller position
		
		if (other.transform.tag == "Controller") {
			
			Debug.Log ("inside");
			
			GetComponent<Renderer>().material.color = Color.white;
			
		}
		
	}}

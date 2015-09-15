using UnityEngine;
using System.Collections;
using Valve.VR;

[RequireComponent(typeof(SteamVR_TrackedObject))]
public class BaseController : MonoBehaviour {

	SteamVR_TrackedObject trackedObj;
	public GameObject handObjPrefab;

	private GameObject handObj;

	void Awake()
	{
		//trackedObj = GetComponent<SteamVR_TrackedObject>();
		//handObj = Instantiate(handObjPrefab) as GameObject;
		//handObj.transform.SetParent (this.transform);
	}

	// Use this for initialization
	void Start () {
	
	}

	void Update() {

		//var currentRotation = getRotationQuaternion ();
		//handObj.transform.rotation = currentRotation;

	}

	public Vector3 getControllerPosition() {
		var device = SteamVR_Controller.Input((int)trackedObj.index);
		return device.transform.pos;
	}

	public bool isTriggered() {
		var device = SteamVR_Controller.Input((int)trackedObj.index);
		return device.GetTouchDown (SteamVR_Controller.ButtonMask.Trigger);
	}

	public bool isPressed() {
		var device = SteamVR_Controller.Input((int)trackedObj.index);
		return device.GetPress (SteamVR_Controller.ButtonMask.Trigger);
	}

	public Vector3 getVelocity() {
		var device = SteamVR_Controller.Input((int)trackedObj.index);
		return device.velocity;
	}

	public Vector3 getAngularVelocity() {
		var device = SteamVR_Controller.Input((int)trackedObj.index);
		return device.angularVelocity;
	}

	public Vector3 getRotation() {
		var device = SteamVR_Controller.Input((int)trackedObj.index);
		return device.transform.rot.eulerAngles;
	}
	public Quaternion getRotationQuaternion() {
		var device = SteamVR_Controller.Input((int)trackedObj.index);
		return device.transform.rot;
	}
	
}

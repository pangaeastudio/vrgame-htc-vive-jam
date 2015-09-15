using System;
using System.Collections.Generic;
using NodeCanvas.Framework.Internal;
using ParadoxNotion.Serialization;
using UnityEngine;


namespace NodeCanvas.Framework{

	/// <summary>
	/// A Blackboard component to hold variables
	/// </summary>
    public class Blackboard : MonoBehaviour, ISerializationCallbackReceiver, IBlackboard{

		[SerializeField]
		private string _serializedBlackboard;
		[SerializeField]
		private List<UnityEngine.Object> _objectReferences;

		[NonSerialized]
		private BlackboardSource _blackboard = new BlackboardSource();

		//serialize blackboard variables to json
		public void OnBeforeSerialize(){

			if (Application.isPlaying)	return;

			_objectReferences = new List<UnityEngine.Object>();
			_serializedBlackboard = JSON.Serialize(typeof(BlackboardSource), _blackboard, false, _objectReferences);
		}


		//deserialize blackboard variables from json
		public void OnAfterDeserialize(){
			_blackboard = JSON.Deserialize<BlackboardSource>(_serializedBlackboard, _objectReferences);
			if (_blackboard == null) _blackboard = new BlackboardSource();
		}


		void Awake(){
			//Call to bind the variables with respected properties on the target game object
			_blackboard.InitializePropertiesBinding(propertiesBindTarget, false);
		}

		new public string name{
			get {return string.IsNullOrEmpty(_blackboard.name)? gameObject.name + "_BB" : _blackboard.name;}
			set
			{
				if (string.IsNullOrEmpty(value))
					value = gameObject.name + "_BB";
				_blackboard.name = value;
			}
		}

		///An indexer to access variables on the blackboard. It's recomended to use GetValue<T> instead
		public object this[string varName]{
			get { return _blackboard[varName]; }
			set { SetValue(varName, value); }
		}

		///The raw variables dictionary. It's highly recomended to use the methods available to access it though
		public Dictionary<string, Variable> variables{
			get {return _blackboard.variables;}
			set {_blackboard.variables = value;}
		}

		///The GameObject target to do variable/property binding
		public GameObject propertiesBindTarget{
			get {return gameObject;}
		}

		///Add a new variable of name and type
		public Variable AddVariable(string name, Type type){
			return _blackboard.AddVariable(name, type);
		}

		///Get a Variable of name and optionaly type
		public Variable GetVariable(string name, Type ofType = null){
			return _blackboard.GetVariable(name, ofType);
		}

		//Generic version of get variable
		public Variable GetVariable<T>(string name){
			return GetVariable(name, typeof(T));
		}

		///Get the variable value of name
		public T GetValue<T>(string name){
			return _blackboard.GetValue<T>(name);
		}

		///Set the variable value of name
		public Variable SetValue(string name, object value){
			return _blackboard.SetValue(name, value);
		}

		///Get all variable names
		public string[] GetVariableNames(){
			return _blackboard.GetVariableNames();
		}

		///Get all variable names of type
		public string[] GetVariableNames(Type ofType){
			return _blackboard.GetVariableNames(ofType);
		}

		////////////////////
		//SAVING & LOADING//
		////////////////////

		///Saved the blackboard with the blackboard name as saveKey.
		public string Save(){ return Save(this.name); }
		///Saves the Blackboard in PlayerPrefs in the provided saveKey. You can use this for a Save system
		public string Save(string saveKey){

			var json = JSON.Serialize(typeof(BlackboardSource), _blackboard, false, _objectReferences);
			PlayerPrefs.SetString(saveKey, json);
			return json;
		}

		///Loads a blackboard with this blackboard name as saveKey.
		public bool Load(){	return Load(this.name); }
		///Loads back the Blackboard from PlayerPrefs of the provided saveKey. You can use this for a Save system
		public bool Load(string saveKey){

			var dataString = PlayerPrefs.GetString(saveKey);
			if (string.IsNullOrEmpty(dataString)){
				Debug.Log("No data to load");
				return false;
			}

			_blackboard = JSON.Deserialize<BlackboardSource>(dataString, _objectReferences);
			_blackboard.InitializePropertiesBinding(propertiesBindTarget, true);
			return true;
		}
	}
}
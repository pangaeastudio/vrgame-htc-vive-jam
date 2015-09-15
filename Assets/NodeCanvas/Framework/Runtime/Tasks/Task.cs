using System;
using System.Collections;
using System.Collections.Generic;
using NodeCanvas.Framework.Internal;
using ParadoxNotion;
using ParadoxNotion.Design;
using ParadoxNotion.Serialization;
using ParadoxNotion.Serialization.FullSerializer;
using ParadoxNotion.Services;
using UnityEngine;


namespace NodeCanvas.Framework{

	///The base class for all Actions and Conditions. You dont actually use or derive this class. Instead derive from ActionTask and ConditionTask
	
	#if UNITY_EDITOR //handles missing Tasks
	[fsObject(Processor = typeof(fsTaskProcessor))]
	#endif
    
    [Serializable]
	abstract public partial class Task {

		///Designates what type of component to get and set the agent from the agent itself on initialization.
		///That component type is also considered required for correct task init.
		[AttributeUsage(AttributeTargets.Class)]
		protected class AgentTypeAttribute : Attribute{
			public Type type;
			public AgentTypeAttribute(Type type){
				this.type = type;
			}
		}

		///Designates that the task requires Unity eventMessages to be forwarded from the agent and to this task
		[AttributeUsage(AttributeTargets.Class)]
		protected class EventReceiverAttribute : Attribute{
			public string[] eventMessages;
			public EventReceiverAttribute(params string[] args){
				this.eventMessages = args;
			}
		}

		///If the field is deriving Component then it will be retrieved from the agent. The field is also considered Required for correct initialization
		[AttributeUsage(AttributeTargets.Field)]
		protected class GetFromAgentAttribute : Attribute{}

		///A special BBParameter for the task agent
		[Serializable]
		public class TaskAgent : BBParameter<UnityEngine.Object>{

			new public UnityEngine.Object value{
				get
				{
					if (useBlackboard){
						var o = base.value;
						if (o is GameObject)
							return (o as GameObject).transform;
						if (o is Component)
							return (Component)o;
						return null;
					}
					return _value as Component;
				}
				set {_value = value;} //the selected blackboard variable is NEVER set through the agent. Instead we set the local (inherited) variable
			}

			protected override object objectValue{
				get {return value;}
				set {this.value = (UnityEngine.Object)value;}
			}
		}


		

		[SerializeField]
		private bool _isActive = true;
		[SerializeField]
		private TaskAgent overrideAgent = null;
		
		[NonSerialized]
		private IBlackboard _blackboard;
		[NonSerialized]
		private ITaskSystem _ownerSystem;

		//the current/last agent used
		[NonSerialized]
		private Component current;
		
		//info
		[NonSerialized]
		private readonly Type _agentType;
		[NonSerialized]
		private readonly string _taskName;
		[NonSerialized]
		private readonly string _taskDescription;
		//


	    public Task(){
			
			//[AgentType]
			var typeAtt = this.GetType().RTGetAttribute<AgentTypeAttribute>(true);
			_agentType = typeAtt != null && (typeof(Component).RTIsAssignableFrom(typeAtt.type) || typeAtt.type.RTIsInterface() )? typeAtt.type : null;

			//[Name]
			var nameAtt = this.GetType().RTGetAttribute<NameAttribute>(false);
			_taskName = nameAtt != null? nameAtt.name : GetType().FriendlyName().SplitCamelCase();

			//[Description]
			var descAtt = this.GetType().RTGetAttribute<DescriptionAttribute>(true);
			_taskDescription = descAtt != null? descAtt.description : string.Empty;
		}


		///Create a new Task of type assigned to the target ITaskSystem
		public static Task Create(Type type, ITaskSystem newOwnerSystem){
			
			var newTask = (Task)Activator.CreateInstance(type);

			#if UNITY_EDITOR
			if (!Application.isPlaying){
				UnityEditor.Undo.RecordObject(newOwnerSystem.baseObject, "New Task");
			}
			#endif

			newTask.SetOwnerSystem(newOwnerSystem);
			newTask.OnValidate(newOwnerSystem);
			return newTask;
		}

		//Duplicate the task for the target ITaskSystem
		virtual public Task Duplicate(ITaskSystem newOwnerSystem){

			//Deep clone
			var newTask = JSON.Deserialize<Task>(  JSON.Serialize(typeof(Task), this)  );

			#if UNITY_EDITOR
			if (!Application.isPlaying){
				UnityEditor.Undo.RecordObject(newOwnerSystem.baseObject, "Duplicate Task");
			}
			#endif

			newTask.SetOwnerSystem(newOwnerSystem);
			newTask.OnValidate(newOwnerSystem);
			return newTask;
		}

		///Called when the task is created, duplicated or otherwise needs validation.
		///This is not the editor only Unity OnValidate call!
		virtual protected void OnValidate(ITaskSystem newOwnerSystem){}

		//Following are special so they are declared first
		//...
		///Sets the system in which this task lives in and initialize BBVariables. Called on Initialization of the system.
		public void SetOwnerSystem(ITaskSystem newOwnerSystem){

			if (newOwnerSystem == null){
				Debug.LogError("ITaskSystem set in task is null");
				return;
			}

			ownerSystem = newOwnerSystem;
            UpdateBBFields(newOwnerSystem.blackboard);
		}


		//Set the target blackboard for all BBVariables found in this instance. This is done every time the blackboard of the Task is set to a new value
		//as well as when the owner system is set by SetOwnerSystem
		void UpdateBBFields(IBlackboard bb){
			BBParameter.SetBBFields(this, bb);
			if (overrideAgent != null)
				overrideAgent.bb = bb; //explicitely set TaskAgent BBParameter since it's private
		}

		///The system this task belongs to from which defaults are taken from.
		public ITaskSystem ownerSystem{
			get {return _ownerSystem;}
			private set{ _ownerSystem = value; }
		}

		///The owner system's assigned agent
		private Component ownerAgent{
			get	{return ownerSystem != null? ownerSystem.agent : null;}
		}

		///The owner system's assigned blackboard
		private IBlackboard ownerBlackboard{
			get	{return ownerSystem != null? ownerSystem.blackboard : null;}
		}

		///The time in seconds that the owner system is running
		protected float ownerElapsedTime{
			get {return ownerSystem != null? ownerSystem.elapsedTime : 0;}
		}
		///

		//Is the task obsolete? (marked by [Obsolete])
		public ObsoleteAttribute isObsolete{
			get	{ return this.GetType().RTGetAttribute<ObsoleteAttribute>(true); }
		}

		///Is the Task active?
		public bool isActive{
			get {return _isActive;}
			set {_isActive = value;}
		}

		///The friendly task name. This can be overriden with the [Name] attribute
		public string name{
			get {return _taskName;}
		}

		///The help description of the task if it has any through [Description] attribute
		public string description{
			get {return _taskDescription;}
		}

		///The type that the agent will be set to by getting component from itself on task initialize. Defined with [AgentType] attribute or by using the generic versions of Action and Condition Tasks.
		///You can omit this to keep the agent propagated as is or if there is no need for a specific type.
		virtual public Type agentType{
			get {return _agentType;}
		}

		///A short summary of what the task will finaly do.
		public string summaryInfo{
			get
			{
				if (this is ActionTask)
					return (agentIsOverride? "* " : "") + info;
				if (this is ConditionTask)
					return (agentIsOverride? "* " : "") + ( (this as ConditionTask).invert? "If <b>!</b> ":"If ") + info;
				return info;
			}
		}

		///Override this and return the information of the task summary
		virtual protected string info{
			get {return name;}
		}

		///Helper summary info to display final agent string within task info if needed
		public string agentInfo{
			get
			{
				if (overrideAgent == null)
					return "<b>owner</b>";
				return overrideAgent != null? overrideAgent.ToString() : "*NULL*";
			}
		}

		///Is the agent overriden or the default taken from owner system will be used?
		public bool agentIsOverride{

			#if UNITY_METRO //undetermined issue in metro fix
			get {return overrideAgent != null && !overrideAgent.isNull}
			#else
			get {return overrideAgent != null;}
			#endif

			private set
			{
				if (value == false && overrideAgent != null){
					overrideAgent = null;
				}

				if (value == true && overrideAgent == null){
					overrideAgent = new TaskAgent();
					overrideAgent.bb = blackboard;					
				}
			}
		}

		///The name of the blackboard variable selected if the agent is overriden and set to a blackboard variable
		public string overrideAgentParameterName{
			get{return overrideAgent != null? overrideAgent.name : null;}
		}

		///The current or last executive agent of this task
		protected Component agent{
			get
			{
				if (current != null)
					return current;
				return agentIsOverride? (Component)overrideAgent.value : ownerAgent != null && agentType != null? ownerAgent.GetComponent(agentType) : null;
			}
		}

		///The current or last blackboard to be used by this task
		protected IBlackboard blackboard{
			get
			{
				if (_blackboard == null){
					_blackboard = ownerBlackboard;
					UpdateBBFields(_blackboard);
				}

				return _blackboard;
			}
			private set
			{
				if (_blackboard != value){
					_blackboard = value;
					UpdateBBFields(_blackboard);
				}
			}
		}

		//////////

		//Tasks can start coroutine through MonoManager
		protected Coroutine StartCoroutine(IEnumerator routine){
			return MonoManager.current.StartCoroutine(routine);
		}

		///Sends an event through the owner system to handle (same as calling ownerSystem.SendEvent)
		protected void SendEvent(EventData eventData){
			if (ownerSystem != null)
				ownerSystem.SendEvent(eventData);
		}

		///Override in your own Tasks. This is called after a NEW agent is set, after initialization and before execution
		///Return null if everything is ok, or a string with the error if not.
		virtual protected string OnInit(){ return null; }

		//Actions and Conditions call this before execution. Returns if the task was sucessfully initialized as well
		protected bool Set(Component newAgent, IBlackboard newBB){

			//set blackboard with normal setter first
			blackboard = newBB;

			//Consider initialized if task dont require a specific agent type
//			if (agentType == null)
//				return true;

			if (agentIsOverride){
				try
				{
					if (current.gameObject == ((Component)overrideAgent.value).gameObject )
						return true;
					return Initialize( (Component)overrideAgent.value, agentType );	
				}
				catch { return Initialize( (Component)overrideAgent.value, agentType ); }
			}

			try
			{
				if (current.gameObject == newAgent.gameObject)
					return true;
				return Initialize(newAgent, agentType);
			}
			catch { return Initialize(newAgent, agentType); }
		}


		//Initialize whenever agent is set to a new value
		bool Initialize(Component newAgent, Type newType){

			//Unsubscribe from previous agent
			UnsubscribeFromAgentEvents(agent);

			//"Transform" the agent to the agentType
			newAgent = (newType != null && newType != typeof(Component) && newAgent != null)? newAgent.GetComponent(newType) : newAgent;

			//Set as current even if null
			current = newAgent;

			//error if it's null but an agentType is required
			if (newAgent == null && agentType != null)
				return Error("Failed to change Agent to requested type '" + agentType + "', for Task '" + name + "' or new Agent is NULL. Does the Agent has the requested Component?");

			//Subscribe to events for the new agent
			var msgAttribute = this.GetType().RTGetAttribute<EventReceiverAttribute>(true);
			if (msgAttribute != null)
				SubscribeToAgentEvents(newAgent, msgAttribute.eventMessages);

			//Use the attributes
			if (InitializeAttributes(newAgent) == false)
				return false;

			//let user make further adjustments and inform us if there was an error
			var errorString = OnInit();
			if (errorString != null)
				return Error(string.Format("'{0}' at Task '{1}'", errorString, this.name));
			
			return true;
		}

		bool InitializeAttributes(Component newAgent){

			//Usage of [RequiredField] and [GetFromAgent] attributes
			foreach (var field in this.GetType().RTGetFields()){
				
				var value = field.GetValue(this);
				var requiredAttribute = field.RTGetAttribute<RequiredFieldAttribute>(true);
				if (requiredAttribute != null){

					if (value == null || value.Equals(null))
						return Error(string.Format("A required field for Task '{0}' is not set! Field '{1}'", name, field.Name));

					if (field.FieldType == typeof(string) && string.IsNullOrEmpty((string)value) )
						return Error(string.Format("A required string field for Task '{0}' is not set! Field '{1}'", name, field.Name));

					if (typeof(BBParameter).RTIsAssignableFrom(field.FieldType) && (value as BBParameter).isNull)
						return Error(string.Format("A required BBParameter field value for Task '{0}' is not set! Field '{1}'", name, field.Name));
				}

				var getterAttribute = field.RTGetAttribute<GetFromAgentAttribute>(true);
				if (getterAttribute != null){

					if (typeof(Component).RTIsAssignableFrom(field.FieldType)){

						field.SetValue(this, newAgent.GetComponent(field.FieldType));
						if ( (field.GetValue(this) as UnityEngine.Object) == null)
							return Error(string.Format("GetFromAgent Attribute failed to get the required Component of type '{0}' from '{1}'. Does it exist?", field.FieldType.Name, agent.gameObject.name));
					
					} else {
						return Error(string.Format("You've used a GetFromAgent Attribute on a field {0} whos type does not derive Component on Task {1}", field.Name, this.name));
					}
				}
			}

			return true;
		}

		//Utility function to log and return errors above (for runtime)
		bool Error(string error){
			Debug.LogError(string.Format("<b>Task Error:</b> {0} (Task Disabled)", error) );
			return false;
		}

		///Subscribe to events of the target agent. This is also handled by the usage of [EventReceiver] attribute
		public void SubscribeToAgentEvents(Component targetAgent, params string[] eventNames){
			
			if (targetAgent == null)
				return;

			var agentUtils = targetAgent.GetComponent<MessageRouter>();
			if (agentUtils == null)
				agentUtils = targetAgent.gameObject.AddComponent<MessageRouter>();

			for (var i = 0; i < eventNames.Length; i++)
				agentUtils.Listen(this, eventNames[i]);
		}

		///Unsubscrive from events for the taget agent
		public void UnsubscribeFromAgentEvents(Component targetAgent){

			if (targetAgent == null)
				return;

			var agentUtils = targetAgent.GetComponent<MessageRouter>();
			if (agentUtils != null)
				agentUtils.Forget(this);
		}

		//Gather errors for user convernience (also used in editor)
		public string[] GetErrors(){

			var errors = new List<string>();

			if (agent == null && agentType != null)
				errors.Add("Null Agent");

			foreach (var field in this.GetType().RTGetFields()){
				
				if (isObsolete != null)
					errors.Add( (string.Format("The Task is obsolete '{0}': <b>'{1}'</b>", name, isObsolete.Message)) );

				if (field.RTGetAttribute<RequiredFieldAttribute>(true) != null){
					if (field.GetValue(this) == null || field.GetValue(this).Equals(null))
						errors.Add("Required field in task is null");
					if (field.FieldType == typeof(string) && string.IsNullOrEmpty( (string)field.GetValue(this) ))
						errors.Add("Required string field in task is empty");
					if (typeof(BBParameter).RTIsAssignableFrom(field.FieldType) && (field.GetValue(this) as BBParameter).isNull )
						errors.Add("Required BBParameter field in task resolves to null");
				}
			}
			return errors.Count > 0? errors.ToArray() : null;
		}

		sealed public override string ToString(){
			return string.Format("{0} {1}", agentInfo, summaryInfo);
		}

		virtual public void OnDrawGizmos(){}
		virtual public void OnDrawGizmosSelected(){}
	}
}
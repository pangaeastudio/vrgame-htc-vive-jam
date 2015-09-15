using System;
using System.Collections;
using ParadoxNotion;
using UnityEngine;


namespace NodeCanvas.Framework{

	///Marks the BBParameter possible to only pick values from a blackboard
	[AttributeUsage(AttributeTargets.Field)]
	public class BlackboardOnlyAttribute : Attribute{}

	///Very internal use, but when added on a non null field, the object will also be included when parsing the BBVariables (so be very careful with this)
	[AttributeUsage(AttributeTargets.Field)]
	public class IncludeParseVariablesAttribute : Attribute{}

	///Class for Parameter Variables that allow binding to a Blackboard variable or specifying a value directly.
	[Serializable]
	abstract public class BBParameter {

		[SerializeField]
		private string _name;

		[NonSerialized]
		private IBlackboard _bb;
		[NonSerialized]
		private Variable _varRef;


		//required
		public BBParameter(){}


		///Create and return an instance of a generic BBParameter<T> with type argument provided and set to read from the specified blackboard
		public static BBParameter CreateInstance(Type t, IBlackboard bb){
			if (t == null) return null;
			var newParamVariable = (BBParameter)Activator.CreateInstance( typeof(BBParameter<>).RTMakeGenericType(new Type[]{t}) );
			newParamVariable.bb = bb;
			return newParamVariable;
		}

		///Set the blackboard reference provided for all *public* BBParameter and List<BBParameter> fields on the target object provided.
		public static void SetBBFields(object o, IBlackboard bb){
			ParseObject(o, (bbParam)=>
			{
				bbParam.bb = bb;
				if (bb != null){
					bbParam.varRef = bb.GetVariable(bbParam.name);
				}
			});
		}

		///Parses an object to find BBVariables and execute the provided Action
		public static void ParseObject(object o, Action<BBParameter> Call){

			foreach (var field in o.GetType().RTGetFields()){

				if (field.RTGetAttribute<IncludeParseVariablesAttribute>(false) != null && field.GetValue(o) != null){
					ParseObject(field.GetValue(o), Call);
				}
				
				//single
				if (field.FieldType.RTIsSubclassOf(typeof(BBParameter))){
					if (field.GetValue(o) == null)
						field.SetValue(o, Activator.CreateInstance(field.FieldType));
					Call( (BBParameter)field.GetValue(o) );
				}

				//lists
				if (field.GetValue(o) != null && typeof(IList).RTIsAssignableFrom(field.FieldType) && !field.FieldType.RTIsArray() ){
                    if (typeof(BBParameter).RTIsAssignableFrom(field.FieldType.RTGetGenericArguments()[0])){
						foreach(BBParameter bbParam in (IList)field.GetValue(o)){
							Call(bbParam);
						}
					}
				}
			}			
		}

		//Determines and gets whether the name is a path to a global bb variable
		private Variable globalVarRef{
			get
			{
				if (name != null && name.Contains("/")){
					var bbName = name.Split('/')[0];
					var varName = name.Split('/')[1];
					var globalBB = GlobalBlackboard.allGlobals.Find(b => b.name == bbName);
					if (globalBB == null){
						return null;
					}
					var globalVar = globalBB.GetVariable( varName );
					if (globalVar == null){
						return null;
					}
					return globalVar;
				}
				return null;
			}
		}

		///The Variable object reference if any.One is set after a get or set as well as well when SetBBFields is called
		///Setting the varRef also binds this variable with that varRef
		protected Variable varRef{
			get {return _varRef;}
			set
			{
				//check for global override
				value = globalVarRef != null? globalVarRef : value;

				if (_varRef != value || value == null){
					_varRef = value;
					Bind(value);
				}
			}
		}

		///The blackboard to read/write from.
		public IBlackboard bb{
			get {return _bb;}
			set
			{
				if (_bb != value){
					_bb = value;
					varRef = value != null && !string.IsNullOrEmpty(name)? value.GetVariable(name, varType) : null;
				}
			}
		}

		///The name of the Variable to read/write from. Null if not, Empty if |NONE|.
		public string name{
			get
			{
				//check for global override
				if (_name == null || _name.Contains("/"))
					return _name;

				return varRef != null? varRef.name : _name;
			}
			set
			{
				if (_name != value){
					_name = value;
					if (value != null){
						useBlackboard = true;
						if (bb != null)
							varRef = bb.GetVariable(_name, varType);
					} else {
						varRef = null;
					}
				}
			}
		}

		///Should the variable read from a blackboard variable?
		public bool useBlackboard{
			get { return name != null; }
			set
			{
				if (value == false){
					name = null;
				}
				if (value == true && name == null){
					name = string.Empty;
				}
			}
		}


		///Has the user selected |NONE| in the dropdown?
		public bool isNone{
			get {return name == string.Empty;}
		}

		///Is the final value null?
		public bool isNull{
			get	{ return objectValue == null || objectValue.Equals(null); }
		}

		///The type of the Variable reference or null if there is no Variable referenced. The returned type is for most cases the same as 'VarType'
		public Type refType{
			get {return varRef != null? varRef.varType : null;}
		}

		///The value as object type when accessing from base class
		public object value{
			get {return objectValue;}
			set {objectValue = value;}
		}

		///The raw object value
		abstract protected object objectValue{get;set;}
		///The type of the value that this BBParameter holds
		abstract public Type varType{get;}
		///Bind the BBParameter to target. Null unbinds.
		abstract protected void Bind(Variable data);

		public override string ToString(){
			if (isNone)
				return "<b>NONE</b>";
			if (useBlackboard)
				return string.Format("<b>${0}</b>", name);
			if (isNull)
				return "<b>NULL</b>";
			if (objectValue is string)
				return string.Format("<b>\"{0}\"</b>", objectValue.ToString());
			if (objectValue is IList)
				return string.Format("<b>{0}</b>", varType.FriendlyName());
			if (objectValue is IDictionary)
				return string.Format("<b>{0}</b>", varType.FriendlyName());
			if (objectValue is UnityEngine.Object)
				return string.Format("<b>{0}</b>", (objectValue as UnityEngine.Object).name );
			return string.Format("<b>{0}</b>", objectValue.ToString() );
		}
	}


	///Use BBParameter to create a variable possible to parametrize from a blackboard variable
	[Serializable]
	public class BBParameter<T> : BBParameter{

	    public BBParameter() {}
        public BBParameter(T value) { _value = value; }

	    //delegates for Variable binding
		private Func<T> getter;
		private Action<T> setter;
		//

		[SerializeField]
		protected T _value;
		new public T value{
			get
			{
				if (getter != null)
					return getter();

				//Dynamic?
				if (name != null && bb != null){
					Bind( bb.GetVariable<T>(name) );
					if (getter != null)
						return getter();
				}

				return _value;
			}
			set
			{
				if (setter != null){
					setter(value);
					return;
				}
				
				if (isNone)
					return;

				//Dynamic?
				if (name != null && bb != null){
					//setting the varRef property also binds it
					varRef = bb.SetValue(name, value);
					return;
				}

				_value = value;
			}
		}
		
		protected override object objectValue{
			get {return value;}
			set {this.value = (T)value;}
		}
		
		public override Type varType{
			get {return typeof(T);}
		}

		///Binds the BBParameter to another Variable. Null unbinds
		protected override void Bind(Variable data){
			if (data == null){
				getter = null;
				setter = null;
				if (useBlackboard){
					_value = default(T);
				}
				return;
			}

			if (!typeof(T).RTIsAssignableFrom(data.varType) && !data.varType.RTIsAssignableFrom(typeof(T)) ){
				Debug.LogWarning(string.Format("<b>BBParameter</b>: Found Variable of name '{0}' and type '{1}' on Blackboard '{2}' is not of requested type '{3}'", name, data.varType.FriendlyName(), bb.name, typeof(T).FriendlyName() ));
				return;
			}

			BindSetter(data);
			BindGetter(data);
		}

		//Bind the Getter
		void BindGetter(Variable data){
			if (data is Variable<T>){
				getter = (data as Variable<T>).GetValue;
			} else if (typeof(T).RTIsAssignableFrom(data.varType)){
				getter = ()=> { return (T)data.value; };
			}
		}

		//Bind the Setter
		void BindSetter(Variable data){
			if (data is Variable<T>){
				setter = (data as Variable<T>).SetValue;
			} else if (data.varType.RTIsAssignableFrom(typeof(T))){
				setter = (T newValue)=> { data.value = newValue; };
			}
		}


	    public static implicit operator BBParameter<T>(T value) {
	        return new BBParameter<T>{value = value};
	    }
/*
	    public static implicit operator T(BBParameter<T> param) {
	        return param.value;
	    }
*/
	}
}
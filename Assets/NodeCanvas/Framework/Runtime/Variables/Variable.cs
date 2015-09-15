using System;
using System.Reflection;
using ParadoxNotion;
using UnityEngine;


namespace NodeCanvas.Framework{

	///Variables are mostly stored in a Blackboard and can optionaly bind to Properties of a Unity Component
	[Serializable]
	abstract public class Variable {

	    [SerializeField]
		private string _name;
		[SerializeField]
		private bool _nonEditable;
		public event Action<string, object> onValueChanged;

		///The name of the variable
		public string name{
			get {return _name;}
			set	{_name = value;}
		}

		///The value as object type when accessing from base class
		public object value{
			get {return objectValue;}
			set {objectValue = value;}
		}

		///Is the variable editable?
		public bool nonEditable{
			get {return _nonEditable;}
			set {_nonEditable = value;}
		}

		protected void OnValueChanged(string name, object value){
			if (onValueChanged != null)
				onValueChanged(name, value);
		}

		//required
		public Variable(){}

		///The path to the property this data is binded to. Null if none
		abstract public string propertyPath{get;set;}
		///The System.Object value of the contained variable
		abstract protected object objectValue{get;set;}
		///The Type this Variable holds
		abstract public Type varType{get;}
		///Returns whether or not the variable is property binded
		abstract public bool hasBinding{get;}
		///Used to bind variable to a property
		abstract public void BindProperty(PropertyInfo prop, GameObject target = null);
		///Used to un-bind variable from a property
		abstract public void UnBindProperty();
		///Called from Blackboard in Awake to Initialize the binding on specified game object
		abstract public void InitializePropertyBinding(GameObject go, bool callSetter = false);
	}

	///The actual Variable
	[Serializable]
	public class Variable<T> : Variable {

		[SerializeField]
		private T _value;
		[SerializeField]
		private string _propertyPath;

		new public event Action<string, T> onValueChanged;

		//required
		public Variable(){}

		//delegates for property binding
		private Func<T> getter;
		private Action<T> setter;
		//

		public override string propertyPath{
			get {return _propertyPath;}
			set {_propertyPath = value;}
		}

		public override bool hasBinding{
			get {return !string.IsNullOrEmpty(_propertyPath);}
		}

		protected override object objectValue{
			get {return value;}
			set {this.value = (T)value;}
		}

		public override Type varType{
			get {return typeof(T);}
		}

		///The value as correct type when accessing as this type
		new public T value{
			get	{ return getter != null? getter() : _value; }
			set
			{
				this._value = value;
				if (setter != null) setter(value);
				if (onValueChanged != null){
					onValueChanged(name, value);
					base.OnValueChanged(name, value);					
				}
			}
		}

		///Used for variable binding
		public T GetValue(){ return value; }
		///Used for variable binding
		public void SetValue(T newValue){ value = newValue; }

		
		public override void BindProperty(PropertyInfo prop, GameObject target = null){
			_propertyPath = string.Format("{0}.{1}", prop.RTReflectedType().Name, prop.Name);
			if (target != null)
				InitializePropertyBinding(target, false);
		}

		public override void UnBindProperty(){
			_propertyPath = null;
			getter = null;
			setter = null;
		}

		///Set the gameobject target for property binding.
		public override void InitializePropertyBinding(GameObject go, bool callSetter = false){
		    
            if ( !hasBinding || !Application.isPlaying )
                return;
		    
            getter = null;
		    setter = null;
		    var arr = _propertyPath.Split('.');
		    var comp = go.GetComponent( arr[0] );
		    if (comp == null){
		        Debug.LogWarning(string.Format("A Blackboard Variable '{0}' is due to bind to a Component type that is missing '{1}'. Binding ingored", name, arr[0]));
		        return;
		    }
		    var prop = comp.GetType().RTGetProperty(arr[1]);
		    if (prop == null){
		        Debug.LogWarning(string.Format("A Blackboard Variable '{0}' is due to bind to a property that does not exist in type '{1}'. Binding ignored", name, arr[0]));
		        return;
		    }

		    if (prop.CanRead){
		        var getMethod = prop.RTGetGetMethod();
		        if (getMethod != null){

		        	#if !UNITY_IOS
		            getter = getMethod.RTCreateDelegate<Func<T>>(comp);
		            #else
		            getter = ()=>{ return (T)getMethod.Invoke(comp, null); };
		            #endif

		        } else {
		            Debug.Log(string.Format("Binded Property '{0}' on type '{1}' get accessor is private. Getter binding ignored", prop.Name, comp.GetType().Name));
		        }
		    }

		    if (prop.CanWrite){
		        var setMethod = prop.RTGetSetMethod();
		        if (setMethod != null){
		            
		            #if !UNITY_IOS
		            setter = setMethod.RTCreateDelegate<Action<T>>(comp);
		            #else
		            setter = (o)=>{ setMethod.Invoke(comp, new object[]{o}); };
		            #endif

		            if (callSetter)
		                setter(_value);

		        } else {
		            Debug.Log(string.Format("Binded Property '{0}' on type '{1}' set accessor is private. Setter binding ignored", prop.Name, comp.GetType().Name));
		        }
		    }
		}
	}
}
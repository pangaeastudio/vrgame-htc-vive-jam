using System;
using System.Linq;
using System.Reflection;
using UnityEngine;


namespace ParadoxNotion.Serialization{

	///Serialized MethodInfo
	[Serializable]
	public class SerializedMethodInfo{
		
		[SerializeField]
		private string _baseInfo;
		[SerializeField]
		private string _paramsInfo;
		private MethodInfo _method;

		//required
		public SerializedMethodInfo(){}
		///Serialize a new MethodInfo
		public SerializedMethodInfo(MethodInfo method){
			_baseInfo = string.Format("{0}|{1}", method.RTReflectedType().FullName, method.Name);
			_paramsInfo = string.Join("|", method.GetParameters().Select(p => p.ParameterType.FullName).ToArray() );
		}

		///Deserialize and return target MethodInfo.
		public MethodInfo Get(){
			if (_method == null && !string.IsNullOrEmpty(_baseInfo)){
				var type = ReflectionTools.GetType(_baseInfo.Split('|')[0]);
				if (type == null){
					return null;
				}
				var name = _baseInfo.Split('|')[1];
				var paramTypeNames = string.IsNullOrEmpty(_paramsInfo)? null : _paramsInfo.Split('|');
				var parameters = paramTypeNames == null? new Type[]{} : paramTypeNames.Select(n => ReflectionTools.GetType(n)).ToArray();
				_method = type.RTGetMethod(name, parameters);
			}

			return _method;
		}

		///Returns the serialized method information.
		public string GetMethodString(){
			return string.Format("{0} ({1})", _baseInfo.Replace("|", "."), _paramsInfo.Replace("|", ", "));
		}
	}
}
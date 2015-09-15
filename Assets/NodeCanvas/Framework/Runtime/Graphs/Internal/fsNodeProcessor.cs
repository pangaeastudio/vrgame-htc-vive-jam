using System;
using System.Linq;
using ParadoxNotion;
using ParadoxNotion.Serialization.FullSerializer;


namespace NodeCanvas.Framework.Internal{

	///Handles missing Node serialization and recovery
	public class fsNodeProcessor : fsObjectProcessor {

		public override bool CanProcess(Type type){
			return typeof(Node).RTIsAssignableFrom(type);
		}

		public override void OnBeforeSerialize(Type storageType, object instance){}
		public override void OnAfterSerialize(Type storageType, object instance, ref fsData data){}

		public override void OnBeforeDeserialize(Type storageType, ref fsData data){
			
			if (data.IsNull)
				return;

			var json = data.AsDictionary;

			if (json.ContainsKey("$type")){

				var serializedType = ReflectionTools.GetType( json["$type"].AsString );

				//Handle missing serialized Node type
				if (serializedType == null){

					//inject the 'MissingNode' type and store recovery serialization state
					json["recoveryState"] = new fsData( data.ToString() );
					json["missingType"] = new fsData( json["$type"].AsString );
					json["$type"] = new fsData( typeof(MissingNode).FullName );
				}

				//Recover possible found serialized type
				if (serializedType == typeof(MissingNode)){

					//Does the missing type now exists? If so recover
					var missingType = ReflectionTools.GetType( json["missingType"].AsString );
					if (missingType != null){

						var recoveryState = json["recoveryState"].AsString;
						var recoverJson = fsJsonParser.Parse(recoveryState).AsDictionary;

						//merge the recover state *ON TOP* of the current state, thus merging only Declared recovered members
						json = json.Concat( recoverJson.Where( kvp => !json.ContainsKey(kvp.Key) ) ).ToDictionary( c => c.Key, c => c.Value );
						json["$type"] = new fsData( missingType.FullName );
						data = new fsData( json );
					}
				}
			}
		}

		public override void OnAfterDeserialize(Type storageType, object instance){}
	}
}
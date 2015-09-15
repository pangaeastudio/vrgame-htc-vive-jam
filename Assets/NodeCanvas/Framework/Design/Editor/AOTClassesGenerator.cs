#if UNITY_EDITOR

using System;
using System.IO;
using System.Text;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

using NodeCanvas.Framework;
using ParadoxNotion.Design;
using ParadoxNotion;

namespace NodeCanvas.Design{

	public static class AOTClassesGenerator {

		static readonly string directory = "Assets/NodeCanvas/";

		static List<Type> spoofTypes = new List<Type>{
			typeof(bool),
			typeof(float),
			typeof(int),
			typeof(string),
			typeof(Vector2),
			typeof(Vector3),
			typeof(Vector4),
			typeof(Quaternion),
			typeof(AnimationCurve),
			typeof(Keyframe),
			typeof(Bounds),
			typeof(Color),
			typeof(Rect),
		};

		[MenuItem("Window/NodeCanvas/Generate AOT Dummy Classes")]
		public static void Generate(){

			var targetTypes = EditorUtils.GetAssemblyTypes(typeof(Task));
			targetTypes.Add(typeof(Variable<>));
			targetTypes.Add(typeof(BBParameter<>));

			var sb = new StringBuilder();

			sb.AppendLine();
			sb.AppendLine("namespace NodeCanvas.Framework.Internal{");
			sb.AppendLine();
			sb.AppendLine("	//Auto generated classes for AOT support, where using undeclared generic classes with value types is limited. These are not actualy used but rather just declared for the compiler");
			sb.AppendLine("	class AOTDummy{");
			sb.AppendLine();

			
			foreach(var t in targetTypes ){
				if (!t.IsAbstract && t.IsGenericTypeDefinition && t.GetGenericArguments().Length == 1){
					
					foreach (var spoofType in spoofTypes.Where(st => st.IsValueType)){
						
						var constrains = t.GetGenericArguments()[0].GetGenericParameterConstraints();
						if ( constrains.Length == 0 || constrains[0].IsAssignableFrom(spoofType) ){
							sb.AppendLine(string.Format("		class {0} : {1}.{2}",
								t.FriendlyName().Replace("<T>", "_" + spoofType.FriendlyName()),
								t.Namespace,
								t.FriendlyName().Replace("<T>", "<" + spoofType.FullName + ">")  ) + "{}");
						}
					}

					sb.AppendLine();
				}
			}




			sb.AppendLine("		void BBMethodsSpoof(){");
			sb.AppendLine("			var bb = new BlackboardSource();");
			foreach (var t in spoofTypes.Where(st => st.IsValueType)){
				sb.AppendLine(string.Format("			bb.GetVariable<{0}>(\"\");", t.FullName));
				sb.AppendLine(string.Format("			bb.GetValue<{0}>(\"\");", t.FullName));
			}
			sb.AppendLine("		}");




			sb.AppendLine("	}");
			sb.AppendLine("}");

			File.WriteAllText(directory + "AOTDummy.cs", sb.ToString());
			AssetDatabase.Refresh();
		}
	}
}

#endif
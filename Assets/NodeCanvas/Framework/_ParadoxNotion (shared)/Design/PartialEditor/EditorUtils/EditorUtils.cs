
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ParadoxNotion;
using UnityObject = UnityEngine.Object;

namespace ParadoxNotion.Design{

	///Have some commonly stuff used across most inspectors and helper functions. Keep outside of Editor folder since many runtime classes use this in #if UNITY_EDITOR
	///This is a partial class. Different implementation provide different tools, so that everything is referenced from within one class.
	public static partial class EditorUtils{

		readonly public static Texture2D playIcon    = EditorGUIUtility.FindTexture("d_PlayButton");
		readonly public static Texture2D pauseIcon   = EditorGUIUtility.FindTexture("d_PauseButton");
		readonly public static Texture2D stepIcon    = EditorGUIUtility.FindTexture("d_StepButton");
		readonly public static Texture2D viewIcon    = EditorGUIUtility.FindTexture("d_ViewToolOrbit On");
		readonly public static Texture2D csIcon      = EditorGUIUtility.FindTexture("cs Script Icon");
		readonly public static Texture2D jsIcon      = EditorGUIUtility.FindTexture("Js Script Icon");
		readonly public static Texture2D tagIcon     = EditorGUIUtility.FindTexture("d_FilterByLabel");
		readonly public static Texture2D searchIcon  = EditorGUIUtility.FindTexture("Search Icon");
		readonly public static Texture2D warningIcon = EditorGUIUtility.FindTexture("d_console.warnicon.sml");
		readonly public static Texture2D redCircle   = EditorGUIUtility.FindTexture("d_winbtn_mac_close");
		readonly public static Texture2D folderIcon  = EditorGUIUtility.FindTexture("Folder Icon");

		readonly public static Color lightOrange = new Color(1, 0.9f, 0.4f);
		readonly public static Color lightBlue   = new Color(0.8f,0.8f,1);
		readonly public static Color lightRed    = new Color(1,0.5f,0.5f, 0.8f);


		//For gathering script/type meta-information
		public class ScriptInfo{

			public Type type;
			public string name;
			public string category;
			public string iconName;
			public string description;

			public Texture icon{
				get
				{
			    	if ( typeof(UnityEngine.Object).IsAssignableFrom(type))
			    		return EditorGUIUtility.ObjectContent(null, type).image;

					if (!string.IsNullOrEmpty(iconName)){
						var _icon = (Texture)EditorGUIUtility.FindTexture(iconName);
						if (_icon == null)
							_icon = (Texture)Resources.Load(iconName);
						return _icon;
					}

					return null;
				}
			}

			public ScriptInfo(Type type, string name, string category){
				this.type = type;
				this.name = name;
				this.category = category;
				var iconAtt = type.RTGetAttribute<IconAttribute>(true);
				iconName = iconAtt != null? iconAtt.iconName : iconName;
				var descAtt = type.RTGetAttribute<DescriptionAttribute>(true);
				description = descAtt != null? descAtt.description : description;
			}
		}

		///Get a list of ScriptInfos of the baseType excluding: the base type, abstract classes, Obsolete classes and those with the DoNotList attribute, from within the project categorized as a list of ScriptInfo
		public static List<ScriptInfo> GetScriptInfosOfType(Type baseType){

			var infos = new List<ScriptInfo>();

			foreach (var subType in GetAssemblyTypes(baseType)){
				
				if (subType.GetCustomAttributes(typeof(DoNotListAttribute), false).FirstOrDefault() == null && subType.GetCustomAttributes(typeof(ObsoleteAttribute), false).FirstOrDefault() == null ){

					if (subType.IsAbstract)
						continue;

					var scriptName = subType.FriendlyName().SplitCamelCase();
					var scriptCategory = string.Empty;

					var nameAttribute = subType.GetCustomAttributes(typeof(NameAttribute), false).FirstOrDefault() as NameAttribute;
					if (nameAttribute != null)
						scriptName = nameAttribute.name;

					var categoryAttribute = subType.GetCustomAttributes(typeof(CategoryAttribute), true).FirstOrDefault() as CategoryAttribute;
					if (categoryAttribute != null)
						scriptCategory = categoryAttribute.category;

					//show the generic types based on constrains and prefered types list
					if (subType.IsGenericTypeDefinition && subType.GetGenericArguments().Length == 1){
						var arg1 = subType.GetGenericArguments()[0];
						var constrains = arg1.GetGenericParameterConstraints();
						var constrainType = constrains.Length == 0? typeof(object) : constrains[0];
						foreach(var t in UserTypePrefs.GetPreferedTypesList( constrainType, false )){
							var genericType = subType.MakeGenericType(new System.Type[]{t});
							var finalCategoryPath = (string.IsNullOrEmpty(scriptCategory)? "" : (scriptCategory + "/") ) + scriptName;
							finalCategoryPath = finalCategoryPath.Replace("<T>", " (T)");
							finalCategoryPath += "/" + (string.IsNullOrEmpty(t.Namespace)? "No Namespace" : t.Namespace.Replace(".","/") ) ;
							var finalName = scriptName.Replace("<T>", string.Format(" ({0})", t.FriendlyName() ) );
							infos.Add( new ScriptInfo(genericType, finalName, finalCategoryPath) );
						}
						continue;
					}
					//

					infos.Add(new ScriptInfo(subType, scriptName, scriptCategory));
				}
			}

			infos = infos.OrderBy(script => script.name).ToList();
			infos = infos.OrderBy(script => script.category).ToList();

			return infos;
		}


		//Get all base derived types in the current loaded assemplies, excluding the base type itself
		private static Dictionary<Type, List<Type>> loadedAssemblyTypes;
		public static List<Type> GetAssemblyTypes(Type baseType){

			if (loadedAssemblyTypes != null && loadedAssemblyTypes.ContainsKey(baseType))
				return loadedAssemblyTypes[baseType];

			if (loadedAssemblyTypes == null)
				loadedAssemblyTypes = new Dictionary<Type, List<Type>>();

			if (!loadedAssemblyTypes.ContainsKey(baseType))
				loadedAssemblyTypes[baseType] = new List<Type>();

			foreach (var ass in System.AppDomain.CurrentDomain.GetAssemblies().Where(ass => !ass.GetName().Name.Contains("Editor"))) {
			    try
			    {
			        foreach (var t in ass.GetExportedTypes().Where(t => t.IsSubclassOf(baseType))) {
			            loadedAssemblyTypes[baseType].Add(t);
			        }
			    }
			    catch
			    {
			        Debug.Log(ass.FullName + " will be excluded");
			        continue;
			    }
			}				
			
			loadedAssemblyTypes[baseType] = loadedAssemblyTypes[baseType].OrderBy(t => t.FriendlyName()).ToList();
			loadedAssemblyTypes[baseType] = loadedAssemblyTypes[baseType].OrderBy(t => t.Namespace).ToList();
			return loadedAssemblyTypes[baseType];
		}


		//Get a list of methods that extend the provided type
		public static MethodInfo[] GetExtensionMethods(this Type type){

			var methods = new List<MethodInfo>();
			foreach (var t in GetAssemblyTypes(typeof(object))){

				if (!t.IsSealed || t.IsGenericType || !t.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
					continue;

				foreach (var m in t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)){

					if (!m.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
						continue;

					if (m.GetParameters()[0].ParameterType == type)
						methods.Add(m);
				}
			}

			return methods.ToArray();
		}


		//Gets the first type found by providing just the name of the type. Rarely used (currently for upgrading ScriptControl tasks)
		public static Type GetType(string name, Type fallback){
			foreach (var t in GetAssemblyTypes(typeof(object))){
				if (t.Name == name)
					return t;
			}
			return fallback;
		}

		///Opens the MonoScript of a type if existant
		public static bool OpenScriptOfType(Type type){
			foreach (var path in AssetDatabase.GetAllAssetPaths()){
				if (path.EndsWith(type.Name + ".cs") || path.EndsWith(type.Name + ".js")){
					var script = (MonoScript)AssetDatabase.LoadAssetAtPath(path, typeof(MonoScript));
					if (type == script.GetClass()){
						AssetDatabase.OpenAsset(script);
						return true;
					}
				}
			}

			Debug.Log(string.Format("Can't open script of type '{0}', cause a script with the same name does not exist", type.FriendlyName() ));
			return false;
		}


		//Get all scene names (added in build settings)
		public static List<string> GetSceneNames(){
			var allSceneNames = new List<string>();
			foreach (var scene in EditorBuildSettings.scenes){
				if (scene.enabled){
					var name = scene.path.Substring(scene.path.LastIndexOf("/") + 1);
					name = name.Substring(0,name.Length-6);
					allSceneNames.Add(name);
				}
			}

			return allSceneNames;
		}

	}
}

#endif
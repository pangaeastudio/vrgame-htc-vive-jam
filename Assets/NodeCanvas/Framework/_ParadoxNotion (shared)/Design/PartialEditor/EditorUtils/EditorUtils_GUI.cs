#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NodeCanvas.Framework;
using UnityEditor;
using UnityEngine;
using ParadoxNotion;
using UnityObject = UnityEngine.Object;


namespace ParadoxNotion.Design{

    /// <summary>
    /// Flavor GUI and AutomaticInspector function
    /// </summary>

	partial class EditorUtils {

		private static Dictionary<Color, Texture2D> textures = new Dictionary<Color, Texture2D>();

        private static Texture2D _tex;
        private static Texture2D tex
        {
            get
            {
                if (_tex == null){
                    _tex = new Texture2D(1, 1);
                    _tex.hideFlags = HideFlags.HideAndDontSave;
                }
                return _tex;
            }
        }

        ///Get a colored 1x1 texture
        public static Texture2D GetTexture(Color color){
        	
        	if (textures.ContainsKey(color))
    			return textures[color];

        	var newTexture = new Texture2D(1,1);
        	newTexture.SetPixel(0, 0, color);
        	newTexture.Apply();
        	textures[color] = newTexture;
        	return newTexture;
        }

		//a cool label :-P (for headers)
		public static void CoolLabel(string text){
			GUI.skin.label.richText = true;
			GUI.color = lightOrange;
			GUILayout.Label("<b><size=14>" + text + "</size></b>");
			GUI.color = Color.white;
			GUILayout.Space(2);
		}

		//a thin separator
		public static void Separator(){
			GUI.backgroundColor = Color.black;
			GUILayout.Box("", GUILayout.MaxWidth(Screen.width), GUILayout.Height(2));
			GUI.backgroundColor = Color.white;
		}

		//A thick separator similar to ngui. Thanks
		public static void BoldSeparator(){
			var lastRect = GUILayoutUtility.GetLastRect();
			GUILayout.Space(14);
			GUI.color = new Color(0, 0, 0, 0.25f);
			GUI.DrawTexture(new Rect(0, lastRect.yMax + 6, Screen.width, 4), tex);
			GUI.DrawTexture(new Rect(0, lastRect.yMax + 6, Screen.width, 1), tex);
			GUI.DrawTexture(new Rect(0, lastRect.yMax + 9, Screen.width, 1), tex);
			GUI.color = Color.white;
		}

		//Combines the rest functions for a header style label
		public static void TitledSeparator(string title){
			GUILayout.Space(1);
			BoldSeparator();
			CoolLabel(title + " ▼");
			Separator();
		}

		//Just a fancy ending for inspectors
		public static void EndOfInspector(){
			var lastRect= GUILayoutUtility.GetLastRect();
			GUILayout.Space(8);
			GUI.color = new Color(0, 0, 0, 0.4f);
			GUI.DrawTexture(new Rect(0, lastRect.yMax + 6, Screen.width, 4), tex);
			GUI.DrawTexture(new Rect(0, lastRect.yMax + 4, Screen.width, 1), tex);
			GUI.color = Color.white;
		}

		//Used just after a textfield with no prefix to show an italic transparent text inside when empty
		public static void TextFieldComment(string check, string comment = "Comments..."){
			if (string.IsNullOrEmpty(check)){
				var lastRect = GUILayoutUtility.GetLastRect();
				GUI.color = new Color(1,1,1,0.3f);
				GUI.Label(lastRect, " <i>" + comment + "</i>");
				GUI.color = Color.white;
			}
		}

		//Show an automatic editor gui for arbitrary objects, taking into account custom attributes
		public static void ShowAutoEditorGUI(object o){
			foreach (var field in o.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public)){
				field.SetValue(o, GenericField(field.Name, field.GetValue(o), field.FieldType, field, o));
				GUI.backgroundColor = Color.white;
			}
		}

		//For generic automatic editors. Passing a MemberInfo will also check for attributes
		public static object GenericField(string name, object value, Type t, MemberInfo member = null, object instance = null){

			if (t == null){
				GUILayout.Label("NO TYPE PROVIDED!");
				return value;
			}

			if (member != null){

				//Hide class?
				if (t.GetCustomAttributes(typeof(HideInInspector), true ).FirstOrDefault() != null)
					return value;

				//Hide field?
				if (member.GetCustomAttributes(typeof(HideInInspector), true).FirstOrDefault() != null)
					return value;

				//Is required?
				if (member.GetCustomAttributes(typeof(RequiredFieldAttribute), true).FirstOrDefault() != null){
					if ( (value == null || value.Equals(null) ) || 
						(t == typeof(string) && string.IsNullOrEmpty((string)value) ) ||
						(typeof(BBParameter).IsAssignableFrom(t) && (value as BBParameter).isNull) )
					{
						GUI.backgroundColor = lightRed;
					}
				}
			}


			name = name.SplitCamelCase();


			if (member != null){
				var nameAtt = member.GetCustomAttributes(typeof(NameAttribute), true).FirstOrDefault() as NameAttribute;
				if (nameAtt != null){
					name = nameAtt.name;
				}

				if (instance != null){
					var showAtt = member.GetCustomAttributes(typeof(ShowIfAttribute), true).FirstOrDefault() as ShowIfAttribute;
					if (showAtt != null){
						var targetField = instance.GetType().GetField(showAtt.fieldName);
						if (targetField == null || targetField.FieldType != typeof(bool)){
							GUILayout.Label(string.Format("[ShowIf] Error: bool \"{0}\" does not exist.", showAtt.fieldName));
						} else {
							if ((bool)targetField.GetValue(instance) != showAtt.show){
								return value;
							}
						}
					}
				}			
			}



			//Before everything check BBParameter
			if (typeof(BBParameter).IsAssignableFrom(t)){
				return BBParameterField(name, (BBParameter)value, false, member);
			}

			//Then check UnityObjects
            if ( typeof(UnityObject).IsAssignableFrom(t) ) {
                if (t == typeof(Component) && (Component)value != null)
                    return ComponentField(name, (Component)value, typeof(Component));
                return EditorGUILayout.ObjectField(name, (UnityObject)value, t, ( typeof(Component).IsAssignableFrom(t) || t == typeof(GameObject) || t == typeof(UnityObject) ));
		    }

		    //Force UnityObject field?
		    if (member != null && member.GetCustomAttributes(typeof(ForceObjectFieldAttribute), true).FirstOrDefault() != null){
		    	return EditorGUILayout.ObjectField(name, value as UnityObject, t, true );
		    }

			//Restricted popup values?
			if (member != null){
				var popAtt = member.GetCustomAttributes(typeof(PopupFieldAttribute), true).FirstOrDefault() as PopupFieldAttribute;
				if (popAtt != null){
					return Popup<object>(name, value, popAtt.values.ToList(), false);
				}
			}


		    //Check Type of Type
			if (t == typeof(Type)){
				return Popup<Type>(name, (Type)value, UserTypePrefs.GetPreferedTypesList(typeof(object), false) );
			}

			//Check abstract
			if ( (value != null && value.GetType().IsAbstract) || (value == null && t.IsAbstract) ){
				EditorGUILayout.LabelField(name, string.Format("Abstract ({0})", t.FriendlyName()));
				return value;
			}

			//Create instance for some types
			if (value == null && !t.IsAbstract && !t.IsInterface && (t.IsValueType || t.GetConstructor(Type.EmptyTypes) != null || t.IsArray) ){
				if (t.IsArray){
					value = Array.CreateInstance(t.GetElementType(), 0);
				} else {
					value = Activator.CreateInstance(t);
				}
			}



			//Check the rest
			//..............
            if (t == typeof(string)){
				if (member != null){
					if (member.GetCustomAttributes(typeof(TagFieldAttribute), true).FirstOrDefault() != null)
						return EditorGUILayout.TagField(name, (string)value);
					var areaAtt = member.GetCustomAttributes(typeof(TextAreaFieldAttribute), true).FirstOrDefault() as TextAreaFieldAttribute;
					if (areaAtt != null){
						GUILayout.Label(name);
						var areaStyle = new GUIStyle(GUI.skin.GetStyle("TextArea"));
						areaStyle.wordWrap = true;
						var s = EditorGUILayout.TextArea((string)value, areaStyle, GUILayout.Height(areaAtt.height));
						return s;
					}
				}

				return EditorGUILayout.TextField(name, (string)value);
			}

			if (t == typeof(bool))
				return EditorGUILayout.Toggle(name, (bool)value);

			if (t == typeof(int)){
				if (member != null){
					var sField = member.GetCustomAttributes(typeof(SliderFieldAttribute), true).FirstOrDefault() as SliderFieldAttribute;
					if (sField != null)
						return (int)EditorGUILayout.Slider(name, (int)value, (int)sField.left, (int)sField.right );
					if (member.GetCustomAttributes(typeof(LayerFieldAttribute), true).FirstOrDefault() != null)
						return EditorGUILayout.LayerField(name, (int)value);
				}

				return EditorGUILayout.IntField(name, (int)value);
			}

			if (t == typeof(float)){
				if (member != null){
					var sField = member.GetCustomAttributes(typeof(SliderFieldAttribute), true).FirstOrDefault() as SliderFieldAttribute;
					if (sField != null)
						return EditorGUILayout.Slider(name, (float)value, sField.left, sField.right);
				}
				return EditorGUILayout.FloatField(name, (float)value);
			}

			if (t == typeof(Vector2))
				return EditorGUILayout.Vector2Field(name, (Vector2)value);

			if (t == typeof(Vector3))
				return EditorGUILayout.Vector3Field(name, (Vector3)value);

			if (t == typeof(Vector4))
				return EditorGUILayout.Vector4Field(name, (Vector4)value);

			if (t == typeof(Quaternion)){
				var quat = (Quaternion)value;
				var vec4 = new Vector4(quat.x, quat.y, quat.z, quat.w);
				vec4 = EditorGUILayout.Vector4Field(name, vec4);
				return new Quaternion(vec4.x, vec4.y, vec4.z, vec4.w);
			}

			if (t == typeof(Color))
				return EditorGUILayout.ColorField(name, (Color)value);

			if (t == typeof(Rect))
				return EditorGUILayout.RectField(name, (Rect)value);

			if (t == typeof(AnimationCurve))
				return EditorGUILayout.CurveField(name, (AnimationCurve)value);

			if (t == typeof(Bounds))
				return EditorGUILayout.BoundsField(name, (Bounds)value);

			if (t == typeof(LayerMask))
				return LayerMaskField(name, (LayerMask)value);
            
			if (t.IsSubclassOf(typeof(System.Enum)))
				return EditorGUILayout.EnumPopup(name, (System.Enum)value);

			if (typeof(IList).IsAssignableFrom(t))
				return ListEditor(name, (IList)value, t);

			if (typeof(IDictionary).IsAssignableFrom(t))
				return DictionaryEditor(name, (IDictionary)value, t);


			//show nested class members recursively
			if (value != null && !t.IsEnum && !t.IsInterface){
	
				GUILayout.BeginVertical();
				EditorGUILayout.LabelField(name, t.FriendlyName());
				EditorGUI.indentLevel ++;
				foreach (var field in value.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
					field.SetValue(value, GenericField(field.Name, field.GetValue(value), field.FieldType, field) );
				EditorGUI.indentLevel --;
				GUILayout.EndVertical();
		
			} else {

				EditorGUILayout.LabelField(name, string.Format("({0})", t.FriendlyName()));
			}
			
			return value;
		}
	}
}

#endif

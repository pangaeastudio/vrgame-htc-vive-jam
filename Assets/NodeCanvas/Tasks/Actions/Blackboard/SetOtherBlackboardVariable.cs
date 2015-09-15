using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;


namespace NodeCanvas.Tasks.Actions{
 
    [Category("✫ Blackboard")]
    [Description("Use this to set a variable on any blackboard by overriding the agent")]
    [AgentType(typeof(Blackboard))]
    public class SetOtherBlackboardVariable : ActionTask {

        [RequiredField]
        public BBParameter<string> targetVariableName;
        public BBParameter newValue;
       
        protected override string info{
            get {return string.Format("<b>{0}</b> = {1}", targetVariableName.ToString(), newValue != null? newValue.ToString() : ""); }
        }

        protected override void OnExecute(){
            (agent as IBlackboard).SetValue(targetVariableName.value, newValue.value);
            EndAction();
        }

        ////////////////////////////////////////
        ///////////GUI AND EDITOR STUFF/////////
        ////////////////////////////////////////
        #if UNITY_EDITOR
        
        protected override void OnTaskInspectorGUI(){
            DrawDefaultInspector();
            if (GUILayout.Button("Select Type"))
                EditorUtils.ShowPreferedTypesSelectionMenu(typeof(object), (t)=> {newValue = BBParameter.CreateInstance(t, blackboard);} );
        }
        
        #endif
    }
}
 
using System.Collections.Generic;
using System;
using UnityEngine;


namespace ParadoxNotion.Services {

    ///Singleton. Automatically added when needed, collectively calls methods that needs updating amongst other things relative to MonoBehaviours
    public class MonoManager : MonoBehaviour {

        //This is actually faster than adding/removing to delegate
        private readonly List<Action> updateMethods = new List<Action>();
        private readonly List<Action> guiMethods = new List<Action>();

        private static bool isQuiting;
        private static MonoManager _current;
        public static MonoManager current {
            get
            {
                if ( _current == null && !isQuiting ) {
                    _current = FindObjectOfType<MonoManager>();
                    if ( _current == null )
                        _current = new GameObject("_MonoManager").AddComponent<MonoManager>();
                }
                return _current;
            }
        }

        ///Creates MonoManager singleton
        public static void Create() { _current = current; }

        public static void AddMethod(Action method) { current.updateMethods.Add(method); }
        public static void RemoveMethod(Action method) { current.updateMethods.Remove(method); }

        public static void AddGUIMethod(Action method) { current.guiMethods.Add(method); }
        public static void RemoveGUIMethod(Action method) { current.guiMethods.Remove(method); }


        void Awake() {
            if ( _current != null && _current != this ) {
                DestroyImmediate(this.gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            _current = this;
        }

        void Update() {
            for (var i = 0; i < updateMethods.Count; i++){
                updateMethods[i]();
            }
        }

        void OnGUI(){
            for (var i = 0; i < guiMethods.Count; i++){
                guiMethods[i]();
            }
        }

        void OnApplicationQuit() { isQuiting = true; }
    }
}
#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using NodeCanvas.Framework;
using UnityEditor;
using UnityEngine;
using CanvasGroup = NodeCanvas.Framework.CanvasGroup;


namespace NodeCanvas.Editor{

	public class GraphEditor : EditorWindow{

		//the current graph loaded for editing. Can be a nested graph of the root graph
		public static Graph currentGraph;

		//the root graph that was first opened in the editor
		private Graph _rootGraph;
		private int rootGraphID;

		//the GrapOwner if any, that was used to open the editor and from which to read the rootGraph
		private GraphOwner _targetOwner;
		private int targetOwnerID;

		private Rect canvasRect; //window position to draw the canvas
		private Rect viewRect; //the panning rect that is drawn within canvasRect
		private Rect nodeBounds; //a rect encapsulating all the nodes
		private Rect totalCanvas;
		private Rect zoomRecoveryRect; //the groupRect used for the zoom trick
		private Event e;
		private readonly float topMargin   = 20;
		private GUISkin guiSkin;
		private bool isMultiSelecting;
		private Vector2 selectionStartPos;
		private int willRepaint;
		private Matrix4x4 oldMatrix;
		private Vector2 scrollPos = Vector2.zero;
		private float zoomFactor = 1f;
		private bool mouseButton2Down = false;

		private Vector2? smoothPan      = null;
		private float? smoothZoomFactor = null;
		private Vector2 _panVelocity    = Vector2.one;
		private float _zoomVelocity     = 1;

		private static bool fullDrawPass = true;
	    private static bool welcomeShown = false;

		private readonly static float unityTabHeight        = 22;
		private readonly static int gridSize                = 15;
		private readonly static Vector2 virtualCenterOffset = new Vector2(-5000, -5000);
		private readonly static Vector2 zoomPoint           = new Vector2(0,20);

		private Graph rootGraph{
			get
			{
				if (_rootGraph == null)
					_rootGraph = EditorUtility.InstanceIDToObject(rootGraphID) as Graph;
				return _rootGraph;
			}
			set
			{
				_rootGraph = value;
				rootGraphID = value != null? value.GetInstanceID() : -1;
			}
		}

		private GraphOwner targetOwner{
			get
			{
				if (_targetOwner == null)
					_targetOwner = EditorUtility.InstanceIDToObject(targetOwnerID) as GraphOwner;
				return _targetOwner;
			}
			set
			{
				_targetOwner = value;
				targetOwnerID = value != null? value.GetInstanceID() : -1;
			}
		}

		private Vector2 pan{
			get {return currentGraph != null? Vector2.Min(currentGraph.translation, Vector2.zero) : virtualCenter;}
			set {if (currentGraph != null) currentGraph.translation = Vector2.Min(value, Vector2.zero); }
		}

		private Vector2 virtualPanPosition{
			get {return (pan - virtualCenterOffset) * -1; }
		}

		private Vector2 virtualCenter{
			get {return -virtualCenterOffset + viewRect.size/2;}
		}

		private Vector2 mousePosInCanvas{
			get {return ViewToCanvas(Event.current.mousePosition); }
		}

		private bool nodesOutOfView{
			get {return viewRect.center != nodeBounds.center;}
		}

		void OnEnable(){
	        #if UNITY_5_1
	        titleContent = new GUIContent("NC Editor");
	        #else
	        title = "NC Editor";
	        #endif

			willRepaint = 1;
			fullDrawPass = true;
			guiSkin = EditorGUIUtility.isProSkin? (GUISkin)Resources.Load("NodeCanvasSkin") : (GUISkin)Resources.Load("NodeCanvasSkinLight");
			wantsMouseMove = true;
			minSize = new Vector2(700, 300);
			UpdateReferences();
			EditorApplication.playmodeStateChanged += UpdateReferences;
		    EditorApplication.playmodeStateChanged += ()=> { welcomeShown = true; Graph.currentSelection = null; };
			Repaint();
            //UNITY 5
            //Application.logMessageReceived += 
		}



		//Update the references for editor convenience
		//ONLY when we have a target GraphOwner
		void UpdateReferences(){
			if (targetOwner != null){
				rootGraph = targetOwner.graph;
				if (rootGraph != null){
					rootGraph.agent = targetOwner;
					rootGraph.blackboard = targetOwner.blackboard;
				}
				//update refs for the currenlty viewing graph as well
				GetCurrentGraph(rootGraph).agent = targetOwner;
				GetCurrentGraph(rootGraph).blackboard = targetOwner.blackboard;
			}
		}

		void OnDisable(){
			//AssetDatabase.SaveAssets();
		    welcomeShown = false;
		}


		void Update(){
			DoSmoothPan();
			DoSmoothZoom();
		}

		void DoSmoothPan(){
			
			if (smoothPan == null)
				return;
			
			var smooth = (Vector2)smoothPan;
			if ( (smooth - pan).magnitude > 0 ){
				smooth = new Vector2( Mathf.Round(smooth.x), Mathf.Round(smooth.y) ); //pixel perfect correction
				pan = Vector2.SmoothDamp(pan, smooth, ref _panVelocity, 0.0001f * Time.deltaTime);
				Repaint();
			} else {
				smoothPan = null;
			}
		}

		void DoSmoothZoom(){
			
			if (smoothZoomFactor == null)
				return;

			var zmooth = (float)smoothZoomFactor;
			if ( Mathf.Abs(zmooth - zoomFactor) > 0 ){
				
				zoomFactor = Mathf.SmoothDamp(zoomFactor, zmooth, ref _zoomVelocity, 0.0001f * Time.deltaTime);
				//pixel perfect correction
				if (zoomFactor > 0.99999f)
					zoomFactor = 1;

				Repaint();
			} else {
				smoothZoomFactor = null;
			}
		}

		//GUI space to canvas space
		Vector2 ViewToCanvas(Vector2 viewPos){
			return (viewPos - pan)/zoomFactor;
		}

		//Canvas space to GUI space
		Vector2 CanvasToView(Vector2 canvasPos){
			return (canvasPos * zoomFactor) + pan;
		}

		void OnGUI(){

			if (EditorApplication.isCompiling){
				ShowNotification(new GUIContent("Compiling Please Wait..."));
				return;			
			}

			//Init
			GUI.color = Color.white;
			GUI.backgroundColor = Color.white;
			e = Event.current;
			GUI.skin = guiSkin;

			//get the graph from the GraphOwner if one is set
			if (targetOwner != null)
				rootGraph = targetOwner.graph;

			if (rootGraph == null){
				ShowNotification(new GUIContent("Please select a GraphOwner GameObject or a Graph Asset"));
				return;
			}

			//hande undo/redo keyboard commands
			if (e.type == EventType.ValidateCommand && e.commandName == "UndoRedoPerformed"){
                GUIUtility.hotControl = 0;
                GUIUtility.keyboardControl = 0;
                Graph.currentSelection = null; //TODO: fix this to not happen
                currentGraph.Validate(); //validate the graph after the undo
                e.Use();
				return;
			}

			///should we set dirty the owner (if any). Put in practise at the end
			var setDirty = false;
			if ( (e.type == EventType.MouseUp && e.button != 2) || e.type == EventType.KeyUp ){
				setDirty = true;
			}

			//set the currently viewing graph by getting the current child graph from the root graph recursively
			currentGraph = GetCurrentGraph(rootGraph);
			if (currentGraph == null || ReferenceEquals(currentGraph, null)){
				return;
			}

			//handles mouse & keyboard inputs
			HandleEvents(e);

			//initialize canvasRect
			canvasRect = new Rect(5, topMargin, position.width -10, position.height - topMargin - 5);

			//canvas background
			GUI.Box(canvasRect, string.Format("{0}\n{1}", currentGraph.GetType().Name, "@NodeCanvas v2.3.6"), "canvasBG");


			if (zoomFactor != 1)
				canvasRect = StartZoomArea(canvasRect);

			//main group
			GUI.BeginGroup(canvasRect);

				//pan the view rect
				totalCanvas = canvasRect;
				totalCanvas.x = 0;
				totalCanvas.y = 0;
				totalCanvas.x += pan.x/zoomFactor;
				totalCanvas.y += pan.y/zoomFactor;
				totalCanvas.width -= pan.x/zoomFactor;
				totalCanvas.height -= pan.y/zoomFactor;

				//begin panning group
				GUI.BeginGroup(totalCanvas);

					//inverse pan the view rect
					viewRect = totalCanvas;
					viewRect.x = 0;
					viewRect.y = 0;
					viewRect.x -= pan.x/zoomFactor;
					viewRect.y -= pan.y/zoomFactor;
					viewRect.width += pan.x/zoomFactor;
					viewRect.height += pan.y/zoomFactor;

					nodeBounds = GetNodeBounds(viewRect, true);
					DrawGrid(viewRect, pan, zoomFactor);

					DoCanvasGroups(e);

					BeginWindows();
					currentGraph.ShowNodesGUI(e, viewRect, fullDrawPass, mousePosInCanvas, zoomFactor);
					EndWindows();		

					DoCanvasRectSelection(viewRect);
			
				GUI.EndGroup();

			GUI.EndGroup();

			if (zoomFactor != 1)
				EndZoomArea();

			ShowScrollBars();


			//Breadcrumb navigation
			GUILayout.BeginArea(new Rect(20, topMargin + 5, Screen.width, Screen.height));
			ShowBreadCrumbNavigation(rootGraph);
			GUILayout.EndArea();


			//Graph controls (after windows so that panels (inspector, blackboard) show on top)
			currentGraph.ShowGraphControls(e, mousePosInCanvas);


			//repaint?
			if (willRepaint > 0 || rootGraph.isRunning){
				willRepaint = Mathf.Max(willRepaint -1, 0);
				Repaint();
			}

			//Set nodes size to minimum. They rescale to fit automaticaly since they use GUILayout.Window.
			//This is done if GUI.changed since basicaly the only reason for a size to change is because some node inspector value has changed
			if (GUI.changed){
				foreach (var node in currentGraph.allNodes){
					node.nodeRect = new Rect( node.nodePosition.x, node.nodePosition.y, Node.minSize.x, Node.minSize.y );
				}
				Repaint();
			}

			//Set the targetowner if any dirty for correct prefab override in case of local graphs
			if (setDirty){
				setDirty = false;
				EditorUtility.SetDirty(currentGraph);
			}

			//closure
			GUI.Box(canvasRect,"", "canvasBorders");
			GUI.skin = null;
			GUI.color = Color.white;
			GUI.backgroundColor = Color.white;
			fullDrawPass = false;
		}

		//Recursively get the currenlty showing nested graph starting from the root
		Graph GetCurrentGraph(Graph root){
			if (root.currentChildGraph == null)
				return root;
			return GetCurrentGraph(root.currentChildGraph);
		}

		//Starts a zoom area, returns the scaled container rect
		Rect StartZoomArea(Rect container){
			GUI.EndGroup();
			container.height += unityTabHeight;
			container.width *= 1/zoomFactor;
			container.height *= 1/zoomFactor;
			oldMatrix = GUI.matrix;
			var matrix1 = Matrix4x4.TRS( zoomPoint, Quaternion.identity, Vector3.one );
			var matrix2 = Matrix4x4.Scale(new Vector3(zoomFactor, zoomFactor, 1f));
			GUI.matrix = matrix1 * matrix2 * matrix1.inverse * GUI.matrix;
			return container;
		}

		//Ends the zoom area
		void EndZoomArea(){
			GUI.matrix = oldMatrix;
			zoomRecoveryRect.y = unityTabHeight;
			zoomRecoveryRect.width = Screen.width;
			zoomRecoveryRect.height = Screen.height;
			GUI.BeginGroup(zoomRecoveryRect); //Recover rect
		}


		void HandleEvents(Event e){

			//set repaint counter if need be
			if (mouseOverWindow == this && (e.isMouse || e.isKey) ){
				willRepaint += 2;
			}

			//snap all nodes on assumption change
			if (e.type == EventType.MouseUp || e.type == EventType.KeyUp){
				SnapNodes();
			}

			if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F && GUIUtility.keyboardControl == 0){
				if (currentGraph.allNodes.Count > 0)
					FocusPosition(GetNodeBounds(viewRect, false).center);
				else FocusPosition(virtualCenter);
			}

			if (e.type == EventType.MouseDown && e.button == 2 && e.clickCount == 2){
				FocusPosition( ViewToCanvas(e.mousePosition) );
			}


			if (e.type == EventType.ScrollWheel && Graph.allowClick){
				if (canvasRect.Contains(e.mousePosition)){
					var zoomDelta = e.shift? 0.1f : 0.25f;
					ZoomAt(e.mousePosition, -e.delta.y > 0? zoomDelta : -zoomDelta );
				}
			}

			//Control + Space for default zoom level (1f)
			if (e.type == EventType.KeyDown && e.control && e.keyCode == KeyCode.Space){
				ZoomAt(e.mousePosition, 1);
			}

			if ( (e.button == 2 && e.type == EventType.MouseDrag && canvasRect.Contains(e.mousePosition))
				|| ( (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.alt && e.isMouse) )
			{
				pan += e.delta;
				smoothPan = null;
				smoothZoomFactor = null;
				e.Use();
			}

			if (e.type == EventType.MouseDown && e.button == 2 && canvasRect.Contains(e.mousePosition)){
				mouseButton2Down = true;
			}

			if (e.type == EventType.MouseUp && e.button == 2){
				mouseButton2Down = false;
			}

			if (mouseButton2Down == true){
				EditorGUIUtility.AddCursorRect (new Rect(0,0,Screen.width,Screen.height), MouseCursor.Pan);
			}
		}

		///Translate the graph to to center the target pos
		void FocusPosition(Vector2 targetPos){
			smoothPan = -targetPos;
			smoothPan += new Vector2( viewRect.width/2, viewRect.height/2);
			smoothPan *= zoomFactor;
		}


		///Zoom with center position
		void ZoomAt(Vector2 center, float delta){
			var pinPoint = (center - pan)/zoomFactor;
			var newZ = zoomFactor;
			newZ += delta;
			newZ = Mathf.Clamp(newZ, 0.25f, 1f);
			smoothZoomFactor = newZ;
			var a = (pinPoint * newZ) + pan;
			var b = center;
			var diff = b - a;
			smoothPan = pan + diff;
		}


		//vertical and horizontal indicator scrollbars (would prefer a MinMaxSlider?)
		void ShowScrollBars(){

			if (!nodesOutOfView)
				return;

			GUI.enabled = false;
			scrollPos = -pan;

			var xLeftDif = Mathf.Max(viewRect.x - nodeBounds.x, 0);
			var xRightDiff = Mathf.Max(nodeBounds.xMax - viewRect.xMax, 0);
			var xHowMuchSee = nodeBounds.width - xLeftDif - xRightDiff ;
			if (xLeftDif > 0 || xRightDiff > 0){
				var xScrollBarRect = new Rect(5, position.height - 18, position.width - 10, 10 );
				scrollPos.x = GUI.HorizontalScrollbar(xScrollBarRect, scrollPos.x, xHowMuchSee, nodeBounds.x, nodeBounds.xMax);
			}

			var yTopDiff = Mathf.Max(viewRect.y - nodeBounds.y, 0);
			var yBottomDiff = Mathf.Max(nodeBounds.yMax - viewRect.yMax, 0);
			var yHowMuchSee = nodeBounds.height - yTopDiff - yBottomDiff ;
			if (yTopDiff > 0 || yBottomDiff > 0){
				var yScrollBarRect = new Rect(position.width - 20, topMargin, 10, position.height - topMargin - 20);
				scrollPos.y = GUI.VerticalScrollbar(yScrollBarRect, scrollPos.y, yHowMuchSee, nodeBounds.y, nodeBounds.yMax);
			}
/*			
			if (pan != -scrollPos){
				pan = -scrollPos;
				smoothPan = null;
			}
*/
			GUI.enabled = true;
		}

	
		///Gets the bound rect for the nodes
		Rect GetNodeBounds(Rect container, bool bound = false){
			var minX = float.PositiveInfinity;
			var minY = float.PositiveInfinity;
			var maxX = float.NegativeInfinity;
			var maxY = float.NegativeInfinity;
			
			for (var i = 0; i < currentGraph.allNodes.Count; i++){
				minX = Mathf.Min(minX, currentGraph.allNodes[i].nodeRect.xMin);
				minY = Mathf.Min(minY, currentGraph.allNodes[i].nodeRect.yMin);
				maxX = Mathf.Max(maxX, currentGraph.allNodes[i].nodeRect.xMax);
				maxY = Mathf.Max(maxY, currentGraph.allNodes[i].nodeRect.yMax);
			}

			minX -= 50;
			minY -= 50;
			maxX += 50;
			maxY += 50;

			if (bound){
				minX = Mathf.Min(minX, container.xMin + 50);
				minY = Mathf.Min(minY, container.yMin + 50);
				maxX = Mathf.Max(maxX, container.xMax - 50);
				maxY = Mathf.Max(maxY, container.yMax - 50);
			}

			return Rect.MinMaxRect(minX, minY, maxX, maxY);
		}



		//Do graphical multi selection box for nodes
		void DoCanvasRectSelection(Rect container){
			
			if (Graph.allowClick && e.type == EventType.MouseDown && e.button == 0 && !e.alt && !e.shift && canvasRect.Contains(CanvasToView(e.mousePosition)) ){
				Graph.currentSelection = null;
				selectionStartPos = e.mousePosition;
				isMultiSelecting = true;
				e.Use();
			}

			if (isMultiSelecting && e.type == EventType.MouseUp){
				var rect = GetSelectionRect(selectionStartPos, e.mousePosition);
				var overlapedNodes = currentGraph.allNodes.Where(n => rect.Overlaps(n.nodeRect) && !n.isHidden).ToList();
				isMultiSelecting = false;

				if (Event.current.control && rect.width > 50 && rect.height > 50){
					Undo.RecordObject(currentGraph, "Create Group");
					if (currentGraph.canvasGroups == null)
						currentGraph.canvasGroups = new List<CanvasGroup>();
					currentGraph.canvasGroups.Add( new CanvasGroup(rect, "New Canvas Group") );
				} else {
					if (overlapedNodes.Count > 0){
						Graph.multiSelection = overlapedNodes.Cast<object>().ToList();
						e.Use();
					}
				}
			}

			if (isMultiSelecting){
				var rect = GetSelectionRect(selectionStartPos, e.mousePosition);
				if (rect.width > 5 && rect.height > 5){
					GUI.color = new Color(0.5f,0.5f,1,0.3f);
					GUI.Box(rect, "");
					foreach (var node in currentGraph.allNodes){
						if (rect.Overlaps(node.nodeRect) && !node.isHidden){
							var highlightRect = node.nodeRect;
							GUI.Box(highlightRect, "", "windowHighlight");
						}
					}
					if (rect.width > 50 && rect.height > 50){
						GUI.color = new Color(1,1,1, e.control? 0.6f : 0.15f);
						GUI.Label(new Rect( e.mousePosition.x + 16, e.mousePosition.y, 120, 22 ), "<i>+ control for group</i>");
					}
				}
			}

			GUI.color = Color.white;
		}

		//Get a rect from to for selection
		Rect GetSelectionRect(Vector2 startPos, Vector2 endPos){
			var num1 = (startPos.x < endPos.x)? startPos.x : endPos.x;
			var num2 = (startPos.x > endPos.x)? startPos.x : endPos.x;
			var num3 = (startPos.y < endPos.y)? startPos.y : endPos.y;
			var num4 = (startPos.y > endPos.y)? startPos.y : endPos.y;
			return new Rect(num1, num3, num2 - num1, num4 - num3);		
		}

		//Draw a simple grid
		void DrawGrid(Rect container, Vector2 offset, float zoomFactor){

			var scaledX = (container.width - offset.x)/zoomFactor;
			var scaledY = (container.height - offset.y)/zoomFactor;

			for (var i = 0 - (int)offset.x; i < scaledX ; i++){
				if (i % gridSize == 0){
					Handles.color = new Color(0,0,0, i % (gridSize * 5) == 0? 0.2f : 0.1f);
					Handles.DrawLine(new Vector3(i,0,0), new Vector3(i, scaledY ,0));
				}
			}
			
			for (var i = 0 - (int)offset.y; i < scaledY ; i++){
				if (i % gridSize == 0){
					Handles.color = new Color(0,0,0, i % (gridSize * 5) == 0? 0.2f : 0.1f);
					Handles.DrawLine(new Vector3(0, i, 0), new Vector3( scaledX, i, 0));
				}
			}

			Handles.color = Color.white;
		}

/*
		private bool isShowingHotBox = false;
		void DoHotBoxPanel(){
			var e = Event.current;
			if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Space){
				isShowingHotBox = true;
			}
			if (e.type == EventType.KeyUp && e.keyCode == KeyCode.Space){
				isShowingHotBox = false;
			}

			if (isShowingHotBox){
				var hotboxRect = new Rect( 50, 50, Screen.width - 100, Screen.height - 100 );
				GUI.Box(hotboxRect, "minimap", (GUIStyle)"editorPanel");
				GUI.BeginGroup(hotboxRect);
				//TODO?
				GUI.EndGroup();
			}
		}
*/

		//This is the hierarchy shown at top left. Recusrsively show the nested path
		void ShowBreadCrumbNavigation(Graph root){

			if (root == null)
				return;

			//if something selected the inspector panel shows on top of the breadcrub. If external inspector active it doesnt matter, so draw anyway.
			if (Graph.currentSelection != null && !NCPrefs.useExternalInspector)
				return;

			var agentInfo = root.agent != null? root.agent.gameObject.name : "No Agent";
			var bbInfo = root.blackboard != null? root.blackboard.name : "No Blackboard";
			var graphInfo = string.Format("<color=#ff4d4d>({0})</color>", targetOwner != null && targetOwner.graph == root && targetOwner.graphIsLocal? "Bound" : ( EditorUtility.IsPersistent(root)? "Asset Reference" : "Instance" ) );

			GUI.color = new Color(1f,1f,1f,0.5f);
			
			GUILayout.BeginVertical();
			if (root.currentChildGraph == null){

				if (root.agent == null && root.blackboard == null){
					GUILayout.Label(string.Format("<b><size=22>{0} {1}</size></b>", root.name, graphInfo));	
				} else {
					GUILayout.Label(string.Format("<b><size=22>{0} {1}</size></b>\n<size=10>{2} | {3}</size>", root.name, graphInfo, agentInfo, bbInfo));
				}

			} else {

				GUILayout.BeginHorizontal();

				//"button" implemented this way due to e.used. It's a delegate matter..
				GUILayout.Label("⤴ " + root.name, (GUIStyle)"button");
				if (Event.current.type == EventType.MouseUp && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition)){
					root.currentChildGraph = null;
				}

				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				ShowBreadCrumbNavigation(root.currentChildGraph);
			}

			GUILayout.EndVertical();
			GUI.color = Color.white;
		}

		
		private static Node[] tempGroupNodes;
		private static CanvasGroup[] tempNestedGroups;
		void DoCanvasGroups(Event e){

			if (currentGraph.canvasGroups == null)
				return;

			for (var i = 0; i < currentGraph.canvasGroups.Count; i++){
				var style = EditorGUIUtility.isProSkin? (GUIStyle)"editorPanel" : "box";
				var group = currentGraph.canvasGroups[i];
				var handleRect = new Rect( group.rect.x, group.rect.y, group.rect.width, 25);
				var scaleRect = new Rect( group.rect.xMax - 20, group.rect.yMax -20, 20, 20);
				GUI.color = new Color(1,1,1,0.4f);
				GUI.Box(group.rect, "", style );
				GUI.Box(new Rect(scaleRect.x+10, scaleRect.y+10, 6,6), "", (GUIStyle)"scaleArrow" );
				GUI.color = Color.white;
				GUI.Box(handleRect, group.name, style );
				EditorGUIUtility.AddCursorRect(handleRect, group.isRenaming? MouseCursor.Text : MouseCursor.Link);
				EditorGUIUtility.AddCursorRect(scaleRect, MouseCursor.ResizeUpLeft);

				if (group.isRenaming){
					group.name = EditorGUI.TextField(handleRect, group.name, style);
					if (e.keyCode == KeyCode.Return || (e.type == EventType.MouseDown && !handleRect.Contains(e.mousePosition)) ){
						group.isRenaming = false;
		                GUIUtility.hotControl = 0;
		                GUIUtility.keyboardControl = 0;						
					}
				}

				if (e.type == EventType.MouseDown && Graph.allowClick){

					//calc group nodes
					tempGroupNodes = currentGraph.allNodes.Where(n => RectEncapsulates(group.rect, n.nodeRect) ).ToArray();
					tempNestedGroups = currentGraph.canvasGroups.Where(c => RectEncapsulates(group.rect, c.rect)).ToArray();

					if (handleRect.Contains(e.mousePosition)){

						if (e.button == 1){
							Undo.RecordObject(this, "Group Operation");
							var menu = new GenericMenu();
							menu.AddItem(new GUIContent("Rename"), false, ()=> { group.isRenaming = true; } );
							menu.AddItem(new GUIContent("Select Nodes"), false, ()=> { Graph.multiSelection = tempGroupNodes.Cast<object>().ToList(); } );
							menu.AddItem(new GUIContent("Delete Group"), false, ()=> { currentGraph.canvasGroups.Remove(group); } );
							Graph.PostGUI += ()=> { menu.ShowAsContext(); };
						} else if (e.button == 0){
							group.isDragging = true;
						}

						e.Use();
					}

					if (e.button == 0 && scaleRect.Contains(e.mousePosition)){
						group.isRescaling = true;
						e.Use();
					}
				}

				if (e.type == EventType.MouseUp){
					group.isDragging = false;
					group.isRescaling = false;
				}

				if (e.type == EventType.MouseDrag){

					if (group.isDragging){

						Undo.RecordObject(currentGraph, "Move Canvas Group");
						group.rect.x += e.delta.x;
						group.rect.y += e.delta.y;						

						foreach (var node in tempGroupNodes){
							node.nodePosition += e.delta;
						}

						foreach (var otherGroup in tempNestedGroups){
							otherGroup.rect.x += e.delta.x;
							otherGroup.rect.y += e.delta.y;
						}
					}

					if (group.isRescaling){
						Undo.RecordObject(currentGraph, "Scale Canvas Group");
						group.rect.xMax = Mathf.Max(e.mousePosition.x + 5, group.rect.x + 100);
						group.rect.yMax = Mathf.Max(e.mousePosition.y + 5, group.rect.y + 100);
					}
				}
			}
		}

		//could be an extension but realy no else used
		bool RectEncapsulates(Rect a, Rect b){
			return a.x < b.x && a.xMax > b.xMax && a.y < b.y && a.yMax > b.yMax;
		}


		//Snap all nodes (this is not what is done while dragging a node. This is called OnMouseUp)
		void SnapNodes(){

			if (!NCPrefs.doSnap)
				return;

			foreach(var node in currentGraph.allNodes){
				var snapedPos = new Vector2(node.nodeRect.xMin, node.nodeRect.yMin);
				snapedPos.y = Mathf.Round(snapedPos.y / 15) * 15;
				snapedPos.x = Mathf.Round(snapedPos.x / 15) * 15;
				node.nodeRect = new Rect(snapedPos.x, snapedPos.y, node.nodeRect.width, node.nodeRect.height);
			}
		}

		//Change viewing graph based on Graph or GraphOwner
		void OnSelectionChange(){
			
			if (NCPrefs.isLocked)
				return;

			if (Selection.activeObject != null && Selection.activeObject is Graph){
				var lastEditor = EditorWindow.focusedWindow;
				OpenWindow(Selection.activeObject as Graph);
				if (lastEditor) lastEditor.Focus();
				return;
			}

			if (Selection.activeGameObject != null){
				var foundOwner = Selection.activeGameObject.GetComponent<GraphOwner>();
				if (foundOwner != null){
					if (foundOwner.graph != null){
						var lastEditor = EditorWindow.focusedWindow;
						OpenWindow(foundOwner);
						if (lastEditor) lastEditor.Focus();
					}
				}
			}
		}


	    //Opening the window for a graph owner
	    public static GraphEditor OpenWindow(GraphOwner owner){
	    	var window = OpenWindow(owner.graph, owner, owner.blackboard);
	    	window.targetOwner = owner;
	    	return window;
	    }

	    //For opening the window from gui button in the nodegraph's Inspector.
	    public static GraphEditor OpenWindow(Graph newGraph){
	    	return OpenWindow(newGraph, newGraph.agent, newGraph.blackboard);
	    }

	    //Open GraphEditor initializing target graph
	    public static GraphEditor OpenWindow(Graph newGraph, Component agent, IBlackboard blackboard) {

			var window = GetWindow<GraphEditor>();

		    if (newGraph != null){
		        //force assign references only in edit mode so that we can debug correctly
		        if (!Application.isPlaying){
			        newGraph.agent = agent;
			        newGraph.blackboard = blackboard;	        	
		        }

		        newGraph.currentChildGraph = null;
		        newGraph.UpdateNodeIDs(false);
		    }

	        window.rootGraph = newGraph;
	        window.targetOwner = null;
	        Graph.currentSelection = null;

            if (NCPrefs.showWelcomeWindow && !Application.isPlaying && welcomeShown == false){
                welcomeShown = true;
                WelcomeWindow.OpenWindow();
            }

            return window;
	    }
	}
}

#endif
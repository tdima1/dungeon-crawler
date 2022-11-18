using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class RoomNodeGraphEditor : EditorWindow
{
   private GUIStyle roomNodeStyle;
   private GUIStyle roomNodeSelectedStyle;

   private static RoomNodeGraphSO currentRoomNodeGraph;
   private RoomNodeTypeListSO roomNodeTypeList;

   private RoomNodeSO currentRoomNode = null;

   private Vector2 graphOffset;
   private Vector2 graphDrag;

   private const float nodeWidth = 160f;
   private const float nodeHeight = 75f;
   private const int nodePadding = 25;
   private const int nodeBorder = 12;
   private const float connectingLineWidth = 3f;
   private const float connectingLineArrowSize = 12f;

   private const float gridLarge = 100f;
   private const float gridSmall = 25f;

   [MenuItem("Room Node Graph Editor", menuItem = "Window/Dungeon Editor/Room Node Graph Editor")]
   private static void OpenWindow()
   {
      GetWindow<RoomNodeGraphEditor>("Room Node Graph Editor");
   }

   private void OnEnable()
   {
      Selection.selectionChanged += InspectorSelectionChanged;

      roomNodeStyle = new GUIStyle();
      roomNodeStyle.normal.background = EditorGUIUtility.Load("node1") as Texture2D;
      roomNodeStyle.normal.textColor = Color.white;
      roomNodeStyle.padding = new RectOffset(nodePadding, nodePadding, nodePadding, nodePadding);
      roomNodeStyle.border = new RectOffset(nodeBorder, nodeBorder, nodeBorder, nodeBorder);

      roomNodeSelectedStyle = new GUIStyle();
      roomNodeSelectedStyle.normal.background = EditorGUIUtility.Load("node1 on") as Texture2D;
      roomNodeSelectedStyle.normal.textColor = Color.white;
      roomNodeSelectedStyle.padding = new RectOffset(nodePadding, nodePadding, nodePadding, nodePadding);
      roomNodeSelectedStyle.border = new RectOffset(nodeBorder, nodeBorder, nodeBorder, nodeBorder);

      roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
   }

   private void OnDisable()
   {
      Selection.selectionChanged -= InspectorSelectionChanged;
   }

   [OnOpenAsset(0)]
   public static bool OnDoubleClickAsset(int instanceId, int line)
   {
      RoomNodeGraphSO roomNodeGraph = EditorUtility.InstanceIDToObject(instanceId) as RoomNodeGraphSO;

      if (roomNodeGraph != null)
      {
         OpenWindow();
         currentRoomNodeGraph = roomNodeGraph;

         return true;
      }
      return false;
   }

   private void OnGUI()
   {
      if (currentRoomNodeGraph != null)
      {
         DrawBackgroundGrid(gridSmall, 0.2f, Color.gray);
         DrawBackgroundGrid(gridLarge, 0.3f, Color.gray);

         DrawDraggedLine();

         ProcessEvents(Event.current);

         DrawRoomConnections();

         DrawRoomNodes();
      }

      if (GUI.changed)
      {
         Repaint();
      }
   }

   private void DrawBackgroundGrid(float gridSize, float gridOpacity, Color gridColor)
   {
      int verticalLineCount = Mathf.CeilToInt((position.width + gridSize) / gridSize);
      int horizontalLineCount = Mathf.CeilToInt((position.height + gridSize) / gridSize);

      Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

      graphOffset += graphDrag * 0.5f;

      Vector3 gridOffset = new Vector3(graphOffset.x % gridSize, graphOffset.y % gridSize, 0);

      for (int i = 0; i < verticalLineCount; i++)
      {
         Handles.DrawLine(new Vector3(gridSize * i, -gridSize, 0) + gridOffset, new Vector3(gridSize * i, position.height + gridSize, 0f) + gridOffset);
      }

      for (int i = 0; i < horizontalLineCount; i++)
      {
         Handles.DrawLine(new Vector3(-gridSize, gridSize * i, 0) + gridOffset, new Vector3(position.width + gridSize, gridSize * i, 0f) + gridOffset);
      }

      Handles.color = Color.white;
   }

   private void DrawDraggedLine()
   {
      if (currentRoomNodeGraph.linePosition != Vector2.zero)
      {
         Handles.DrawBezier(
            currentRoomNodeGraph.roomNodeToDrawLineFrom.rect.center,
            currentRoomNodeGraph.linePosition,
            currentRoomNodeGraph.roomNodeToDrawLineFrom.rect.center,
            currentRoomNodeGraph.linePosition,
            Color.white,
            null,
            connectingLineWidth);
      }
   }

   private void ProcessEvents(Event currentEvent)
   {
      graphDrag = Vector2.zero;


      if (currentRoomNode == null || !currentRoomNode.IsLeftClickDragging)
      {
         currentRoomNode = IsMouseOverRoomNode(currentEvent);
      }

      if (currentRoomNode == null || currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
      {
         ProcessRoomNodeGraphEvents(currentEvent);
      }
      else
      {
         currentRoomNode.ProcessEvents(currentEvent);
      }

   }

   private RoomNodeSO IsMouseOverRoomNode(Event currentEvent)
   {
      for (int i = currentRoomNodeGraph.roomNodeList.Count - 1; i >= 0; i--)
      {
         if (currentRoomNodeGraph.roomNodeList[i].rect.Contains(currentEvent.mousePosition))
         {
            return currentRoomNodeGraph.roomNodeList[i];
         }
      }

      return null;
   }

   private void DrawRoomNodes()
   {
      foreach (var roomNode in currentRoomNodeGraph.roomNodeList)
      {
         if (roomNode.IsSelected)
         {
            roomNode.Draw(roomNodeSelectedStyle);
         } else
         {
            roomNode.Draw(roomNodeStyle);
         }
      }

      GUI.changed = true;
   }

   private void ProcessRoomNodeGraphEvents(Event currentEvent)
   {
      switch (currentEvent.type)
      {
         case EventType.MouseDown:
            ProcessMouseDownEvent(currentEvent);
            break;
         case EventType.MouseDrag:
            ProcessMouseDragEvent(currentEvent);
            break;
         case EventType.MouseUp:
            ProcessMouseUpEvent(currentEvent);
            break;
         default:
            break;
      }
   }

   private void ProcessMouseDownEvent(Event currentEvent)
   {
      if (currentEvent.button == 1)
      {
         ShowContextMenu(currentEvent.mousePosition);
      } else if (currentEvent.button == 0) {
         ClearLineDrag();
         ClearAllSelectedRoomNodes();
      }
   }

   private void ProcessMouseDragEvent(Event currentEvent)
   {
      if (currentEvent.button == 1)
      {
         ProcessRightMouseDragEvent(currentEvent);
      } 
      else if (currentEvent.button == 0)
      {
         ProcessLeftMouseDragEvent(currentEvent.delta);
      }
   }


   private void ProcessMouseUpEvent(Event currentEvent)
   {
      if (currentEvent.button == 1 && currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
      {
         RoomNodeSO roomNode = IsMouseOverRoomNode(currentEvent);

         if (roomNode != null)
         {
            if (currentRoomNodeGraph.roomNodeToDrawLineFrom.AddChildRoomNodeIdToRoomNode(roomNode.id))
            {
               roomNode.AddParentRoomNodeIdToRoomNode(currentRoomNodeGraph.roomNodeToDrawLineFrom.id);
            }
         }

         ClearLineDrag();
      }
   }

   private void ShowContextMenu(Vector2 mousePosition)
   {
      GenericMenu menu = new GenericMenu();
      menu.AddItem(new GUIContent("Create Room Node"), false, CreateRoomNode, mousePosition);

      menu.AddSeparator("");
      menu.AddItem(new GUIContent("Select All"), false, SelectAllRoomNodes);

      menu.AddSeparator("");
      menu.AddItem(new GUIContent("Delete Selected Room Node Links"), false, DeleteSelectedRoomNodeLinks);
      menu.AddItem(new GUIContent("Delete Selected Room Nodes"), false, DeleteSelectedRoomNodes);

      menu.ShowAsContext();
   }

   private void ProcessRightMouseDragEvent(Event currentEvent)
   {
      if (currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
      {
         DragConnectingLine(currentEvent.delta);
         GUI.changed = true;
      }
   }
   private void ProcessLeftMouseDragEvent(Vector2 delta)
   {
      graphDrag = delta;

      for (int i = 0; i < currentRoomNodeGraph.roomNodeList.Count; i++)
      {
         currentRoomNodeGraph.roomNodeList[i].DragNode(delta);
      }
      GUI.changed = true;
   }

   private void DragConnectingLine(Vector2 delta)
   {
      currentRoomNodeGraph.linePosition += delta;
   }

   private void CreateRoomNode(object mousePositionObject)
   {
      if (currentRoomNodeGraph.roomNodeList.Count == 0)
      {
         CreateRoomNode(new Vector2(200f,200f), roomNodeTypeList.list.Find(type => type.IsEntrance));
      }

      CreateRoomNode(mousePositionObject, roomNodeTypeList.list.Find(type => type.IsNone));
   }

   private void CreateRoomNode(object mousePositionObject, RoomNodeTypeSO roomNodeType)
   {
      Vector2 mousePosition = (Vector2)mousePositionObject;

      RoomNodeSO roomNode = ScriptableObject.CreateInstance<RoomNodeSO>();
      currentRoomNodeGraph.roomNodeList.Add(roomNode);

      roomNode.Initialise(new Rect(mousePosition, new Vector2(nodeWidth, nodeHeight)), currentRoomNodeGraph, roomNodeType);

      AssetDatabase.AddObjectToAsset(roomNode, currentRoomNodeGraph);
      AssetDatabase.SaveAssets();

      currentRoomNodeGraph.OnValidate();
   }

   private void SelectAllRoomNodes()
   {
      foreach (var roomNode in currentRoomNodeGraph.roomNodeList)
      {
         roomNode.IsSelected = true;
      }
      GUI.changed = true;
   }

   private void DeleteSelectedRoomNodeLinks()
   {
      foreach (var roomNode in currentRoomNodeGraph.roomNodeList)
      {
         if (roomNode.IsSelected && roomNode.childRoomNodeIds.Count > 0)
         {
            for (int i = roomNode.childRoomNodeIds.Count-1; i >= 0; i--)
            {
               RoomNodeSO childRoomNode = currentRoomNodeGraph.GetRoomNodeById(roomNode.childRoomNodeIds[i]);

               if (childRoomNode != null && childRoomNode.IsSelected)
               {
                  roomNode.RemoveChildNodeIdFromRoomNode(childRoomNode.id);

                  childRoomNode.RemoveParentNodeIdFromRoomNode(roomNode.id);
               }
            }
         }
      }

      ClearAllSelectedRoomNodes();
   }

   private void DeleteSelectedRoomNodes()
   {
      Queue<RoomNodeSO> roomNodeDeletionQueue = new Queue<RoomNodeSO>();

      foreach (var roomNode in currentRoomNodeGraph.roomNodeList)
      {
         if (roomNode.IsSelected && !roomNode.roomNodeType.IsEntrance)
         {
            roomNodeDeletionQueue.Enqueue(roomNode);

            foreach (var childRoomNodeId in roomNode.childRoomNodeIds)
            {
               RoomNodeSO childRoomNode = currentRoomNodeGraph.GetRoomNodeById(childRoomNodeId);

               if (childRoomNode != null)
               {
                  childRoomNode.RemoveParentNodeIdFromRoomNode(roomNode.id);
               }
            }

            foreach (var parentRoomNodeId in roomNode.parentRoomNodeIds)
            {
               RoomNodeSO parentRoomNode = currentRoomNodeGraph.GetRoomNodeById(parentRoomNodeId);

               if (parentRoomNode != null)
               {
                  parentRoomNode.RemoveChildNodeIdFromRoomNode(roomNode.id);
               }
            }
         }
      }

      while (roomNodeDeletionQueue.Count > 0)
      {
         var roomNodeToDelete = roomNodeDeletionQueue.Dequeue();

         currentRoomNodeGraph.roomNodeDictionary.Remove(roomNodeToDelete.id);
         currentRoomNodeGraph.roomNodeList.Remove(roomNodeToDelete);

         DestroyImmediate(roomNodeToDelete, true);
         AssetDatabase.SaveAssets();
      }
   }

   private void ClearLineDrag()
   {
      currentRoomNodeGraph.roomNodeToDrawLineFrom = null;
      currentRoomNodeGraph.linePosition = Vector2.zero;
      GUI.changed = true;
   }

   private void ClearAllSelectedRoomNodes()
   {
      foreach (var roomNode in currentRoomNodeGraph.roomNodeList)
      {
         if (roomNode.IsSelected)
         {
            roomNode.IsSelected = false;
            GUI.changed = true;
         }
      }
   }

   private void DrawRoomConnections()
   {
      foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
      {
         if (roomNode.childRoomNodeIds.Count > 0)
         {
            foreach (var childId in roomNode.childRoomNodeIds)
            {
               if (currentRoomNodeGraph.roomNodeDictionary.ContainsKey(childId))
               {
                  DrawConnectionLine(roomNode, currentRoomNodeGraph.roomNodeDictionary[childId]);
                  GUI.changed = true;
               }
            }
         }
      }
   }

   private void DrawConnectionLine(RoomNodeSO parentRoomNode, RoomNodeSO childRoomNode)
   {
      Vector2 startPosition = parentRoomNode.rect.center;
      Vector2 endPosition = childRoomNode.rect.center;

      Vector2 midPosition = (startPosition + endPosition) / 2f;
      Vector2 direction = endPosition - startPosition;

      Vector2 arrowTailPoint1 = midPosition - new Vector2(-direction.y, direction.x).normalized * connectingLineArrowSize;
      Vector2 arrowTailPoint2 = midPosition + new Vector2(-direction.y, direction.x).normalized * connectingLineArrowSize;
      Vector2 arrowHeadPoint = midPosition + direction.normalized * connectingLineArrowSize;

      Handles.DrawBezier(arrowHeadPoint, arrowTailPoint1, arrowHeadPoint, arrowTailPoint1, Color.white, null, connectingLineWidth);
      Handles.DrawBezier(arrowHeadPoint, arrowTailPoint2, arrowHeadPoint, arrowTailPoint2, Color.white, null, connectingLineWidth);

      Handles.DrawBezier(startPosition, endPosition, startPosition, endPosition, Color.white, null, connectingLineWidth);

      GUI.changed = true;
   }

   private void InspectorSelectionChanged()
   {
      RoomNodeGraphSO roomNodeGraph = Selection.activeObject as RoomNodeGraphSO;

      if (roomNodeGraph != null)
      {
         currentRoomNodeGraph = roomNodeGraph;
         GUI.changed = true;
      }
   }
}

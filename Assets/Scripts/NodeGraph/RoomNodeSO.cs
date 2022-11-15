using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class RoomNodeSO : ScriptableObject
{
   [HideInInspector] public string id;
   [HideInInspector] public List<string> parentRoomNodeIds = new List<string>();
   [HideInInspector] public List<string> childRoomNodeIds = new List<string>();
   [HideInInspector] public RoomNodeGraphSO roomNodeGraph;
   public RoomNodeTypeSO roomNodeType;
   [HideInInspector] public RoomNodeTypeListSO roomNodeTypeList;

   #region Editor Code

#if UNITY_EDITOR

   [HideInInspector] public Rect rect;
   [HideInInspector] public bool IsLeftClickDragging = false;
   [HideInInspector] public bool IsSelected = false;

   public void Initialise(Rect rect, RoomNodeGraphSO roomNodeGraph, RoomNodeTypeSO roomNodeType)
   {
      this.rect = rect;
      this.id = Guid.NewGuid().ToString();
      this.name = "RoomNode";
      this.roomNodeGraph = roomNodeGraph;
      this.roomNodeType = roomNodeType;

      roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
   }

   public void Draw(GUIStyle roomNodeStyle)
   {
      GUILayout.BeginArea(rect, roomNodeStyle);

      EditorGUI.BeginChangeCheck();

      int selected = roomNodeTypeList.list.FindIndex(type => type == roomNodeType);
      int selection = EditorGUILayout.Popup("", selected, GetRoomNodeTypesToDisplay());

      roomNodeType = roomNodeTypeList.list[selection];

      if(EditorGUI.EndChangeCheck())
      {
         EditorUtility.SetDirty(this);
      }

      GUILayout.EndArea();
   }

   private string[] GetRoomNodeTypesToDisplay()
   {
      string[] roomArray = new string[roomNodeTypeList.list.Count];

      for(int i = 0; i < roomNodeTypeList.list.Count; i++)
      {
         if(roomNodeTypeList.list[i].DisplayInNodeGraphEditor)
         {
            roomArray[i] = roomNodeTypeList.list[i].RoomNodeTypeName;
         }
      }

      return roomArray;
   }

   public void ProcessEvents(Event currentEvent)
   {
      switch(currentEvent.type)
      {
         case EventType.MouseDown:
            ProcessMouseDownEvent(currentEvent);
            break;
         case EventType.MouseUp:
            ProcessMouseUpEvent(currentEvent);
            break;
         case EventType.MouseDrag:
            ProcessMouseDragEvent(currentEvent);
            break;
         default:
            break;
      }
   }

   private void ProcessMouseDownEvent(Event currentEvent)
   {
      if (currentEvent.button == 0)
      {
         ProcessLeftClickDownEvent();
      }
   }
   private void ProcessLeftClickDownEvent()
   {
      Selection.activeObject = this;
      IsSelected = !IsSelected;
   }

   private void ProcessMouseUpEvent(Event currentEvent)
   {
      if (currentEvent.button == 0)
      {
         ProcessLeftClickUpEvent();
      }
   }
   private void ProcessLeftClickUpEvent()
   {
      if (IsLeftClickDragging)
      {
         IsLeftClickDragging = false;
      }
   }

   private void ProcessMouseDragEvent(Event currentEvent)
   {
      if (currentEvent.button == 0)
      {
         ProcessLeftClickDragEvent(currentEvent);
      }
   }
   private void ProcessLeftClickDragEvent(Event currentEvent)
   {
      IsLeftClickDragging = true;

      DragNode(currentEvent.delta);
      GUI.changed = true;
   }

   private void DragNode(Vector2 delta)
   {
      rect.position += delta;
      EditorUtility.SetDirty(this);
   }


#endif
   #endregion
}

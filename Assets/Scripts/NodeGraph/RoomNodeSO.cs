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

      if (parentRoomNodeIds.Count > 0 || roomNodeType.IsEntrance)
      {
         EditorGUILayout.LabelField(roomNodeType.RoomNodeTypeName);
      } else
      {
         int selected = roomNodeTypeList.list.FindIndex(type => type == roomNodeType);
         int selection = EditorGUILayout.Popup("", selected, GetRoomNodeTypesToDisplay());

         roomNodeType = roomNodeTypeList.list[selection];

         if (roomNodeTypeList.list[selected].IsCorridor && !roomNodeTypeList.list[selection].IsCorridor ||
            !roomNodeTypeList.list[selected].IsCorridor && roomNodeTypeList.list[selection].IsCorridor ||
            !roomNodeTypeList.list[selected].IsBossRoom && roomNodeTypeList.list[selection].IsBossRoom)
         {
            if (childRoomNodeIds.Count > 0)
            {
               for (int i = childRoomNodeIds.Count-1; i >= 0; i--)
               {
                  RoomNodeSO childRoomNode = roomNodeGraph.GetRoomNodeById(childRoomNodeIds[i]);

                  if (childRoomNode != null)
                  {
                     RemoveChildNodeIdFromRoomNode(childRoomNode.id);

                     childRoomNode.RemoveParentNodeIdFromRoomNode(id);
                  }
               }
            }
         }
      }

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
      else if (currentEvent.button == 1)
      {
         ProcessRightClickDownEvent(currentEvent);
      }
   }

   private void ProcessLeftClickDownEvent()
   {
      Selection.activeObject = this;
      IsSelected = !IsSelected;
   }

   private void ProcessRightClickDownEvent(Event currentEvent)
   {
      roomNodeGraph.SetNodeToDrawConnectionLineFrom(this, currentEvent.mousePosition);
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

   public void DragNode(Vector2 delta)
   {
      rect.position += delta;
      EditorUtility.SetDirty(this);
   }

   public bool AddChildRoomNodeIdToRoomNode(string childId)
   {
      if (IsChildRoomValid(childId))
      {
         childRoomNodeIds.Add(childId);
         return true;
      }

      return false;
   }

   public bool AddParentRoomNodeIdToRoomNode(string parentId)
   {
      parentRoomNodeIds.Add(parentId);
      return true;
   }

   public bool RemoveChildNodeIdFromRoomNode(string childId)
   {
      if (childRoomNodeIds.Contains(childId))
      {
         childRoomNodeIds.Remove(childId);
         return true;
      }
      return false;
   }

   public bool RemoveParentNodeIdFromRoomNode(string parentId)
   {
      if (parentRoomNodeIds.Contains(parentId))
      {
         parentRoomNodeIds.Remove(parentId);
         return true;
      }
      return false;
   }

   #region Validation
   private bool IsChildRoomValid(string childId)
   {
      bool isConnectedBossNodeAlready = false;
      var child = roomNodeGraph.GetRoomNodeById(childId);

      foreach (var roomNode in roomNodeGraph.roomNodeList)
      {
         if (roomNode.roomNodeType.IsBossRoom && roomNode.parentRoomNodeIds.Count > 0)
         {
            isConnectedBossNodeAlready = true;
         }
      }

      if (child.roomNodeType.IsBossRoom && isConnectedBossNodeAlready)
      {
         return false;
      }

      if (child.roomNodeType.IsNone)
      {
         return false;
      }

      if (childRoomNodeIds.Contains(childId))
      {
         return false;
      }

      if (id == childId)
      {
         return false;
      }

      if (parentRoomNodeIds.Contains(childId))
      {
         return false;
      }

      if (child.parentRoomNodeIds.Count > 0)
      {
         return false;
      }

      if (roomNodeType.IsCorridor && child.roomNodeType.IsCorridor)
      {
         return false;
      }

      if (!roomNodeType.IsCorridor && !child.roomNodeType.IsCorridor)
      {
         return false;
      }

      if (child.roomNodeType.IsCorridor && childRoomNodeIds.Count >= Settings.maxChildCorridors)
      {
         return false;
      }

      if (child.roomNodeType.IsEntrance)
      {
         return false;
      }

      if (!child.roomNodeType.IsCorridor && childRoomNodeIds.Count > 0)
      {
         return false;
      }

      return true;
   }
   #endregion

#endif
   #endregion
}

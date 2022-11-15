using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomNodeType", menuName = "Scriptable Objects/Dungeon/Room Node Type")]
public class RoomNodeTypeSO : ScriptableObject
{
   public string RoomNodeTypeName;

   public bool DisplayInNodeGraphEditor = true;
   public bool IsCorridor;
   public bool IsCorridorNS;
   public bool IsCorridorEW;
   public bool IsEntrance;
   public bool IsBossRoom;
   public bool IsNone;

   private void OnValidate()
   {
      HelperUtilities.ValidateEmptyString(this, nameof(RoomNodeTypeName), RoomNodeTypeName);
   }
}

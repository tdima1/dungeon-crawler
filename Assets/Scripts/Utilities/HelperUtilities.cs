using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HelperUtilities
{
   public static bool ValidateEmptyString(Object obj, string fieldName, string stringToCheck)
   {
      if(string.IsNullOrEmpty(stringToCheck))
      {
         Debug.Log($"{fieldName} is empty and must contain a value in object {obj.name.ToString()}.");
         return true;
      }
      return false;
   }

   public static bool ValidateEnumerableValues(Object obj, string fieldName, IEnumerable enumerableObjectToCheck)
   {
      bool error = false;
      int count = 0;

      foreach(var item in enumerableObjectToCheck)
      {
         if(item == null)
         {
            Debug.Log($"{fieldName} has null values in object {obj.name.ToString()}");
            error = true;
         }
         else
         {
            count++;
         }
      }

      if(count == 0)
      {
         Debug.Log($"{fieldName} has no values in object {obj.name.ToString()}");
         error = true;
      }

      return error;
   }
}

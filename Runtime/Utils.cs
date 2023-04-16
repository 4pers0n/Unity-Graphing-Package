using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace EvanZ.Tools
{
    public class Utils
    {
        public static float GetAngleFromVectorFloat(Vector2 direction)
        {
            float rad = Mathf.Atan(direction.y / direction.x);
            return Mathf.Rad2Deg * rad;
        }
    }
}
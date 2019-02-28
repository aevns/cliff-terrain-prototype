using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Just some methods used commonly in this project.
static class Vector3Extensions
{
    public static Vector3 Abs(this Vector3 vec)
    {
        return new Vector3(Mathf.Abs(vec.x), Mathf.Abs(vec.y), Mathf.Abs(vec.z));
    }
    public static Vector3 DivideBy(this Vector3 a, Vector3 b)
    {
        return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
    }
}
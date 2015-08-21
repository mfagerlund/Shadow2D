using UnityEngine;
using System.Collections;

public class TrackMouse : MonoBehaviour
{
    public void Update()
    {
        Vector2 v = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 p = new Vector3(v.x, v.y, transform.position.z);
        transform.position = p;
    }
}

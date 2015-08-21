using UnityEngine;
using System.Collections;
using System.Linq;

public class MoveBetween : MonoBehaviour
{
    public GameObject[] targets;
    public float speed = 20;
    public float acceleration = 10;
    public IEnumerator Start()
    {
        if (targets.Any())
        {
            while (true)
            {
                // The ToArray is to make us safe from changes to the targets array by some external force of evil,
                // during the foreaching.
                foreach (GameObject target in targets.ToArray())
                {
                    //float time = distance/speed;
                    float tspeed = 0;
                    while (true)
                    {
                        float distance = (transform.position - target.transform.position).magnitude;
                        if (distance < 0.001)
                        {
                            break;
                        }

                        transform.position = Vector3.MoveTowards(transform.position, target.transform.position, tspeed * Time.deltaTime);
                        tspeed = Mathf.MoveTowards(tspeed, speed, acceleration * Time.deltaTime);
                        yield return null;
                    }
                }
            }
        }
    }
}

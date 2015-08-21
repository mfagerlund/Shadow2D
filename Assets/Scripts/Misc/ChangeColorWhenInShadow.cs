using System.Collections.Generic;
using Shadow2D;
using UnityEngine;

public class ChangeColorWhenInShadow : MonoBehaviour
{
    public List<Shadow2DLight> lightSources;
    private SpriteRenderer _spriteRenderer;

    public void Start()
    {

        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Update()
    {
        bool lit = false;
        foreach (Shadow2DLight shadow2DLight in lightSources)
        {
            lit = shadow2DLight.GetIsPointInLight(transform.position);
            if (lit)
            {
                break;
            }
        }

        _spriteRenderer.color = lit ? Color.white : Color.red;
    }
}
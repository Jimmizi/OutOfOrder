using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleSpriteAnim : MonoBehaviour
{
    SpriteRenderer _renderer;
    SpriteRenderer renderer
    {
        get
        {
            if (!_renderer) { _renderer = GetComponent<SpriteRenderer>(); }
            return _renderer;
        }
    }

    float animationFrame;

    [SerializeField]
    float animationSpeed;

    [SerializeField]
    List<Sprite> frames;

    void Update()
    {
        animationFrame += animationSpeed * Time.deltaTime;
        if(animationFrame >= frames.Count)
        {
            animationFrame -= frames.Count;
        }

        if (renderer && frames != null && frames.Count > 0)
        {
            int frameIndex = ((int)animationFrame) % frames.Count;
            renderer.sprite = frames[frameIndex];
        }
    }
}

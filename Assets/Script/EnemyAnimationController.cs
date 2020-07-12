using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAnimationController : MonoBehaviour
{
    GridEnemy _enemy;
    GridEnemy enemy
    {
        get
        {
            if(!_enemy) { _enemy = GetComponent<GridEnemy>(); }
            return _enemy;
        }
    }

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
    List<Sprite> IdleFrames;

    [SerializeField]
    List<Sprite> MoveFrames;
    
    void Update()
    {
        animationFrame += animationSpeed * Time.deltaTime;

        List<Sprite> currentAnim = IdleFrames;

        if (enemy && enemy.IsMoving)
        {
            currentAnim = MoveFrames;
        }

        if (renderer && currentAnim != null && currentAnim.Count > 0)
        {
            int frameIndex = ((int)animationFrame) % currentAnim.Count;
            renderer.sprite = currentAnim[frameIndex];
        }
    }
}

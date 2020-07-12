using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UITextMoveUpDown : MonoBehaviour
{
    public Vector3 PositionOne;
    public Vector3 PositionTwo;

    public Vector3 PositionOne_Shadow;
    public Vector3 PositionTwo_Shadow;

    public RectTransform ShadowRectTransform;

    public float TimeBetweenPositionChange;
    private float posChangeTimer = 0f;

    private RectTransform rTransform;

    // Start is called before the first frame update
    void Start()
    {
        rTransform = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        posChangeTimer += GameConfig.GetDeltaTime();
        if (posChangeTimer >= TimeBetweenPositionChange)
        {
            posChangeTimer = 0f;
            if (transform.localPosition == PositionOne)
            {
                rTransform.localPosition = PositionTwo;
                ShadowRectTransform.localPosition = PositionTwo_Shadow;
            }
            else
            {
                rTransform.localPosition = PositionOne;
                ShadowRectTransform.localPosition = PositionOne_Shadow;
            }
        }

    }
}

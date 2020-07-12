using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIHideAfterCount : MonoBehaviour
{
    public float TimeToHide;
    public List<Text> TextToHide = new List<Text>();

    private float timer;

    // Start is called before the first frame update
    void Start()
    {
        foreach (var text in TextToHide)
        {
            text.enabled = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        timer += GameConfig.GetDeltaTime();
        if (timer >= TimeToHide)
        {
            foreach (var text in TextToHide)
            {
                text.enabled = false;
            }

            Destroy(this);
        }
    }
}

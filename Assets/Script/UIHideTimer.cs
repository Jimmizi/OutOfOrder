using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIHideTimer : MonoBehaviour
{
    public float TimeToAppear;
    public List<Text> TextToShow = new List<Text>();

    private float timer;

    // Start is called before the first frame update
    void Start()
    {
        foreach (var text in TextToShow)
        {
            text.enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        timer += GameConfig.GetDeltaTime();
        if (timer >= TimeToAppear)
        {
            foreach (var text in TextToShow)
            {
                text.enabled = true;
            }

            Destroy(this);
        }
    }
}

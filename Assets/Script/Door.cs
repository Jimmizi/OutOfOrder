using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField]
    private bool _isOpen;

    [SerializeField]
    public Sprite _OpenSprite;

    [SerializeField]
    public Sprite _ClosedSprite;

    private SpriteRenderer _Renderer;

    public bool isOpen
    {
        get { return _isOpen; }
        set { if (value) Open(); else Close(); }
    }

    private SpriteRenderer Renderer
    {
        get
        {
            if (!_Renderer)
            {
                _Renderer = GetComponent<SpriteRenderer>();
            }

            return _Renderer;
        }
    }
    
    void Start()
    {
        if (Renderer)
        {
            Renderer.sprite = _isOpen ? _OpenSprite : _ClosedSprite;
        }
    }

    public void Open()
    {
        if (_isOpen)
            return;

        _isOpen = true;

        if (Renderer)
        {
            Renderer.sprite = _OpenSprite;
        }

    }

    public void Close()
    {
        if (!_isOpen)
            return;

        _isOpen = false;

        if (Renderer)
        {
            Renderer.sprite = _ClosedSprite;
        }
    }
}

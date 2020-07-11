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

    private Collider _Collider;

    public bool isOpen
    {
        get { return _isOpen; }
        set
        {
            if (_isOpen == value)
                return;

            _isOpen = value;

            if (Renderer)
            {
                Renderer.sprite = _isOpen ? _OpenSprite : _ClosedSprite;
            }

            if (Collider)
            {
                Collider.enabled = !_isOpen;
            }
        }
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

    private Collider Collider
    {
        get
        {
            if (!_Collider)
            {
                _Collider = GetComponent<Collider>();
            }

            return _Collider;
        }
    }

    void Start()
    {
        Service.Flow.ObjectsToDestroyOnLevelEnd.Add(this.gameObject);

        if (Renderer)
        {
            Renderer.sprite = _isOpen ? _OpenSprite : _ClosedSprite;
        }
    }
}

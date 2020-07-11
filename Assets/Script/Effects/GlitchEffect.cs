using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GlitchEffect : MonoBehaviour
{
    [SerializeField, Range(0.0f, 1.0f)]
    float _amount;

    [SerializeField]
    float _minGlitchChangeTime = 0.1f;

    [SerializeField]
    float _maxGlitchChangeTime = 0.5f;

    float _seedChangeTimer = 0.1f;
    float _seed;

    public Material material;

    // Postprocess the image
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        material.SetFloat("_Amount", _amount);
        material.SetFloat("_Seed", _seed);
        Graphics.Blit(source, destination, material);
    }

    private void Update()
    {
        _seedChangeTimer -= Time.deltaTime;
        if (_seedChangeTimer < 0)
        {
            _seedChangeTimer = Random.Range(_minGlitchChangeTime, _maxGlitchChangeTime);
            _seed = Random.value;
        }
    }
}
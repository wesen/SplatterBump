using System;
using UnityEngine;

public class SpriteSheetSwitcher : MonoBehaviour {
    public int Offset = 0;
    public Texture2D Texture;
    
    private Sprite[] _subSprites;
    private SpriteRenderer _renderer;
    
    void Start() {
        _renderer = GetComponentInChildren<SpriteRenderer>();
        _subSprites = Resources.LoadAll<Sprite>("Sprites/rabbit");
    }
    
    void LateUpdate() {
        for (int i = 0; i < Math.Min(Offset, _subSprites.Length); i++) {
            if (_subSprites[i].name == _renderer.sprite.name) {
                _renderer.sprite = _subSprites[i + Offset];
                break;
            }
        }
    }
}

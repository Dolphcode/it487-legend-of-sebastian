using System;
using UnityEngine;

namespace SQZL.Entity.VFX
{
    public class DamageVisualEffector : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRendererRef;
        [SerializeField] private int originalColorIdx = 0;
        [SerializeField] private int colorIdx = 0;

        private int lastColorIdx;

        private int _p_SwapIdx = 0;
        
        private MaterialPropertyBlock _matPropertyBlock;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // Initialize the material property block
            _matPropertyBlock = new MaterialPropertyBlock();
            _p_SwapIdx = Shader.PropertyToID("_SwapIdx");
            _matPropertyBlock.SetInt(_p_SwapIdx, colorIdx);
            spriteRendererRef.SetPropertyBlock(_matPropertyBlock);
            
            // Cache color idx
            lastColorIdx = colorIdx;
        }

        public void LateUpdate()
        {
            Debug.Log($"Current color idx {colorIdx}");
            if (colorIdx != lastColorIdx)
            {
                lastColorIdx = colorIdx;
                _matPropertyBlock.SetInt(_p_SwapIdx, colorIdx);
            }
        }
    }
}

using System;
using UnityEngine;

namespace SQZL.World.Interactable
{
    [RequireComponent(typeof(Collider2D))]
    public class StatuePressurePlate : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private SpriteRenderer _spriteRenderer;
        [SerializeField] private Sprite _unpressedTex;
        [SerializeField] private Sprite _pressedTex;
        [SerializeField] private StatueObject _statueObject;

        [Header("Config")] [SerializeField] private float liftDelay = 0.2f;

        private float liftDelayTimeLeft = 0f;
        private bool pressed = false;
        private bool playerOn = false;
        
        // Update is called once per frame
        void Update()
        {
            if (!playerOn && pressed)
            {
                liftDelayTimeLeft -= Time.deltaTime;
                if (liftDelayTimeLeft <= 0f)
                {
                    pressed = false;
                    _spriteRenderer.sprite = _unpressedTex;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                playerOn = true;
                if (!pressed)
                {
                    pressed = true;
                    _spriteRenderer.sprite = _pressedTex;
                    liftDelayTimeLeft = liftDelay;
                    _statueObject.TriggerTurnFace();
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                playerOn = false;
            } 
        }
    }
}

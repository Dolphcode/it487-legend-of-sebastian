using System.Collections;
using System;
using SQZL.Entity;
using SQZL.Entity.Player;
using UnityEngine;

namespace SQZL.World.Interactable
{
    public class StatueObject : MonoBehaviour, ITilebodyCollisionHandler
    {

        [Header("Initial Config")] [SerializeField]
        internal bool hasTorchFace = true;
        [SerializeField] private StatueObjectFace statueObjectFace = StatueObjectFace.PIGEON;
        [SerializeField] private PlayerEntityController _playerRef;

        internal event Action OnStatueFaceUpdate;
        
        private Animator _animator;
        private int _a_Turn;
        private int _a_HasTorchFace;
        
        internal StatueObjectFace CurrentFace { private set; get; }
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _animator = GetComponent<Animator>();
            _a_Turn = Animator.StringToHash("Turn");
            _a_HasTorchFace = Animator.StringToHash("HasTorchFace");
            _animator.SetBool(_a_HasTorchFace, hasTorchFace);
            
            // TODO: Maybe implement starting face if needed?
            CurrentFace = StatueObjectFace.PIGEON; // just default

        }

        public void TriggerTurnFace()
        {
            StartCoroutine(TurnCoroutine()); 
        }

        public void FaceTurnComplete()
        {
            _faceTurning = false;
        }

        private bool _faceTurning = false;
        private IEnumerator TurnCoroutine()
        {
            _animator.SetTrigger(_a_Turn);
            _faceTurning = true;
            _playerRef.SetAnimatorState(false, _playerRef.Facing);
            _playerRef.playerInputFrozen = true;
            while (_faceTurning)
            {
                yield return null;
            }
            CurrentFace = (StatueObjectFace)(((int)CurrentFace + 1) % 4);
            OnStatueFaceUpdate?.Invoke();
            _playerRef.playerInputFrozen = false;
        }

        public void OnCollideWithTilebody(Tilebody2D body)
        {
            if (!hasTorchFace)
            {
                hasTorchFace = true;
                _animator.SetBool(_a_HasTorchFace, true);
                OnStatueFaceUpdate?.Invoke();
            }
        }
    }

    internal enum StatueObjectFace
    {
        PIGEON, TORCH, GARGOYLE, RATKING
    }
}

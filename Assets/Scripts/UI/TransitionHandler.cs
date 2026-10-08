using UnityEngine;
using System;

namespace SQZL.UI
{
    public class TransitionHandler : MonoBehaviour
    {
        private Animator _animator;

        private int _a_TriggerSlide;

        public event Action OnSlideTransitionEnd;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _animator = GetComponent<Animator>();
            _a_TriggerSlide = Animator.StringToHash("TriggerSlide");
        }

        public void TriggerSlide()
        {
            Debug.Log($"{_animator} and {_a_TriggerSlide}");

            _animator.SetTrigger(_a_TriggerSlide);
        }

        void OnSlideEnd()
        {
            OnSlideTransitionEnd?.Invoke();
        }



    }
}

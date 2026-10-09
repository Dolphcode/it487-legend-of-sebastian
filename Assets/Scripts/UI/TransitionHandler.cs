using UnityEngine;
using System;

namespace SQZL.UI
{
    public class TransitionHandler : MonoBehaviour
    {
        private Animator _animator;

        private int _a_TriggerSlide;
        private int _a_TriggerClosed;

        public event Action OnSlideTransitionEnd;
        public event Action OnSlideClosed;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _animator = GetComponent<Animator>();
            _a_TriggerSlide = Animator.StringToHash("TriggerSlide");
            _a_TriggerClosed = Animator.StringToHash("TriggerClose");
        }

        public void TriggerSlide()
        {
            Debug.Log($"{_animator} and {_a_TriggerSlide}");

            _animator.SetTrigger(_a_TriggerSlide);
        }

        public void TriggerCloseStep()
        {
            _animator.SetTrigger(_a_TriggerClosed);
        }

        void OnSlideEnd()
        {
            OnSlideTransitionEnd?.Invoke();
        }

        void OnCloseEnd()
        {
            OnSlideClosed?.Invoke();
        }



    }
}

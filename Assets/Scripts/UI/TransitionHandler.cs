using UnityEngine;

namespace SQZL.UI
{
    public class TransitionHandler : MonoBehaviour
    {
        private Animator _animator;

        private int _a_TriggerSlide;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _animator = GetComponent<Animator>();
            _a_TriggerSlide = Animator.StringToHash("TriggerSlide");
        }

        public void TriggerSlide()
        {
            _animator.SetTrigger(_a_TriggerSlide);
        }

        void OnSlideEnd()
        {

        }



    }
}

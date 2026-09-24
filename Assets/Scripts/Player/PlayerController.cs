using Math = System.Math;
using MidpointRounding = System.MidpointRounding;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerController : MonoBehaviour
    {

        [Header("Movement Config")] [SerializeField]
        private float moveSpeed = 5f;
        
        
        // On Start actions
        private InputAction moveAction;  
        
        // On Start components
        private Rigidbody2D rigidbody2D;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // Get all components
            rigidbody2D = GetComponent<Rigidbody2D>();
            
            // Actions
            moveAction = InputSystem.actions.FindAction("Move");
        }

        // Fixed Update is called once per physics frame
        void FixedUpdate()
        {
            /*
             * 1. Vertical movement evaluates before horizontal. If holding UP/LEFT you will go all the way up first and then when you collide with the wall you will start going left.
             * 2. 
             */
            
            // Get the movement input value and round it to integral values
            Vector2 moveInputValue = moveAction.ReadValue<Vector2>();
            int xIn = (int)Math.Round(moveInputValue.x, MidpointRounding.AwayFromZero);
            int yIn = (int)Math.Round(moveInputValue.y, MidpointRounding.AwayFromZero);
            Debug.Log(xIn + " " + yIn);

            // Evaluate vertical movement
            if (yIn != 0)
            {
                float delta = moveSpeed * Time.fixedDeltaTime;
                float a = Mathf.Round(rigidbody2D.position.x) - rigidbody2D.position.x;
                float b = Mathf.Max(delta - Mathf.Abs(a), 0f) * yIn;
                float remainderMove = Mathf.Min(Mathf.Abs(a), delta) * Mathf.Sign(a);
                Debug.Log($"a:{a}, b:{b}, remainderMove:{remainderMove}");
                rigidbody2D.MovePosition(rigidbody2D.position + new Vector2(remainderMove, b));
            } else if (xIn != 0)
            {
                float delta = moveSpeed * Time.fixedDeltaTime;
                float a = Mathf.Round(rigidbody2D.position.y) - rigidbody2D.position.y;
                float b = Mathf.Max(delta - Mathf.Abs(a), 0f) * xIn;
                float remainderMove = Mathf.Min(Mathf.Abs(a), delta) * Mathf.Sign(a);
                rigidbody2D.MovePosition(rigidbody2D.position + new Vector2(b, remainderMove));
            }
        }
    }
}

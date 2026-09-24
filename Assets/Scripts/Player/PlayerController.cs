using System;
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

        [SerializeField] [Range(0f, 1.0e-4f)] private float blockTestThreshold = 1.0e-5f;
        [SerializeField] private float positionSnapThreshold = 1f/32f;
        [SerializeField] private float motionBias = 0.1f;
        
        // On Start actions
        private InputAction moveAction;  
        
        // On Start components
        private Rigidbody2D rigidbody2D;
        
        // State variables
        private float currY = 0f, expectedY = 0f;
        private int prevYIn = 0;
        private bool snappedToGridFlag = false;
        private Vector2 lastPosition, deltaPosition;
        
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // Get all components
            rigidbody2D = GetComponent<Rigidbody2D>();
            
            // Actions
            moveAction = InputSystem.actions.FindAction("Move");
            
            // Save the current y position
            currY = rigidbody2D.position.y;
            lastPosition = rigidbody2D.position;
            expectedY = rigidbody2D.position.y;
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
            
            // Save the current y position and shift the y position from fixed update at the previous frame
            // The purpose of this is to take the change in y from the previous frame to the current after
            // collision is resolved (which presumably occurs after FixedUpdate? may need to verify this
            // We also compute the delta position between frames to apply a motion bias and save the last position at this frame
            deltaPosition = rigidbody2D.position - lastPosition;
            Debug.Log(deltaPosition);
            lastPosition = rigidbody2D.position;
            currY = rigidbody2D.position.y;

            // Evaluate vertical movement first
            // Skip to horizontal if we are being blocked vertically by a wall essentially and both a vertical and horizontal input are being applied
            // We only perform the block test if we were pressing a y input in the previous frame of course
            // I realized I needed to distinguish between being blocked up or down
            Vector2 newPosition = rigidbody2D.position;
            float delta = moveSpeed * Time.fixedDeltaTime;
            bool vBlocked = ((expectedY - currY > blockTestThreshold && prevYIn == 1) ||
                             (expectedY - currY < -blockTestThreshold && prevYIn == -1));
            if (yIn != 0 && !vBlocked)
            {
                float a = Mathf.Round(rigidbody2D.position.x + motionBias * Mathf.Sign(deltaPosition.x)) - rigidbody2D.position.x;
                float b = Mathf.Max(delta - Mathf.Abs(a), 0f) * yIn;
                
                // The correction component (a component) for grid snapping
                float remainderMove = Mathf.Min(Mathf.Abs(a), delta) * Mathf.Sign(a);

                newPosition = rigidbody2D.position + new Vector2(remainderMove, b);
                rigidbody2D.MovePosition(newPosition);
            } else if (xIn != 0) // Then horizontal
            {
                float a;
                if (yIn != 0)
                    a = Mathf.Round(rigidbody2D.position.y) - rigidbody2D.position.y;
                else
                    a = Mathf.Round(rigidbody2D.position.y + motionBias * Mathf.Sign(deltaPosition.y)) - rigidbody2D.position.y;
                float b = Mathf.Max(delta - Mathf.Abs(a), 0f) * xIn;

                float remainderMove = Mathf.Min(Mathf.Abs(a), delta) * Mathf.Sign(a);

                newPosition = rigidbody2D.position + new Vector2(b, remainderMove);
                rigidbody2D.MovePosition(newPosition);
            }
            
            // At slow speeds, there is a bug that occurs likely due to the overlapping of identically
            // sized single tile hitboxes. To reduce this I implement an additional, more aggressive positional
            // grid snap so that hitboxes align correctly in this micropixel case scenario
            Vector2 snappedPosition = newPosition;
            if (Mathf.Abs(newPosition.x - Mathf.Round(newPosition.x)) < positionSnapThreshold)
                snappedPosition.x =  Mathf.Round(newPosition.x);
            if (Mathf.Abs(newPosition.y - Mathf.Round(newPosition.y)) < positionSnapThreshold)
                snappedPosition.y =  Mathf.Round(newPosition.y);
            rigidbody2D.MovePosition(snappedPosition);
            
            // Save what the y should be without collision resolution
            expectedY = snappedPosition.y;
            
            // Save this yIn as the previous yIn
            prevYIn = yIn;
        }
    }
}

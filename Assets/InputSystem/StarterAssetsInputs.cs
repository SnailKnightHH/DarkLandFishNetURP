using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    public class StarterAssetsInputs : MonoBehaviour
    {
        [Header("Character Input Values")]
        public Vector2 move;
        public Vector2 look;
        public bool jump;
        public bool sprint;
        public bool interact;
        public bool interact2;
        public bool attack;
        public bool throwItem;
        public bool build;

        [Header("Movement Settings")]
        public bool analogMovement;

        [Header("Mouse Cursor Settings")]
        public bool cursorLocked = true;
        public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM

        public Action<bool, bool> NetworkAudioWalkRunAction;

        public void OnMove(InputValue value)
        {
            MoveInput(value.Get<Vector2>());
        }

        public void OnLook(InputValue value)
        {
            if (cursorInputForLook)
            {
                LookInput(value.Get<Vector2>());
            }
        }

        public void OnJump(InputValue value)
        {
            JumpInput(value.isPressed);
        }

        public void OnSprint(InputValue value)
        {
            SprintInput(value.isPressed);
        }

        public void OnInteract(InputValue value)
        {
            InteractInput(value.isPressed);
        }

        public void OnInteract2(InputValue value)
        {
            Interact2Input(value.isPressed);
        }

        public void OnAttack(InputValue value)
        {
            AttackInput(value.isPressed);
        }

        public void OnThrowItem(InputValue value)
        {
            ThrowItemInput(value.isPressed);
        }

        public void OnBuild(InputValue value)
        {
            BuildInput(value.isPressed);
        }
#endif


        public void MoveInput(Vector2 newMoveDirection)
        {
            move = newMoveDirection;
            NetworkAudioWalkRunAction?.Invoke(move != Vector2.zero, sprint);
        }

        public void LookInput(Vector2 newLookDirection)
        {
            look = newLookDirection;
        }

        public void JumpInput(bool newJumpState)
        {
            jump = newJumpState;
        }

        public void SprintInput(bool newSprintState)
        {
            sprint = newSprintState;
            NetworkAudioWalkRunAction?.Invoke(move != Vector2.zero, sprint);
        }

        public void InteractInput(bool newInteractState)
        {
            interact = newInteractState;
        }

        public void Interact2Input(bool newInteractState)
        {
            interact2 = newInteractState;
        }

        public void AttackInput(bool newInteractState)
        {
            attack = newInteractState;
        }

        public void ThrowItemInput(bool newInteractState)
        {
            throwItem = newInteractState;
        }

        public Action BuildButtonReleased;

        public void BuildInput(bool newInteractState)
        {
            build = newInteractState;
            if (newInteractState == false)
            {
                BuildButtonReleased?.Invoke();
            }
        }

        //public void ItemSwitchInput(int newIdx) // 1-based
        //{
        //    selectedItemIdx = newIdx - 1;
        //}

        private void OnApplicationFocus(bool hasFocus)
        {
            SetCursorState(cursorLocked);
        }

        public bool newState = true;

        public void SetCursorState(bool newState)
        {
            Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
            //Cursor.lockState = CursorLockMode.None; // tmep
        }
    }

}
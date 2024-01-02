using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using StarterAssets;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static EnvironmentalHazardsManager;
using Unity.VisualScripting;
using static AudioManager;

public class Player : Character, ITrackable
{
    [SerializeField] private Transform _eyeTransform;
    public Transform eyeTransform
    {
        get { return _eyeTransform; }
        private set { _eyeTransform = value; }
    }
    [field: SerializeField]
    public Transform cameraTransform
    {
        get; private set;
    }

    [SerializeField] private float playerInteractDistance = 0.01f;
    [HideInInspector] public GameObject objectToCarry;
    private GameObject objectToInteract;

    //public GameObject objectHeld { get; private set; }    
    [SerializeField] private Transform _carryMountPoint;
    [SerializeField] private Transform _defenseCarryMountPoint;
    public Transform CarryMountPoint { get { return _carryMountPoint; } }
    [SerializeField] private float sphereColliderRadius = 1f;

    private PlayerWeaponContext weaponContext;
    [SerializeField] private Iweapon Fist;
    private IEffectDecorator effectDecorator;

    public bool isUsingCraftingTable
    {
        get; set;
    }

    // Canvas
    [SerializeField] private Canvas PickupPrompt;
    [SerializeField] private Canvas CraftingTablePrompt;
    [SerializeField] private Canvas InventoryFullPrompt;
    [SerializeField] private Canvas InventoryPrompt;
    [SerializeField] private Image HealthBar;

    public void SetCraftingTablePromptState(bool ifEnable)
    {
        CraftingTablePrompt.enabled = ifEnable;
    }

    private List<Tuple<GameObject, int>> inventoryList;
    public List<Tuple<GameObject, int>> InventoryList
    {
        get
        {
            return inventoryList;
        }
    }
    private int _currentlyHeldIdx = 0;
    public int CurrentlyHeldIdx
    {
        get
        {
            return _currentlyHeldIdx;
        }
    }
    private const int ITEM_HELD_LIMIT = 10;

    [SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly)]
    public bool Grabbed;

    protected override int Damage { get => 20; } // not used for now, since player's damage is their weapons

    private AudioSource _audioSource;

    [Header("Player")]
    [Tooltip("Sprint speed bonus of the character in m/s")]
    public float SprintSpeedBonus = 3.0f;
    [Tooltip("Rotation speed of the character")]
    public float RotationSpeed = 1.0f;
    [Tooltip("Acceleration and deceleration")]
    public float SpeedChangeRate = 10.0f;

    [Space(10)]
    [Tooltip("The height the player can jump")]
    public float JumpHeight = 1.2f;
    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float Gravity = -15.0f;

    [Space(10)]
    [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
    public float JumpTimeout = 0.1f;
    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    public float FallTimeout = 0.15f;

    [Header("Player Grounded")]
    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    public bool Grounded = true;
    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.14f;
    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.5f;
    [Tooltip("What layers the character uses as ground")]
    public LayerMask GroundLayers;

    [Header("Cinemachine")]
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject CinemachineCameraTarget;
    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 90.0f;
    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -90.0f;

    // cinemachine
    private float _cinemachineTargetPitch;

    // player
    private float _speed;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;

    // timeout deltatime
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;


#if ENABLE_INPUT_SYSTEM
    private PlayerInput _playerInput;
#endif
    private CharacterController _controller;
    public StarterAssetsInputs _input { get; private set; }

    public Animator _animator;

    private bool IsCurrentDeviceMouse
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
        }
    }

    public override void OnStartNetwork()
    {
#if UNITY_EDITOR
        Debug.Log("isOwner = " + base.Owner.IsLocalClient);
#endif
        //If this is not the owner, turn off player inputs
        if (!base.Owner.IsLocalClient) gameObject.GetComponent<PlayerInput>().enabled = false;
        if (base.Owner.IsLocalClient)
        {
            cameraTransform.AddComponent<AudioListener>();
            _audioSource = GetComponentInChildren<AudioSource>();
        }
        else
        {
            GameObject spawnPoint = GameObject.FindGameObjectWithTag("SpawnPoint");
            if (spawnPoint != null)
            {
                _controller = GetComponent<CharacterController>();
                _controller.enabled = false;
                transform.position = spawnPoint.transform.position;
                _controller.enabled = true;
            }
        }
        // Destroy menu camera when player enter game (only one camera should be active, which is on the player. Two camera cut performance in half.)
        Destroy(GameObject.Find("MenuCamera"));
    }

    // Currently used as "isUsingStorage" 
    [HideInInspector] public bool freezePlayerAndCameraMovement = false;
    //[HideInInspector] public Vector3 freezePosition;
    //[HideInInspector] public Vector3 lastCameraRotation;
    //public void FreezeOrUnfreezePlayerAndCameraMovement()
    //{
    //    if (freezePlayerAndCameraMovement)
    //    {
    //        transform.position = new Vector3(freezePosition.x, transform.position.y, freezePosition.x);            
    //    }        
    //}

    private float GetFinalMovementSpeed()
    {
        return effectDecorator.MovementSpeedEffect();
    }

    private void Awake()
    {
        Grabbed = false;
    }

    protected override void Start()
    {
        base.Start();
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<StarterAssetsInputs>();
        _input.BuildButtonReleased += BuildButtonReleasedAction;
#if ENABLE_INPUT_SYSTEM
        _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

        // reset our timeouts on start
        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
        Fist = transform.GetComponentInChildren<Iweapon>();
        weaponContext = new PlayerWeaponContext(Fist);
        effectDecorator = new PlayerBaseEffect(speed, Damage);
        rb = GetComponent<Rigidbody>();
        PickupPrompt.enabled = false;
        CraftingTablePrompt.enabled = false;
        InventoryFullPrompt.enabled = false;
        UpdateInventoryUI("Fist", "");
        isUsingCraftingTable = false;
        inventoryList = new List<Tuple<GameObject, int>>() { Tuple.Create<GameObject, int>(null, 0),
            Tuple.Create<GameObject, int>(null, 0),
            Tuple.Create<GameObject, int>(null, 0),
            Tuple.Create<GameObject, int>(null, 0) };
        Keyboard.current.MakeCurrent();
        IsBuilding = false;
        _input.NetworkAudioWalkRunAction += NetworkAudioWalkRunDelegate;
        myClientId = NetworkManager.ClientManager.Connection.ClientId;
    }

    private void Update()
    {
        if (!IsOwner || Grabbed) return; //If this is not the owner, skip Update()
        JumpAndGravity();
        GroundedCheck();
        Move();
        interact();
        PerformAction();
        UseTool();
        //ThrowItem();
        ItemSwitch();
        Untrap();
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    void OnDrawGizmos()
    {
        // Display the pathFindingDestination for debug purpose
        //Gizmos.color = Color.red;
        //Gizmos.DrawLine(cameraTransform.position, cameraTransform.forward * playerInteractDistance);
        //Gizmos.DrawLine(transform.position, transform.position + transform.forward * playerInteractDistance);
        //Gizmos.DrawLine(eyeTransform.position, eyeTransform.position + transform.forward * playerInteractDistance);
        //Gizmos.DrawWireSphere(eyeTransform.position, sphereColliderRadius);
        //Gizmos.DrawWireMesh(eyeTransform.position, Quaternion.identity);
    }

    private int myClientId;

    private void NetworkAudioWalkRunDelegate(bool isWalking, bool isSprinting)
    {
        if (isWalking && isSprinting)
        {
            AudioManager.Instance.PlayAudioContinuousNetwork(NetworkObject, SoundName.Run, true, myClientId);
        }
        else if (isWalking)
        {
            AudioManager.Instance.PlayAudioContinuousNetwork(NetworkObject, SoundName.Walk, true, myClientId);
        }
        else
        {
            AudioManager.Instance.PlayAudioContinuousNetwork(NetworkObject, SoundName.Walk, false, myClientId);
            AudioManager.Instance.PlayAudioContinuousNetwork(NetworkObject, SoundName.Run, false, myClientId);
        }
    }

    private void GroundedCheck()
    {
        Grounded = Utilities.GroundedCheck(transform, GroundedOffset, GroundedRadius, GroundLayers);
        //// set sphere position, with offset
        //Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
        //Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
    }

    private void CameraRotation()
    {
        if (freezePlayerAndCameraMovement) return;
        //Don't multiply mouse input by Time.deltaTime
        float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

        _cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
        _rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

        // clamp our pitch rotation
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        // Update Cinemachine camera target pitch
        CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

        // rotate the player left and right
        transform.Rotate(Vector3.up * _rotationVelocity);
    }

    public override void Move()
    {
        // set target speed based on move speed, sprint speed and if sprint is pressed
        float targetSpeed = _input.sprint ? GetFinalMovementSpeed() + SprintSpeedBonus : GetFinalMovementSpeed();
#if UNITY_EDITOR
        Debug.Log("targetSpeed: " + targetSpeed);
#endif
        // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

        // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is no input, set the target speed to 0
        if (_input.move == Vector2.zero) targetSpeed = 0.0f;

        // a reference to the players current horizontal velocity
        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

        // accelerate or decelerate to target speed
        if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            // creates curved result rather than a linear one giving a more organic speed change
            // note T in Lerp is clamped, so we don't need to clamp our speed
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

            // round speed to 3 decimal places
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        _animator.SetFloat("Speed", _speed);

        // normalise input direction
        Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

        // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is a move input rotate player when the player is moving
        if (_input.move != Vector2.zero)
        {
            // move
            inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
            if (_input.sprint)
            {
                AudioManager.Instance.PlayAudioContinuousLocal(_audioSource, SoundName.Run, NetworkManager.ClientManager.Connection.ClientId);
            }
            else
            {
                AudioManager.Instance.PlayAudioContinuousLocal(_audioSource, SoundName.Walk, NetworkManager.ClientManager.Connection.ClientId);
            }
        }

        // move the player
        if (!freezePlayerAndCameraMovement && !IsBuilding)
        {
            _controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        }
        else
        {
            _controller.Move(new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        }

    }

    private void JumpAndGravity()
    {
        if ((freezePlayerAndCameraMovement || IsBuilding) && _input.jump)
        {
            _input.jump = false;
            return;
        }
        if (Grounded)
        {
            // reset the fall timeout timer
            _fallTimeoutDelta = FallTimeout;

            // stop our velocity dropping infinitely when grounded
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }

            // Jump
            if (_input.jump && _jumpTimeoutDelta <= 0.0f && !freezePlayerAndCameraMovement && !IsBuilding)
            {
                // the square root of H * -2 * G = how much velocity needed to reach desired height
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
            }

            // jump timeout
            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            // reset the jump timeout timer
            _jumpTimeoutDelta = JumpTimeout;

            // fall timeout
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            if (_input.jump)
            {
                AudioManager.Instance.PlayAudioDiscrete(NetworkObject, AudioManager.SoundName.Jump);
            }

            // if we are not grounded, do not jump
            _input.jump = false;
        }

        // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += Gravity * Time.deltaTime;
        }
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    // Todo: Extract inventory to its own class
    public bool isInventorySlotEmpty(int idx)
    {
        return (inventoryList[idx].Item1 == null && inventoryList[idx].Item2 == 0);
    }

    public void UpdateInventorySlot(int idx, Tuple<GameObject, int> newTuple)
    {
        inventoryList[idx] = newTuple;
        if (inventoryList[idx].Item1 != null)
        {
            UpdateInventoryUI(inventoryList[idx].Item1.GetComponent<PickupableObject>().objectItem.ItemName, inventoryList[idx].Item2.ToString());
        }
    }

    public void AddOneToInventoryList(int idx) // Todo: maybe support overload that takes gameobject as argument
    {
        inventoryList[idx] = Tuple.Create(inventoryList[idx].Item1, inventoryList[idx].Item2 + 1);
        if (_currentlyHeldIdx == idx)
        {
            UpdateInventoryUI(inventoryList[idx].Item1.GetComponent<PickupableObject>().objectItem.ItemName, inventoryList[idx].Item2.ToString());
        }
    }

    private void MakeInventorySlotEmpty(int idx)
    {
        inventoryList[idx] = Tuple.Create<GameObject, int>(null, 0);
    }

    /// <summary>
    /// Calls MakeInventorySlotEmpty(), but also despawn the item
    /// </summary>
    public void DeleteInventorySlotItem(int idx)
    {
        // To prevent race condition where MakeInventorySlotEmpty() executes before server rpc finishes, use NetworkObjectId as the parameter
        DespawnObjectServerRpc(inventoryList[idx].Item1.GetComponent<NetworkObject>());
        MakeInventorySlotEmpty(idx);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DespawnObjectServerRpc(NetworkObject objectToDespawn)
    {
        base.Despawn(objectToDespawn);
    }

    /// <summary>
    /// Is used by other classes to determine if a player can pick up this object (predicate for is inventory full).
    /// Object can be picked up if return value is not -1.    
    /// </summary>
    /// <remarks>
    /// Behaviour: If player already has object A in inventoryList, then player can pick up ITEM_HELD_LIMIT - (num of A already owned) at this index.
    /// Otherwise, if there is an empty slot, that index will be returned.
    /// </remarks>
    /// <returns>
    /// Returns [availableIdx, halfOccupiedIdx]. If availableIdx is -1, not pickupable. Otherwise, if halfOccupiedIdx != -1, then both inventory[availableIdx] and inventory[halfOccupiedIdx] will change.
    /// </returns>
    public int[] DeterminePickupIdx(GameObject objectTobeHeld)
    {
        int halfOccupiedIdx = -1;
        int availableIdx = -1;
        for (int i = 0; i < inventoryList.Count; i++)
        {
            if (availableIdx == -1 && isInventorySlotEmpty(i))
            {
                availableIdx = i;
                continue;
            }
            if (!isInventorySlotEmpty(i)
                && inventoryList[i].Item1.GetComponent<PickupableObject>().objectItem.ItemName
                    == objectTobeHeld.GetComponent<PickupableObject>().objectItem.ItemName)
            {
                if (inventoryList[i].Item2 + objectTobeHeld.GetComponent<PickupableObject>().NumOfItem <= ITEM_HELD_LIMIT)
                {
                    return new int[] { i, -1 };
                }
                else // consider case: [5 0], and you have 6, it needs to be [10, 1]
                {
                    halfOccupiedIdx = i;
                }
            }
        }
        if (halfOccupiedIdx != -1 && availableIdx == -1) { return new int[] { -1, -1 }; }  // consider case: [5, 10], and you have 6 -> inventory full
        if (halfOccupiedIdx != -1 && availableIdx != -1)
        {
            // eg. [5, 0], numOfItem = 6, 6 - (10 - 5) = 1
            return new int[] { availableIdx, halfOccupiedIdx };
        }
        return new int[] { availableIdx, -1 };
    }

    /// <summary>
    /// Method overload for parameter type of Gameobject. For now only used by storage unit take out button.
    /// </summary>
    public int[] DeterminePickupIdx(Item objectTobeHeldItem)
    {
        int halfOccupiedIdx = -1;
        int availableIdx = -1;
        for (int i = 0; i < inventoryList.Count; i++)
        {
            if (availableIdx == -1 && isInventorySlotEmpty(i))
            {
                availableIdx = i;
                continue;
            }
            if (!isInventorySlotEmpty(i)
                && inventoryList[i].Item1.GetComponent<PickupableObject>().objectItem.ItemName
                    == objectTobeHeldItem.ItemName)
            {
                if (inventoryList[i].Item2 + 1 <= ITEM_HELD_LIMIT)
                {
                    return new int[] { i, -1 };
                }
                else
                {
                    halfOccupiedIdx = i;
                }
            }
        }
        if (halfOccupiedIdx != -1 && availableIdx == -1) { return new int[] { -1, -1 }; }
        if (halfOccupiedIdx != -1 && availableIdx != -1)
        {
            // eg. [5, 0], numOfItem = 6, 6 - (10 - 5) = 1
            return new int[] { availableIdx, halfOccupiedIdx };
        }
        return new int[] { availableIdx, -1 };
    }


    public void pickup(GameObject objectTobeHeld, int[] availableIdices = null, int NumOfItemOverride = -1)
    {
        // attach object to mount point and keep reference
        if (objectTobeHeld.GetComponent<PickupableObject>().isPickedUp) { return; }
        if (availableIdices == null)
        {
            availableIdices = DeterminePickupIdx(objectTobeHeld);
            if (availableIdices[0] == -1) { return; } // no empty space, cannot pickup
        }

        // NumOfItemOverride is for when spawning object, PickupableObject.NumOfItem has to go through server for update. 
        // By the time pickup method is reached, it's almost always the case that the update has not propogated back through the network yet.
        // Thus, if spawn method passes in NumOfItemOverride, use that instead.
        int NumOfItem = NumOfItemOverride == -1 ? objectTobeHeld.GetComponent<PickupableObject>().NumOfItem : NumOfItemOverride;

        Debug.Assert(availableIdices.Length == 2 && availableIdices[0] != -1, "Available index is -1 or method array parameter invalid, cannot pick up.");
        int availableIdx = availableIdices[0];
        int halfOccupiedIdx = availableIdices[1];

        GameObject previouslyHeldGO = inventoryList[availableIdx].Item1;

        if (halfOccupiedIdx != -1)
        {
            int newNumOfItem = NumOfItem - (ITEM_HELD_LIMIT - inventoryList[halfOccupiedIdx].Item2);
            // Do not use UpdateInventorySlot() since 1. have direct access to inventoryList, 2: do not have to update UI; Todo: when inventory gets refactored to its own class, point 1 is no longer true
            inventoryList[halfOccupiedIdx] = Tuple.Create(inventoryList[halfOccupiedIdx].Item1, ITEM_HELD_LIMIT);
            objectTobeHeld.GetComponent<PickupableObject>().UpdateNumberOfItemServerRpc(newNumOfItem);
        }

        inventoryList[availableIdx] = Tuple.Create(objectTobeHeld, NumOfItem + (isInventorySlotEmpty(availableIdx) ? 0 : inventoryList[availableIdx].Item2));
        inventoryList[availableIdx].Item1.GetComponent<PickupableObject>().PickUp(_carryMountPoint, cameraTransform, _defenseCarryMountPoint);
        if (objectTobeHeld.GetComponent<Defense>() != null && inventoryList[availableIdx].Item2 > 1)    // since we spawn additional defenses when we deploy them, we also need to despawn the extras upon pick up
        {
            base.Despawn(objectTobeHeld, DespawnType.Pool);
            Debug.Assert(previouslyHeldGO != null && previouslyHeldGO.GetComponent<Defense>() != null, "Previously held object has to be a defense as well.");
            inventoryList[availableIdx] = Tuple.Create(previouslyHeldGO, inventoryList[availableIdx].Item2);
        }
        if (availableIdx == _currentlyHeldIdx) // Don't hide object, update UI
        {
            UpdateInventoryUI(inventoryList[availableIdx].Item1.GetComponent<PickupableObject>().objectItem.ItemName,
                inventoryList[availableIdx].Item2.ToString());
        }
        else if (halfOccupiedIdx == _currentlyHeldIdx)
        {
            UpdateInventoryUI(inventoryList[halfOccupiedIdx].Item1.GetComponent<PickupableObject>().objectItem.ItemName,
                inventoryList[halfOccupiedIdx].Item2.ToString());
        }
        else // Hide object 
        {
            if (IsServer || IsHost)
            {
                SetMeshStatusClientRpc(inventoryList[availableIdx].Item1.gameObject.GetComponent<NetworkObject>(), false);
            }
            else
            {
                SetMeshStatusServerRpc(inventoryList[availableIdx].Item1.gameObject.GetComponent<NetworkObject>(), false);
            }
        }
        //isCarrying = true;
        if (inventoryList[availableIdx].Item1.GetComponent<Iweapon>() != null)
        {
            weaponContext.SetWeapon(inventoryList[availableIdx].Item1.GetComponent<Iweapon>(), transform);
        }


    }



    public void dropoff(bool destroyObject = false)
    {
        // unattach object
        if (isInventorySlotEmpty(_currentlyHeldIdx)) { return; }

        if (inventoryList[_currentlyHeldIdx].Item1.GetComponent<Iweapon>() != null)
        {
            weaponContext.SetWeapon(Fist, transform);
        }
        if (destroyObject)
        {
            inventoryList[_currentlyHeldIdx].Item1.GetComponent<PickupableObject>().DestroyNetworkObjectServerRpc();
            UpdateInventoryUI("Fist", "");
            MakeInventorySlotEmpty(_currentlyHeldIdx);
        }
        else if (inventoryList[_currentlyHeldIdx].Item1.GetComponent<Defense>() == null)  // Defenses can only be deployed and cannot be dropped off
        {
            inventoryList[_currentlyHeldIdx].Item1.GetComponent<PickupableObject>().Dropoff(inventoryList[_currentlyHeldIdx].Item2);
            UpdateInventoryUI("Fist", "");
            MakeInventorySlotEmpty(_currentlyHeldIdx);
        }


        //isCarrying = false;
    }

    private void ItemSwitch() // there must be a better way...
    {
        if (IsBuilding)
        {
            return;
        }
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            SwitchItemLogic(0);
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            SwitchItemLogic(1);
        }
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            SwitchItemLogic(2);
        }
        else if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            SwitchItemLogic(3);
        }
    }

    private void UpdateInventoryUI(string name, string count)
    {
        if (GetComponent<NetworkObject>().OwnerId == LocalConnection.ClientId)
        {
            InventoryPrompt.transform.GetChild(0).GetComponent<TMP_Text>().text = name;
            InventoryPrompt.transform.GetChild(1).GetComponent<TMP_Text>().text = count;
        }
        else
        {
            InventoryPrompt.enabled = false;
        }
    }

    private void SwitchItemLogic(int idx)
    {
        if (!isInventorySlotEmpty(_currentlyHeldIdx))
        {
            //inventoryList[_currentlyHeldIdx].Item1.GetComponent<PickupableObject>().DisableOrEnableMesh(false); // client can execute locally to minimize perceived delay
            if (IsServer || IsHost)
            {
                SetMeshStatusClientRpc(inventoryList[_currentlyHeldIdx].Item1.gameObject.GetComponent<NetworkObject>(), false);
            }
            else
            {
                SetMeshStatusServerRpc(inventoryList[_currentlyHeldIdx].Item1.gameObject.GetComponent<NetworkObject>(), false);
            }
        }
        _currentlyHeldIdx = idx;
        if (!isInventorySlotEmpty(_currentlyHeldIdx))
        {
            //inventoryList[_currentlyHeldIdx].Item1.GetComponent<PickupableObject>().DisableOrEnableMesh(true); // client can execute locally to minimize perceived delay
            if (IsServer || IsHost)
            {
                SetMeshStatusClientRpc(inventoryList[_currentlyHeldIdx].Item1.gameObject.GetComponent<NetworkObject>(), true);
            }
            else
            {
                SetMeshStatusServerRpc(inventoryList[_currentlyHeldIdx].Item1.gameObject.GetComponent<NetworkObject>(), true);
            }
            UpdateInventoryUI(inventoryList[_currentlyHeldIdx].Item1.GetComponent<PickupableObject>().objectItem.ItemName, inventoryList[_currentlyHeldIdx].Item2.ToString());
        }
        else
        {
            UpdateInventoryUI("Fist", "");
        }
    }

    [ServerRpc(RequireOwnership = false, RunLocally = true)]
    private void SetMeshStatusServerRpc(NetworkObject item, bool ifActive)
    {
        item.gameObject.GetComponent<PickupableObject>().DisableOrEnableMesh(ifActive);
        SetMeshStatusClientRpc(item, ifActive);
    }

    [ObserversRpc(BufferLast = true)]
    private void SetMeshStatusClientRpc(NetworkObject item, bool ifActive)
    {
        item.gameObject.GetComponent<PickupableObject>().DisableOrEnableMesh(ifActive);
    }

    public void SpawnItem(string itemName, Vector3 spawnPosition, Quaternion spawnRotation, int NumOfItem = 1, int[] availableIdices = null)
    {
        SpawnItemServerRpc(itemName, NumOfItem, availableIdices, spawnPosition, spawnRotation);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnItemServerRpc(string itemName, int NumOfItem, int[] availableIdices, Vector3 spawnPosition, Quaternion spawnRotation, NetworkConnection networkConnection = null)
    {
        Item item = SOManager.Instance.AllItemsNameToItemMapping[itemName];
        GameObject itemPrefab = SOManager.Instance.ItemPrefabMapping[item];

        Debug.Assert(NetworkManager.ObjectPool != null, "Object pool is null.");
        NetworkObject spawnedNetworkObject = NetworkManager.ObjectPool.RetrieveObject(itemPrefab.GetComponent<NetworkObject>().PrefabId, itemPrefab.GetComponent<NetworkObject>().SpawnableCollectionId, spawnPosition, spawnRotation, IsServer);
        spawnedNetworkObject.transform.root.GetComponent<PickupableObject>().UpdateNumberOfItemServerRpc(NumOfItem);
        base.Spawn(spawnedNetworkObject, networkConnection);
        // Todo: there is still a slight issue where spawned item will jiggle by a very small magnitude, reason unknown yet

        EquipClientWithItemTakenOutClientRpc(networkConnection, availableIdices, NumOfItem, spawnedNetworkObject);
    }

    [TargetRpc]
    private void EquipClientWithItemTakenOutClientRpc(NetworkConnection conn, int[] availableIdices, int NumOfItemOverride, NetworkObject spawnedItemNetworkObject)
    {
        GameObject pickUpObject = spawnedItemNetworkObject.gameObject;

        if (availableIdices != null && availableIdices[0] != -1)
        {
            pickup(pickUpObject, availableIdices, NumOfItemOverride);
        }
        else
        {
            int[] idices = DeterminePickupIdx(pickUpObject);
            if (idices[0] != -1)
            {
                pickup(pickUpObject, idices, NumOfItemOverride);
            }
            else
            {
                // Todo: Store in inventory
            }

        }
    }

    private void PerformAction()
    {
        if (_input.attack)
        {
            _input.attack = false;
            if (isUsingCraftingTable) { return; } // Damn... spent forever to figure out this bug, make sure this if is within if (_input.attack) and reset _input.attack = false regardless of any condition
            if (!isInventorySlotEmpty(_currentlyHeldIdx) && inventoryList[_currentlyHeldIdx].Item1.GetComponent<Tool>() != null)
            {
                // meaning player wants to use tool instead of attack
                return;
            }
            if (!isInventorySlotEmpty(_currentlyHeldIdx) && inventoryList[_currentlyHeldIdx].Item1.GetComponent<Defense>() != null)
            {
                if (!inventoryList[_currentlyHeldIdx].Item1.GetComponent<Defense>().Deploy()) { return; }

                int defenseHeldNumber = inventoryList[_currentlyHeldIdx].Item2;
                inventoryList[_currentlyHeldIdx].Item1.GetComponent<Defense>().Dropoff(1);
                if (defenseHeldNumber > 1)
                {
                    string itemName = inventoryList[_currentlyHeldIdx].Item1.GetComponent<PickupableObject>().objectItem.ItemName;
                    MakeInventorySlotEmpty(CurrentlyHeldIdx);
                    SpawnItem(itemName, _carryMountPoint.position, cameraTransform.rotation, defenseHeldNumber - 1, new int[] { _currentlyHeldIdx, -1 }); // create a new one and pick it up
                }
                else
                {
                    UpdateInventoryUI("Fist", "");
                    MakeInventorySlotEmpty(_currentlyHeldIdx);
                }

                return;
            }

            Debug.Assert(weaponContext.HasWeapon(), "Tries to attack but has no weapon");
            weaponContext.WeaponAttack();
        }
    }

    public Defense Trap
    {
        get; set;
    }

    private void Untrap()
    {
        if (_input.build && Trap != null)
        {

        }
    }

    public bool IsBuilding
    {
        get; set;
    }

    private void BuildButtonReleasedAction()
    {
        if (!isInventorySlotEmpty(_currentlyHeldIdx)
            && inventoryList[CurrentlyHeldIdx].Item1.GetComponent<Tool>() != null
            && objectToInteract != null
            && objectToInteract.GetComponentInParent<Structure>() != null
            && IsBuilding)
        {
            inventoryList[CurrentlyHeldIdx].Item1.GetComponent<Tool>().structure.BuildButtonReleased = true;
        }
    }

    private bool usingToolCurrently = false;
    private void UseTool()
    {
        if (_input.build
            && !isInventorySlotEmpty(_currentlyHeldIdx)
            && inventoryList[CurrentlyHeldIdx].Item1.GetComponent<Tool>() != null
            && !IsBuilding
            && !usingToolCurrently
            && !freezePlayerAndCameraMovement)
        {
            usingToolCurrently = true;
            StartCoroutine(UseToolCoroutine());
        }
    }

    private IEnumerator UseToolCoroutine()
    {
        inventoryList[CurrentlyHeldIdx].Item1.GetComponent<Tool>().structure = objectToInteract == null ? null : objectToInteract.GetComponentInParent<Structure>();
        yield return StartCoroutine(inventoryList[CurrentlyHeldIdx].Item1.GetComponent<Tool>().UseTool(this));
        if (inventoryList[CurrentlyHeldIdx].Item1.GetComponent<Tool>().structure != null)
        {
            inventoryList[CurrentlyHeldIdx].Item1.GetComponent<Tool>().structure.BuildButtonReleased = false;
        }
        _input.build = false;
        usingToolCurrently = false;
    }

    public void interact()
    {
        updateObjectToInteract();
        if (_input.interact && objectToCarry) // later expand to accept item of same type
        {
            Debug.Assert(objectToCarry.GetComponent<PickupableObject>().objectItem != null, "Object picked up does not have item field");
            pickup(objectToCarry);
        }
        else if (_input.interact && objectToInteract)
        {
            objectToInteract.GetComponent<Interactable>().Interact(gameObject);
        }
        else if (_input.interact2 && objectToInteract)
        {
            objectToInteract.GetComponent<Interactable>().Interact2(gameObject);
        }
        else if (_input.throwItem && inventoryList[_currentlyHeldIdx] != null && !isUsingCraftingTable)
        {
            dropoff();
        }

        // make inputs false regardless whether any of the above if condition is satisfied
        _input.interact = false;
        _input.interact2 = false;
        _input.throwItem = false;
        //objectToInteract = null;
    }

    public Tuple<GameObject, int> playerCurrentlyHeldObject
    {
        get
        {
            return inventoryList[_currentlyHeldIdx];
        }
    }

    public Transform TrackOrigin => eyeTransform;

    private Structure structure;

    public void updateObjectToInteract()
    {
        objectToCarry = null;
        objectToInteract = null;

#if UNITY_EDITOR
        if (RotaryHeart.Lib.PhysicsExtension.Physics.SphereCast(cameraTransform.position, sphereColliderRadius, cameraTransform.forward, out RaycastHit hitInfo, playerInteractDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore, RotaryHeart.Lib.PhysicsExtension.PreviewCondition.Both))
        {
#else
        if (Physics.SphereCast(cameraTransform.position, sphereColliderRadius, cameraTransform.forward, out RaycastHit hitInfo, playerInteractDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore)) { 
#endif
#if UNITY_EDITOR
            Debug.Log("Hitting: " + hitInfo.transform.name);
#endif
            if (hitInfo.transform.GetComponent<Carryable>() != null)
            {
                objectToCarry = hitInfo.transform.gameObject;

            }
            else if (hitInfo.transform.root.GetComponent<Carryable>() != null)
            {
                objectToCarry = hitInfo.transform.root.gameObject;
            }
            else
            {
                PickupPrompt.enabled = false;
                InventoryFullPrompt.enabled = false;
            }

            if (objectToCarry != null)
            {
                if (!objectToCarry.GetComponent<PickupableObject>().isPickedUp && DeterminePickupIdx(hitInfo.transform.gameObject)[0] == -1)
                {
                    InventoryFullPrompt.enabled = true;
                }
                else
                {
                    Debug.Assert(objectToCarry.GetComponent<PickupableObject>() != null, "Now we assume carryable <=> PickupableObject, so cannot pickup object if it is not PickupableObject");
#if UNITY_EDITOR
                    Debug.Log($"Is picked up: {objectToCarry.GetComponent<PickupableObject>().isPickedUp}");
#endif
                    if (!objectToCarry.GetComponent<PickupableObject>().isPickedUp)
                    {
                        PickupPrompt.transform.GetChild(1).GetComponent<TMP_Text>().text = objectToCarry.GetComponent<PickupableObject>().NumOfItem.ToString();
                        PickupPrompt.transform.GetChild(2).GetComponent<TMP_Text>().text = objectToCarry.GetComponent<PickupableObject>().objectItem.ItemName;
                        PickupPrompt.enabled = true;
                    }
                }
            }

            if (hitInfo.transform.gameObject.GetComponent<Interactable>() != null)
            {
                objectToInteract = hitInfo.transform.gameObject;
            }
            else if (hitInfo.transform.gameObject.GetComponentInParent<Interactable>() != null)
            {
                objectToInteract = hitInfo.transform.parent.gameObject;
            }
            if (objectToInteract != null)
            {
                structure = objectToInteract.GetComponentInParent<Structure>();
                if (structure != null && !structure.IsBuilt)
                {
                    structure.UI.canDisplay(true);
                }
            }
        }
        else
        {
            if (structure != null)
            {
                structure.UI.canDisplay(false);
                structure = null;
            }
            PickupPrompt.enabled = false;
            InventoryFullPrompt.enabled = false;
        }
    }

    public void EnterEnvironmentalHazard(EnvironmentalHazardType type)
    {
        if (type == EnvironmentalHazardType.Swamp)
        {
            effectDecorator = new SwampDecorator(effectDecorator);
        }
    }

    public void LeaveEnvironmentalHazard(EnvironmentalHazardType type)
    {
        // example to consider: CompressionDecorator(EncryptionDecorator(source))
        // If the first decorator in the chain is the type we are looking for 
        if ((effectDecorator as AbstractEffectDecorator).getHazardType() == type)
        {
            effectDecorator = (effectDecorator as AbstractEffectDecorator).getEffectWrappee();
            return;
        }
        IEffectDecorator cur = effectDecorator;
        IEffectDecorator next = (effectDecorator as AbstractEffectDecorator).getEffectWrappee();
        if ((next as AbstractEffectDecorator).getHazardType() == type)
        {
            (cur as AbstractEffectDecorator).setEffectWrappee((next as AbstractEffectDecorator).getEffectWrappee());
        }
        else if (next is AbstractEffectDecorator)
        {
            cur = next;
            next = (next as AbstractEffectDecorator).getEffectWrappee();
        }
        else // playerEffectBase is not AbstractEffectDecorator, so we know base case reached 
        {
            return;
        }
    }

    //private void ThrowItem()
    //{
    //    if (objectHeld != null && objectHeld.GetComponent<IThrowable>() != null && _input.throwItem)
    //    {
    //        Debug.Log("In throw item in player.cs");
    //        _input.throwItem = false;
    //        GameObject objectTobeThrown = objectHeld;
    //        dropoff();
    //        objectTobeThrown.GetComponent<IThrowable>().Throw(cameraTransform);
    //    }
    //}

    public override void ReceiveDamage(int damage)
    {
        base.ReceiveDamage(damage);
        HealthBar.fillAmount = (float)((float)curHealth / (float)initialHealth);
    }

    protected override void Die()
    {
#if UNITY_EDITOR
        Debug.Log("PlayerId: " + LocalConnection.ClientId + " is killed");
#endif
        // Todo: For now use this to force ontriggerexit. Need a better way.
        transform.position = new Vector3(1000000, 1000000, 1000000);
        GetComponent<NetworkObject>().Despawn();
    }
}

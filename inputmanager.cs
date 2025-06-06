using UnityEngine;

public class InputManager : Singleton<InputManager>
{
    [Header("Input Settings")]
    public KeyCode interactKey = KeyCode.E;
    public KeyCode inventoryKey = KeyCode.I;
    public KeyCode pauseKey = KeyCode.Escape;
    public KeyCode skillKey1 = KeyCode.Alpha1;
    public KeyCode skillKey2 = KeyCode.Alpha2;
    public KeyCode skillKey3 = KeyCode.Alpha3;
    public KeyCode skillKey4 = KeyCode.Alpha4;
    
    [Header("Movement")]
    public float horizontalInput;
    public float verticalInput;
    public Vector2 movementInput;
    
    [Header("Mouse")]
    public Vector2 mousePosition;
    public bool leftMouseDown;
    public bool rightMouseDown;
    public bool leftMousePressed;
    public bool rightMousePressed;
    
    // Eventos de input
    public static System.Action<Vector2> OnMovementInput;
    public static System.Action<Vector3> OnMousePositionInput;
    public static System.Action OnRunInput;
    public static System.Action OnRunInputReleased;
    public static System.Action OnPrimaryAttackInput;
    public static System.Action OnSecondaryAttackInput;
    public static System.Action<int> OnSkillInput;
    public static System.Action OnInventoryInput;
    
    protected override void Awake()
    {
        base.Awake();
    }
    
    private void Update()
    {
        HandleMovementInput();
        HandleMouseInput();
        HandleKeyboardInput();
    }
    
    private void HandleMovementInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        movementInput = new Vector2(horizontalInput, verticalInput).normalized;
        
        // Trigger movement event
        OnMovementInput?.Invoke(movementInput);
        
        // Run input
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            OnRunInput?.Invoke();
        }
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            OnRunInputReleased?.Invoke();
        }
    }
    
    private void HandleMouseInput()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f; // Para jogos 2D ou manter no plano
        
        mousePosition = mouseWorldPos;
        leftMouseDown = Input.GetMouseButtonDown(0);
        rightMouseDown = Input.GetMouseButtonDown(1);
        leftMousePressed = Input.GetMouseButton(0);
        rightMousePressed = Input.GetMouseButton(1);
        
        // Trigger mouse position event
        OnMousePositionInput?.Invoke(mouseWorldPos);
        
        // Attack inputs
        if (leftMouseDown)
        {
            OnPrimaryAttackInput?.Invoke();
        }
        
        if (rightMouseDown)
        {
            OnSecondaryAttackInput?.Invoke();
        }
    }
    
    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(interactKey))
        {
            EventManager.TriggerInteractPressed();
        }
        
        if (Input.GetKeyDown(inventoryKey))
        {
            EventManager.TriggerInventoryToggled();
            OnInventoryInput?.Invoke();
        }
        
        if (Input.GetKeyDown(pauseKey))
        {
            EventManager.TriggerPauseToggled();
        }
        
        if (Input.GetKeyDown(skillKey1))
        {
            EventManager.TriggerSkillUsed(1);
            OnSkillInput?.Invoke(1);
        }
        
        if (Input.GetKeyDown(skillKey2))
        {
            EventManager.TriggerSkillUsed(2);
            OnSkillInput?.Invoke(2);
        }
        
        if (Input.GetKeyDown(skillKey3))
        {
            EventManager.TriggerSkillUsed(3);
            OnSkillInput?.Invoke(3);
        }
        
        if (Input.GetKeyDown(skillKey4))
        {
            EventManager.TriggerSkillUsed(4);
            OnSkillInput?.Invoke(4);
        }
    }
    
    public Vector2 GetMovementInput()
    {
        return movementInput;
    }
    
    public Vector2 GetMouseWorldPosition()
    {
        return mousePosition;
    }
    
    public bool IsLeftMousePressed()
    {
        return leftMousePressed;
    }
    
    public bool IsRightMousePressed()
    {
        return rightMousePressed;
    }
    
    public bool IsLeftMouseDown()
    {
        return leftMouseDown;
    }
    
    public bool IsRightMouseDown()
    {
        return rightMouseDown;
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        // Limpar eventos
        OnMovementInput = null;
        OnMousePositionInput = null;
        OnRunInput = null;
        OnRunInputReleased = null;
        OnPrimaryAttackInput = null;
        OnSecondaryAttackInput = null;
        OnSkillInput = null;
        OnInventoryInput = null;
    }
}
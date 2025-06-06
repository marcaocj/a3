using UnityEngine;
using System.Collections;

public class GameManager : Singleton<GameManager>
{
    [Header("Game Settings")]
    public GameState currentGameState = GameState.MainMenu;
    public int currentDifficulty = 1; // 0=Easy, 1=Normal, 2=Hard
    public bool isPaused = false;
    public float gameTime = 0f;
    
    [Header("Player References")]
    public GameObject playerPrefab;
    public Transform playerSpawnPoint;
    private GameObject currentPlayer;
    
    [Header("Camera")]
    public Camera mainCamera;
    private CameraController cameraController;
    
    [Header("UI References")]
    public GameObject pauseMenu;
    public GameObject gameOverScreen;
    public GameObject victoryScreen;
    public GameObject loadingScreen;
    
    [Header("Audio")]
    public AudioClip mainMenuMusic;
    public AudioClip gameplayMusic;
    public AudioClip victoryMusic;
    public AudioClip gameOverMusic;
    public AudioClip buttonClickSound;
    
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver,
        Victory,
        Loading
    }
    
    // Public property for CurrentPlayer
    public GameObject CurrentPlayer => currentPlayer;
    
    protected override void Awake()
    {
        base.Awake();
        InitializeGame();
    }
    
    private void Start()
    {
        SetupEventListeners();
        SetGameState(GameState.MainMenu);
    }
    
    private void Update()
    {
        if (currentGameState == GameState.Playing)
        {
            gameTime += Time.deltaTime;
        }
    }
    
    private void InitializeGame()
    {
        Time.timeScale = 1f;
        
        // Configurar referências principais
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }
    
    private void SetupEventListeners()
    {
        EventManager.OnPlayerDeath += HandlePlayerDeath;
        EventManager.OnPauseToggled += TogglePause;
    }
    
    public void SetGameState(GameState newState)
    {
        GameState previousState = currentGameState;
        currentGameState = newState;
        
        HandleStateTransition(previousState, newState);
    }
    
    private void HandleStateTransition(GameState from, GameState to)
    {
        // Desativar UI do estado anterior
        DeactivateStateUI(from);
        
        // Ativar UI do novo estado
        ActivateStateUI(to);
        
        // Lógica específica para cada transição
        switch (to)
        {
            case GameState.MainMenu:
                HandleMainMenuState();
                break;
            case GameState.Playing:
                HandlePlayingState();
                break;
            case GameState.Paused:
                HandlePausedState();
                break;
            case GameState.GameOver:
                HandleGameOverState();
                break;
            case GameState.Victory:
                HandleVictoryState();
                break;
            case GameState.Loading:
                HandleLoadingState();
                break;
        }
    }
    
    private void DeactivateStateUI(GameState state)
    {
        switch (state)
        {
            case GameState.Paused:
                if (pauseMenu) pauseMenu.SetActive(false);
                break;
            case GameState.GameOver:
                if (gameOverScreen) gameOverScreen.SetActive(false);
                break;
            case GameState.Victory:
                if (victoryScreen) victoryScreen.SetActive(false);
                break;
            case GameState.Loading:
                if (loadingScreen) loadingScreen.SetActive(false);
                break;
        }
    }
    
    private void ActivateStateUI(GameState state)
    {
        switch (state)
        {
            case GameState.Paused:
                if (pauseMenu) pauseMenu.SetActive(true);
                break;
            case GameState.GameOver:
                if (gameOverScreen) gameOverScreen.SetActive(true);
                break;
            case GameState.Victory:
                if (victoryScreen) victoryScreen.SetActive(true);
                break;
            case GameState.Loading:
                if (loadingScreen) loadingScreen.SetActive(true);
                break;
        }
    }
    
    private void HandleMainMenuState()
    {
        Time.timeScale = 1f;
        isPaused = false;
        
        PlayMusic(mainMenuMusic);
    }
    
    private void HandlePlayingState()
    {
        Time.timeScale = 1f;
        isPaused = false;
        
        PlayMusic(gameplayMusic);
        
        // Configurar câmera se necessário
        if (cameraController == null && currentPlayer != null)
        {
            cameraController = FindFirstObjectByType<CameraController>();
        }
    }
    
    private void HandlePausedState()
    {
        Time.timeScale = 0f;
        isPaused = true;
    }
    
    private void HandleGameOverState()
    {
        Time.timeScale = 0f;
        isPaused = true;
        
        PlayMusic(gameOverMusic);
    }
    
    private void HandleVictoryState()
    {
        Time.timeScale = 0f;
        isPaused = true;
        
        PlayMusic(victoryMusic);
    }
    
    private void HandleLoadingState()
    {
        // Manter time scale normal durante loading
        Time.timeScale = 1f;
    }
    
    private void PlayMusic(AudioClip musicClip)
    {
        if (musicClip != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(musicClip);
        }
    }
    
    public void StartNewGame()
    {
        SetGameState(GameState.Loading);
        StartCoroutine(StartNewGameCoroutine());
    }
    
    private IEnumerator StartNewGameCoroutine()
    {
        yield return new WaitForSeconds(1f);
        
        gameTime = 0f;
        SpawnPlayer();
        SetGameState(GameState.Playing);
    }
    
    public void ResumeGame()
    {
        if (currentGameState == GameState.Paused)
        {
            SetGameState(GameState.Playing);
        }
    }
    
    public void SpawnPlayer()
    {
        if (playerPrefab != null && playerSpawnPoint != null)
        {
            if (currentPlayer != null)
            {
                Destroy(currentPlayer);
            }
            
            currentPlayer = Instantiate(playerPrefab, playerSpawnPoint.position, playerSpawnPoint.rotation);
            
            // Configurar câmera para seguir o player
            if (cameraController != null)
            {
                cameraController.SetTarget(currentPlayer.transform);
            }
        }
    }
    
    public void TogglePause()
    {
        if (currentGameState == GameState.Playing)
        {
            SetGameState(GameState.Paused);
            EventManager.TriggerGamePaused();
        }
        else if (currentGameState == GameState.Paused)
        {
            SetGameState(GameState.Playing);
            EventManager.TriggerGameResumed();
        }
    }
    
    private void HandlePlayerDeath()
    {
        StartCoroutine(DelayedGameOver());
    }
    
    private IEnumerator DelayedGameOver()
    {
        yield return new WaitForSeconds(2f);
        SetGameState(GameState.GameOver);
        EventManager.TriggerGameOver();
    }
    
    public void HandleLevelComplete()
    {
        SetGameState(GameState.Victory);
    }
    
    public void RestartGame()
    {
        SetGameState(GameState.Loading);
        StartCoroutine(RestartGameCoroutine());
    }
    
    private IEnumerator RestartGameCoroutine()
    {
        yield return new WaitForSeconds(1f);
        
        gameTime = 0f;
        SpawnPlayer();
        SetGameState(GameState.Playing);
    }
    
    public void ReturnToMainMenu()
    {
        SetGameState(GameState.Loading);
        StartCoroutine(ReturnToMainMenuCoroutine());
    }
    
    private IEnumerator ReturnToMainMenuCoroutine()
    {
        yield return new WaitForSeconds(1f);
        
        if (currentPlayer != null)
        {
            Destroy(currentPlayer);
            currentPlayer = null;
        }
        
        gameTime = 0f;
        SetGameState(GameState.MainMenu);
    }
    
    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    // Public methods for UI buttons
    public void PlayButtonClick()
    {
        if (buttonClickSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(buttonClickSound);
        }
    }
    
    // Public properties
    public bool IsGamePlaying()
    {
        return currentGameState == GameState.Playing;
    }
    
    public bool IsGamePaused()
    {
        return isPaused;
    }
    
    public float GetGameTime()
    {
        return gameTime;
    }
    
    public GameObject GetCurrentPlayer()
    {
        return currentPlayer;
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        EventManager.OnPlayerDeath -= HandlePlayerDeath;
        EventManager.OnPauseToggled -= TogglePause;
    }
}
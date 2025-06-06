using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

/// <summary>
/// Gerencia o sistema de salvamento e carregamento do jogo
/// </summary>
public class SaveManager : Singleton<SaveManager>
{
    [Header("Save Settings")]
    public string saveFileName = "GameSave";
    public string fileExtension = ".json";
    public bool encryptSaves = false;
    public int maxSaveSlots = 10;
    public bool autoSave = true;
    public float autoSaveInterval = 300f; // 5 minutos
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    // Caminho dos arquivos de save
    private string savePath;
    private GameSaveData currentSaveData;
    private float lastAutoSaveTime;
    
    // Eventos
    public System.Action<int> OnGameSaved;
    public System.Action<int> OnGameLoaded;
    public System.Action<string> OnSaveError;
    
    protected override void OnSingletonAwake()
    {
        // Configurar caminho de salvamento
        savePath = Application.persistentDataPath + "/Saves/";
        
        // Criar diretório se não existir
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"SaveManager inicializado. Caminho: {savePath}");
        }
    }
    
    private void Start()
    {
        lastAutoSaveTime = Time.time;
    }
    
    private void Update()
    {
        // Auto save
        if (autoSave && Time.time - lastAutoSaveTime >= autoSaveInterval)
        {
            AutoSave();
            lastAutoSaveTime = Time.time;
        }
    }
    
    #region Save System
    
    /// <summary>
    /// Salva o jogo em um slot específico
    /// </summary>
    public bool SaveGame(int saveSlot = 0, string customName = "")
    {
        try
        {
            if (saveSlot < 0 || saveSlot >= maxSaveSlots)
            {
                LogError($"Slot de save inválido: {saveSlot}");
                return false;
            }
            
            // Coletar dados de todos os sistemas
            GameSaveData saveData = CollectSaveData();
            
            // Configurar metadados
            saveData.saveSlot = saveSlot;
            saveData.saveName = string.IsNullOrEmpty(customName) ? $"Save {saveSlot + 1}" : customName;
            saveData.saveDate = DateTime.Now.ToString();
            saveData.gameVersion = Application.version;
            
            // Salvar arquivo
            string fileName = GetSaveFileName(saveSlot);
            string filePath = Path.Combine(savePath, fileName);
            
            string jsonData = JsonUtility.ToJson(saveData, true);
            
            if (encryptSaves)
            {
                jsonData = EncryptData(jsonData);
            }
            
            File.WriteAllText(filePath, jsonData);
            
            currentSaveData = saveData;
            OnGameSaved?.Invoke(saveSlot);
            
            if (enableDebugLogs)
            {
                Debug.Log($"Jogo salvo no slot {saveSlot}: {fileName}");
            }
            
            return true;
        }
        catch (Exception e)
        {
            string error = $"Erro ao salvar jogo: {e.Message}";
            LogError(error);
            OnSaveError?.Invoke(error);
            return false;
        }
    }
    
    /// <summary>
    /// Carrega o jogo de um slot específico
    /// </summary>
    public bool LoadGame(int saveSlot = 0)
    {
        try
        {
            if (!HasSave(saveSlot))
            {
                LogError($"Save não encontrado no slot {saveSlot}");
                return false;
            }
            
            string fileName = GetSaveFileName(saveSlot);
            string filePath = Path.Combine(savePath, fileName);
            
            string jsonData = File.ReadAllText(filePath);
            
            if (encryptSaves)
            {
                jsonData = DecryptData(jsonData);
            }
            
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(jsonData);
            
            if (saveData == null)
            {
                LogError("Dados de save corrompidos");
                return false;
            }
            
            // Aplicar dados aos sistemas
            ApplySaveData(saveData);
            
            currentSaveData = saveData;
            OnGameLoaded?.Invoke(saveSlot);
            
            if (enableDebugLogs)
            {
                Debug.Log($"Jogo carregado do slot {saveSlot}");
            }
            
            return true;
        }
        catch (Exception e)
        {
            string error = $"Erro ao carregar jogo: {e.Message}";
            LogError(error);
            OnSaveError?.Invoke(error);
            return false;
        }
    }
    
    /// <summary>
    /// Auto save do jogo
    /// </summary>
    public void AutoSave()
    {
        if (GameManager.Instance?.IsGamePlaying() == true)
        {
            SaveGame(0, "Auto Save");
        }
    }
    
    /// <summary>
    /// Salva apenas as configurações
    /// </summary>
    public bool SaveSettings()
    {
        try
        {
            SettingsSaveData settingsData = new SettingsSaveData();
            
            // Coletar configurações de áudio
            if (AudioManager.Instance != null)
            {
                settingsData.masterVolume = AudioManager.Instance.masterVolume;
                settingsData.musicVolume = AudioManager.Instance.musicVolume;
                settingsData.sfxVolume = AudioManager.Instance.sfxVolume;
                settingsData.voiceVolume = AudioManager.Instance.voiceVolume;
                settingsData.ambientVolume = AudioManager.Instance.ambientVolume;
                settingsData.uiVolume = AudioManager.Instance.uiVolume;
            }
            
            // Coletar configurações de input
            if (InputManager.Instance != null)
            {
                settingsData.interactKey = InputManager.Instance.interactKey.ToString();
                settingsData.inventoryKey = InputManager.Instance.inventoryKey.ToString();
                settingsData.pauseKey = InputManager.Instance.pauseKey.ToString();
                settingsData.skillKey1 = InputManager.Instance.skillKey1.ToString();
                settingsData.skillKey2 = InputManager.Instance.skillKey2.ToString();
                settingsData.skillKey3 = InputManager.Instance.skillKey3.ToString();
                settingsData.skillKey4 = InputManager.Instance.skillKey4.ToString();
            }
            
            // Configurações gráficas
            settingsData.resolution = Screen.currentResolution.ToString();
            settingsData.fullscreen = Screen.fullScreen;
            settingsData.vsync = QualitySettings.vSyncCount > 0;
            settingsData.qualityLevel = QualitySettings.GetQualityLevel();
            
            string jsonData = JsonUtility.ToJson(settingsData, true);
            string filePath = Path.Combine(savePath, "settings.json");
            
            File.WriteAllText(filePath, jsonData);
            
            if (enableDebugLogs)
            {
                Debug.Log("Configurações salvas");
            }
            
            return true;
        }
        catch (Exception e)
        {
            LogError($"Erro ao salvar configurações: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Carrega as configurações
    /// </summary>
    public bool LoadSettings()
    {
        try
        {
            string filePath = Path.Combine(savePath, "settings.json");
            
            if (!File.Exists(filePath))
            {
                if (enableDebugLogs)
                {
                    Debug.Log("Arquivo de configurações não encontrado, usando padrões");
                }
                return false;
            }
            
            string jsonData = File.ReadAllText(filePath);
            SettingsSaveData settingsData = JsonUtility.FromJson<SettingsSaveData>(jsonData);
            
            // Aplicar configurações de áudio
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(settingsData.masterVolume);
                AudioManager.Instance.SetMusicVolume(settingsData.musicVolume);
                AudioManager.Instance.SetSFXVolume(settingsData.sfxVolume);
                AudioManager.Instance.SetVoiceVolume(settingsData.voiceVolume);
                AudioManager.Instance.SetAmbientVolume(settingsData.ambientVolume);
                AudioManager.Instance.SetUIVolume(settingsData.uiVolume);
            }
            
            // Aplicar configurações de input
            if (InputManager.Instance != null)
            {
                if (Enum.TryParse(settingsData.interactKey, out KeyCode interactKey))
                    InputManager.Instance.interactKey = interactKey;
                if (Enum.TryParse(settingsData.inventoryKey, out KeyCode inventoryKey))
                    InputManager.Instance.inventoryKey = inventoryKey;
                if (Enum.TryParse(settingsData.pauseKey, out KeyCode pauseKey))
                    InputManager.Instance.pauseKey = pauseKey;
                if (Enum.TryParse(settingsData.skillKey1, out KeyCode skillKey1))
                    InputManager.Instance.skillKey1 = skillKey1;
                if (Enum.TryParse(settingsData.skillKey2, out KeyCode skillKey2))
                    InputManager.Instance.skillKey2 = skillKey2;
                if (Enum.TryParse(settingsData.skillKey3, out KeyCode skillKey3))
                    InputManager.Instance.skillKey3 = skillKey3;
                if (Enum.TryParse(settingsData.skillKey4, out KeyCode skillKey4))
                    InputManager.Instance.skillKey4 = skillKey4;
            }
            
            // Aplicar configurações gráficas
            Screen.fullScreen = settingsData.fullscreen;
            QualitySettings.vSyncCount = settingsData.vsync ? 1 : 0;
            QualitySettings.SetQualityLevel(settingsData.qualityLevel);
            
            if (enableDebugLogs)
            {
                Debug.Log("Configurações carregadas");
            }
            
            return true;
        }
        catch (Exception e)
        {
            LogError($"Erro ao carregar configurações: {e.Message}");
            return false;
        }
    }
    
    #endregion
    
    #region Data Collection
    
    /// <summary>
    /// Coleta todos os dados necessários para o save
    /// </summary>
    private GameSaveData CollectSaveData()
    {
        GameSaveData saveData = new GameSaveData();
        
        // Dados do GameManager
        if (GameManager.Instance != null)
        {
            saveData.gameTime = GameManager.Instance.GetGameTime();
            saveData.currentDifficulty = GameManager.Instance.currentDifficulty;
        }
        
        // Dados do Player
        GameObject player = GameManager.Instance?.GetCurrentPlayer();
        if (player != null)
        {
            // Posição e rotação
            saveData.playerPosition = player.transform.position;
            saveData.playerRotation = player.transform.rotation;
            
            // Stats do player
            PlayerStats playerStats = player.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                saveData.playerStatsData = new PlayerStatsData();
                saveData.playerStatsData.level = playerStats.level;
                saveData.playerStatsData.experience = playerStats.experience;
                saveData.playerStatsData.experienceToNextLevel = playerStats.experienceToNextLevel;
                saveData.playerStatsData.currentHealth = playerStats.currentHealth;
                saveData.playerStatsData.maxHealth = playerStats.maxHealth;
                saveData.playerStatsData.currentMana = playerStats.currentMana;
                saveData.playerStatsData.maxMana = playerStats.maxMana;
                saveData.playerStatsData.strength = playerStats.strength;
                saveData.playerStatsData.dexterity = playerStats.dexterity;
                saveData.playerStatsData.intelligence = playerStats.intelligence;
                saveData.playerStatsData.vitality = playerStats.vitality;
                saveData.playerStatsData.damage = playerStats.damage;
                saveData.playerStatsData.armor = playerStats.armor;
                saveData.playerStatsData.criticalChance = playerStats.criticalChance;
                saveData.playerStatsData.criticalDamage = playerStats.criticalDamage;
                saveData.playerStatsData.attackSpeed = playerStats.attackSpeed;
                saveData.playerStatsData.movementSpeed = playerStats.movementSpeed;
                saveData.playerStatsData.availableStatPoints = playerStats.availableStatPoints;
            }
            
            // Inventário
            PlayerInventory playerInventory = player.GetComponent<PlayerInventory>();
            if (playerInventory != null)
            {
                saveData.inventoryData = playerInventory.GetSaveData();
            }
            
            // Equipamentos
            PlayerEquipment playerEquipment = player.GetComponent<PlayerEquipment>();
            if (playerEquipment != null)
            {
                saveData.equipmentData = playerEquipment.GetSaveData();
            }
            
            // Skills
            PlayerSkillManager skillManager = player.GetComponent<PlayerSkillManager>();
            if (skillManager != null)
            {
                saveData.skillManagerData = skillManager.GetSaveData();
            }
        }
        
        // Cooldowns
        CooldownManager cooldownManager = player?.GetComponent<CooldownManager>();
        if (cooldownManager != null)
        {
            saveData.cooldownData = cooldownManager.GetSaveData();
        }
        
        // Quests
        if (QuestManager.Instance != null)
        {
            var questManagerData = QuestManager.Instance.GetSaveData();
            saveData.questData = new QuestSaveData();
            saveData.questData.activeQuestIds = questManagerData.activeQuestIds;
            saveData.questData.completedQuestIds = questManagerData.completedQuestIds;
            saveData.questData.failedQuestIds = questManagerData.failedQuestIds;
            saveData.questData.questData = questManagerData.questData;
        }
        
        // Cena atual
        saveData.currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        return saveData;
    }
    
    /// <summary>
    /// Aplica os dados salvos aos sistemas do jogo
    /// </summary>
    private void ApplySaveData(GameSaveData saveData)
    {
        // Carregar cena se necessário
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (currentScene != saveData.currentScene)
        {
            SceneLoader.Instance?.LoadScene(saveData.currentScene, false);
            return; // Dados serão aplicados após carregar a cena
        }
        
        // Aplicar dados do GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentDifficulty = saveData.currentDifficulty;
            // gameTime seria aplicado se GameManager tivesse um setter
        }
        
        // Aguardar spawn do player
        StartCoroutine(ApplyPlayerDataWhenReady(saveData));
    }
    
    private System.Collections.IEnumerator ApplyPlayerDataWhenReady(GameSaveData saveData)
    {
        // Aguardar player estar disponível
        while (GameManager.Instance?.GetCurrentPlayer() == null)
        {
            yield return null;
        }
        
        GameObject player = GameManager.Instance.GetCurrentPlayer();
        
        // Aplicar posição e rotação
        if (player.GetComponent<PlayerController>() != null)
        {
            player.GetComponent<PlayerController>().TeleportTo(saveData.playerPosition);
            player.transform.rotation = saveData.playerRotation;
        }
        
        // Aplicar stats do player
        PlayerStats playerStats = player.GetComponent<PlayerStats>();
        if (playerStats != null && saveData.playerStatsData != null)
        {
            var data = saveData.playerStatsData;
            playerStats.level = data.level;
            playerStats.experience = data.experience;
            playerStats.experienceToNextLevel = data.experienceToNextLevel;
            playerStats.currentHealth = data.currentHealth;
            playerStats.maxHealth = data.maxHealth;
            playerStats.currentMana = data.currentMana;
            playerStats.maxMana = data.maxMana;
            playerStats.strength = data.strength;
            playerStats.dexterity = data.dexterity;
            playerStats.intelligence = data.intelligence;
            playerStats.vitality = data.vitality;
            playerStats.damage = data.damage;
            playerStats.armor = data.armor;
            playerStats.criticalChance = data.criticalChance;
            playerStats.criticalDamage = data.criticalDamage;
            playerStats.attackSpeed = data.attackSpeed;
            playerStats.movementSpeed = data.movementSpeed;
            playerStats.availableStatPoints = data.availableStatPoints;
            
            playerStats.RecalculateStats();
        }
        
        // Aplicar inventário
        PlayerInventory playerInventory = player.GetComponent<PlayerInventory>();
        if (playerInventory != null && saveData.inventoryData != null)
        {
            playerInventory.LoadSaveData(saveData.inventoryData);
        }
        
        // Aplicar equipamentos
        PlayerEquipment playerEquipment = player.GetComponent<PlayerEquipment>();
        if (playerEquipment != null && saveData.equipmentData != null)
        {
            playerEquipment.LoadSaveData(saveData.equipmentData);
        }
        
        // Aplicar skills
        PlayerSkillManager skillManager = player.GetComponent<PlayerSkillManager>();
        if (skillManager != null && saveData.skillManagerData != null)
        {
            skillManager.LoadSaveData(saveData.skillManagerData);
        }
        
        // Aplicar cooldowns
        CooldownManager cooldownManager = player.GetComponent<CooldownManager>();
        if (cooldownManager != null && saveData.cooldownData != null)
        {
            cooldownManager.LoadSaveData(saveData.cooldownData);
        }
        
        // Aplicar quests
        if (QuestManager.Instance != null && saveData.questData != null)
        {
            var questManagerSaveData = new QuestManager.QuestSaveData();
            questManagerSaveData.activeQuestIds = saveData.questData.activeQuestIds;
            questManagerSaveData.completedQuestIds = saveData.questData.completedQuestIds;
            questManagerSaveData.failedQuestIds = saveData.questData.failedQuestIds;
            questManagerSaveData.questData = saveData.questData.questData;
            
   //         QuestManager.Instance.LoadFromSaveData(questManagerSaveData);
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Verifica se existe um save no slot
    /// </summary>
    public bool HasSave(int saveSlot)
    {
        string fileName = GetSaveFileName(saveSlot);
        string filePath = Path.Combine(savePath, fileName);
        return File.Exists(filePath);
    }
    
    /// <summary>
    /// Obtém informações de um save
    /// </summary>
    public SaveInfo GetSaveInfo(int saveSlot)
    {
        if (!HasSave(saveSlot))
            return null;
        
        try
        {
            string fileName = GetSaveFileName(saveSlot);
            string filePath = Path.Combine(savePath, fileName);
            
            string jsonData = File.ReadAllText(filePath);
            
            if (encryptSaves)
            {
                jsonData = DecryptData(jsonData);
            }
            
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(jsonData);
            
            SaveInfo info = new SaveInfo();
            info.saveSlot = saveSlot;
            info.saveName = saveData.saveName;
            info.saveDate = saveData.saveDate;
            info.gameVersion = saveData.gameVersion;
            info.playerLevel = saveData.playerStatsData?.level ?? 1;
            info.gameTime = saveData.gameTime;
            info.currentScene = saveData.currentScene;
            
            return info;
        }
        catch (Exception e)
        {
            LogError($"Erro ao ler informações do save {saveSlot}: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Obtém lista de todos os saves
    /// </summary>
    public List<SaveInfo> GetAllSaves()
    {
        List<SaveInfo> saves = new List<SaveInfo>();
        
        for (int i = 0; i < maxSaveSlots; i++)
        {
            SaveInfo info = GetSaveInfo(i);
            if (info != null)
            {
                saves.Add(info);
            }
        }
        
        return saves;
    }
    
    /// <summary>
    /// Deleta um save
    /// </summary>
    public bool DeleteSave(int saveSlot)
    {
        try
        {
            if (!HasSave(saveSlot))
                return false;
            
            string fileName = GetSaveFileName(saveSlot);
            string filePath = Path.Combine(savePath, fileName);
            
            File.Delete(filePath);
            
            if (enableDebugLogs)
            {
                Debug.Log($"Save deletado: slot {saveSlot}");
            }
            
            return true;
        }
        catch (Exception e)
        {
            LogError($"Erro ao deletar save {saveSlot}: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Deleta todos os saves
    /// </summary>
    public void DeleteAllSaves()
    {
        for (int i = 0; i < maxSaveSlots; i++)
        {
            DeleteSave(i);
        }
    }
    
    private string GetSaveFileName(int saveSlot)
    {
        return $"{saveFileName}_{saveSlot:D2}{fileExtension}";
    }
    
    #endregion
    
    #region Encryption (Simple)
    
    private string EncryptData(string data)
    {
        // Implementação simples de "encriptação" (apenas para obfuscação básica)
        // Para um jogo real, usar algo mais robusto
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data);
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)(bytes[i] ^ 0xAB); // XOR simples
        }
        return System.Convert.ToBase64String(bytes);
    }
    
    private string DecryptData(string encryptedData)
    {
        try
        {
            byte[] bytes = System.Convert.FromBase64String(encryptedData);
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ 0xAB); // XOR reverso
            }
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            // Se falhar, assumir que não está encriptado
            return encryptedData;
        }
    }
    
    #endregion
    
    #region Debug
    
    private void LogError(string message)
    {
        if (enableDebugLogs)
        {
            Debug.LogError($"[SaveManager] {message}");
        }
    }
    
    [ContextMenu("Test Save Game")]
    private void TestSaveGame()
    {
        SaveGame(99, "Test Save");
    }
    
    [ContextMenu("Test Load Game")]
    private void TestLoadGame()
    {
        LoadGame(99);
    }
    
    [ContextMenu("List All Saves")]
    private void ListAllSaves()
    {
        var saves = GetAllSaves();
        Debug.Log($"Encontrados {saves.Count} saves:");
        foreach (var save in saves)
        {
            Debug.Log($"Slot {save.saveSlot}: {save.saveName} - {save.saveDate} - Level {save.playerLevel}");
        }
    }
    
    #endregion
    
    #region Properties
    
    public string SavePath => savePath;
    public GameSaveData CurrentSaveData => currentSaveData;
    public bool HasCurrentSave => currentSaveData != null;
    
    #endregion
}

/// <summary>
/// Dados principais do save do jogo
/// </summary>
[System.Serializable]
public class GameSaveData
{
    // Metadados
    public int saveSlot;
    public string saveName;
    public string saveDate;
    public string gameVersion;
    
    // Dados do jogo
    public float gameTime;
    public int currentDifficulty;
    public string currentScene;
    
    // Dados do player
    public Vector3 playerPosition;
    public Quaternion playerRotation;
    public PlayerStatsData playerStatsData;
    public InventoryData inventoryData;
    public EquipmentData equipmentData;
    public SkillManagerData skillManagerData;
    public CooldownManagerData cooldownData;
    
    // Dados de sistemas
    public QuestSaveData questData;
}

/// <summary>
/// Dados das estatísticas do player para save
/// </summary>
[System.Serializable]
public class PlayerStatsData
{
    public int level;
    public int experience;
    public int experienceToNextLevel;
    public float currentHealth;
    public float maxHealth;
    public float currentMana;
    public float maxMana;
    public int strength;
    public int dexterity;
    public int intelligence;
    public int vitality;
    public int damage;
    public int armor;
    public float criticalChance;
    public float criticalDamage;
    public float attackSpeed;
    public float movementSpeed;
    public int availableStatPoints;
}

/// <summary>
/// Dados das configurações do jogo
/// </summary>
[System.Serializable]
public class SettingsSaveData
{
    // Áudio
    public float masterVolume = 1f;
    public float musicVolume = 0.7f;
    public float sfxVolume = 1f;
    public float voiceVolume = 1f;
    public float ambientVolume = 0.5f;
    public float uiVolume = 0.8f;
    
    // Input
    public string interactKey = "E";
    public string inventoryKey = "I";
    public string pauseKey = "Escape";
    public string skillKey1 = "Alpha1";
    public string skillKey2 = "Alpha2";
    public string skillKey3 = "Alpha3";
    public string skillKey4 = "Alpha4";
    
    // Gráficos
    public string resolution;
    public bool fullscreen = true;
    public bool vsync = true;
    public int qualityLevel = 2;
}

/// <summary>
/// Informações de um save para exibição
/// </summary>
[System.Serializable]
public class SaveInfo
{
    public int saveSlot;
    public string saveName;
    public string saveDate;
    public string gameVersion;
    public int playerLevel;
    public float gameTime;
    public string currentScene;
    
    public string GetFormattedGameTime()
    {
        int hours = Mathf.FloorToInt(gameTime / 3600);
        int minutes = Mathf.FloorToInt((gameTime % 3600) / 60);
        return $"{hours:D2}:{minutes:D2}";
    }
}

/// <summary>
/// Dados de salvamento das quests (cópia da classe do QuestManager)
/// </summary>
[System.Serializable]
public class QuestSaveData
{
    public List<string> activeQuestIds = new List<string>();
    public List<string> completedQuestIds = new List<string>();
    public List<string> failedQuestIds = new List<string>();
    public Dictionary<string, Dictionary<string, object>> questData = new Dictionary<string, Dictionary<string, object>>();
}
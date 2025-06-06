using UnityEngine;

/// <summary>
/// Classe base genérica para implementar o padrão Singleton em MonoBehaviours
/// </summary>
/// <typeparam name="T">Tipo da classe que herda de Singleton</typeparam>
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    private static readonly object lockObject = new object();
    private static bool applicationIsQuitting = false;
    
    /// <summary>
    /// Instância única da classe
    /// </summary>
    public static T Instance
    {
        get
        {
            if (applicationIsQuitting)
            {
                Debug.LogWarning($"[Singleton] Instância de {typeof(T)} não encontrada pois a aplicação está finalizando.");
                return null;
            }
            
            lock (lockObject)
            {
                if (instance == null)
                {
                    // Usar a nova função recomendada pelo Unity
                    instance = FindFirstObjectByType<T>();
                    
                    if (instance == null)
                    {
                        GameObject singletonObject = new GameObject($"{typeof(T).Name} (Singleton)");
                        instance = singletonObject.AddComponent<T>();
                        
                        Debug.Log($"[Singleton] Uma instância de {typeof(T)} foi criada automaticamente.");
                    }
                }
                
                return instance;
            }
        }
    }
    
    /// <summary>
    /// Verifica se existe uma instância do singleton
    /// </summary>
    public static bool HasInstance => instance != null && !applicationIsQuitting;
    
    /// <summary>
    /// Inicialização do singleton
    /// </summary>
    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            
            // Manter o objeto através das cenas se especificado
            if (ShouldPersistAcrossScenes())
            {
                DontDestroyOnLoad(gameObject);
            }
            
            // Chamar inicialização personalizada
            OnSingletonAwake();
        }
        else if (instance != this)
        {
            Debug.LogWarning($"[Singleton] Múltiplas instâncias de {typeof(T)} detectadas. Destruindo duplicata.");
            
            // Se já existe uma instância, destruir esta
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }
    }
    
    /// <summary>
    /// Limpeza quando a aplicação está finalizando
    /// </summary>
    protected virtual void OnApplicationQuit()
    {
        applicationIsQuitting = true;
    }
    
    /// <summary>
    /// Limpeza quando o objeto é destruído
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            OnSingletonDestroy();
        }
    }
    
    /// <summary>
    /// Método virtual para determinar se o singleton deve persistir entre cenas
    /// Override este método nas classes filhas se necessário
    /// </summary>
    /// <returns>True se deve persistir, False caso contrário</returns>
    protected virtual bool ShouldPersistAcrossScenes()
    {
        return true; // Por padrão, singletons persistem entre cenas
    }
    
    /// <summary>
    /// Método virtual chamado quando o singleton é inicializado
    /// Override este método nas classes filhas para inicialização personalizada
    /// </summary>
    protected virtual void OnSingletonAwake()
    {
        // Implementação padrão vazia - override nas classes filhas se necessário
    }
    
    /// <summary>
    /// Método virtual chamado quando o singleton é destruído
    /// Override este método nas classes filhas para limpeza personalizada
    /// </summary>
    protected virtual void OnSingletonDestroy()
    {
        // Implementação padrão vazia - override nas classes filhas se necessário
    }
    
    /// <summary>
    /// Força a criação da instância se ela não existir
    /// Útil para garantir que o singleton seja inicializado em um momento específico
    /// </summary>
    public static void EnsureInstance()
    {
        var temp = Instance; // Simplesmente acessar a propriedade já força a criação
    }
    
    /// <summary>
    /// Destrói a instância atual do singleton
    /// Use com cuidado - geralmente apenas para testes ou casos especiais
    /// </summary>
    public static void DestroyInstance()
    {
        if (instance != null)
        {
            if (Application.isPlaying)
            {
                Destroy(instance.gameObject);
            }
            else
            {
                DestroyImmediate(instance.gameObject);
            }
            instance = null;
        }
    }
}

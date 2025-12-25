using UnityEngine;

public sealed class GameBootstrap : MonoBehaviour
{
    static bool _initialized;

    [SerializeField] UIScreenCatalog uiCatalog;

    void Awake()
    {
        if (_initialized)
        {
            Destroy(gameObject);
            return;
        }

        _initialized = true;
        DontDestroyOnLoad(gameObject);

        UISystemInitializer.InitAll(uiCatalog);
    }
}
using UnityEngine;

public class PlayTimeManager : MonoBehaviour
{
    public static PlayTimeManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (CloudSaveManager.Instance != null && CloudSaveManager.Instance.CurrentData != null)
        {
            CloudSaveManager.Instance.CurrentData.OyunSuresi += Time.deltaTime;
        }
    }
}

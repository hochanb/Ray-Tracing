using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField, ReadOnly]
    string _sceneName;
    public void Load()
    {
        SceneManager.LoadSceneAsync(_sceneName, LoadSceneMode.Single);
    }

    public void Load(string sceneName)
    {
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
    }

#if UNITY_EDITOR
    [SerializeField]
    SceneAsset _scene;
    private void OnValidate()
    {
        if (_scene != null)
        {
            _sceneName = _scene.name;
            UnityEditor.EditorPrefs.SetString(GetSceneNameKey(), _sceneName);
        }
    }

    private void OnEnable()
    {
        _sceneName = UnityEditor.EditorPrefs.GetString(GetSceneNameKey(), _sceneName);
    }

    private string GetSceneNameKey()
    {
        return $"{gameObject.name}_SceneName";
    }
#endif

}

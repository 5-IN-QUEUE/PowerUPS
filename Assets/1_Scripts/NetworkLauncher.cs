using Fusion;
using UnityEngine;

public class NetworkLauncher : MonoBehaviour
{
    private NetworkRunner _runner;
    [SerializeField] private NetworkRunner _networkRunnerPrefab;

    public async void Start()
    {
        _runner = Instantiate(_networkRunnerPrefab);
        _runner.ProvideInput = true;
        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = "Test",
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }
}

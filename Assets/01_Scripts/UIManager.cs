using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private TextMeshProUGUI ammoMaxText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Image            weaponIMG;

    [Header("Round End")]
    [SerializeField] private GameObject      roundEndPanel;
    [SerializeField] private TextMeshProUGUI roundResultText;
    [SerializeField] private TextMeshProUGUI roundScoreText;

    [Header("Loading")]
    [SerializeField] private GameObject loadingPanel;

    [Header("Match End")]
    [SerializeField] private GameObject matchEndPanel;
    [SerializeField] private TextMeshProUGUI matchResultText;
    [SerializeField] private TextMeshProUGUI matchFinalScoreText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button exitButton;

    private PlayerShoot      _localShoot;
    private PlayerController _localCtrl;

    private int _lastAmmo = -1;
    private int _lastHp   = -1;

    private void Awake() => Instance = this;

    private void OnEnable()
    {
        RoundManager.OnRoundEnd += ShowRoundEnd;
        GameFlowManager.OnStateChanged += HandleGameFlowStateChange;
        PowerUpManager.OnPowerUpApplied += ShowPowerUpApplication;
    }

    private void OnDisable()
    {
        RoundManager.OnRoundEnd -= ShowRoundEnd;
        GameFlowManager.OnStateChanged -= HandleGameFlowStateChange;
        PowerUpManager.OnPowerUpApplied -= ShowPowerUpApplication;
    }

    private void Start()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartButtonClicked);
        
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitButtonClicked);
    }

    private void Update()
    {
        if (weaponIMG != null && _localShoot != null && _localShoot.Stats != null)
            weaponIMG.sprite = _localShoot.Stats.GunIMG;
        
        if (_localShoot == null && PlayerController.localPlayer != null)
        {
            _localShoot = PlayerController.localPlayer.GetComponent<PlayerShoot>();
            _localCtrl  = PlayerController.localPlayer.GetComponent<PlayerController>();

            if (ammoMaxText != null)
                ammoMaxText.SetText("/ {0}", GunData.MaxAmmo);
        }

        if (_localShoot != null)
        {
            int ammo = _localShoot.Ammo;
            if (ammo != _lastAmmo)
            {
                _lastAmmo = ammo;
                if (ammoText != null) ammoText.SetText("{0}", ammo);
            }
        }

        if (_localCtrl != null)
        {
            int hp = _localCtrl.PlayerHealth;
            if (hp != _lastHp)
            {
                _lastHp = hp;
                if (hpText != null) hpText.SetText("{0}", hp);
            }
        }
    }

    private void ShowRoundEnd(string killerName, string scores)
    {
        if (roundEndPanel != null)  roundEndPanel.SetActive(true);
        if (roundResultText != null) roundResultText.SetText($"{killerName} eliminated!");
        if (roundScoreText != null)  roundScoreText.SetText(scores);

        StartCoroutine(HideRoundEndAfterDelay(3f));
    }

    private IEnumerator HideRoundEndAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (roundEndPanel != null) roundEndPanel.SetActive(false);

        _lastAmmo = -1;
        _lastHp   = -1;
    }

    private void HandleGameFlowStateChange(GameFlowManager.GameState newState)
    {
        switch (newState)
        {
            case GameFlowManager.GameState.Waiting:
                break;

            case GameFlowManager.GameState.Loading:
                if (roundEndPanel != null) roundEndPanel.SetActive(false);
                if (matchEndPanel != null) matchEndPanel.SetActive(false);
                break;

            case GameFlowManager.GameState.AugmentSelect:
                if (loadingPanel != null) loadingPanel.SetActive(false);
                if (roundEndPanel != null) roundEndPanel.SetActive(false);
                break;

            case GameFlowManager.GameState.RoundActive:
                if (loadingPanel != null) loadingPanel.SetActive(false);
                break;

            case GameFlowManager.GameState.RoundEnd:
                break;

            case GameFlowManager.GameState.MatchEnd:
                ShowMatchEnd();
                break;
        }
    }

    private void ShowMatchEnd()
    {
        if (matchEndPanel == null) return;

        matchEndPanel.SetActive(true);

        if (PlayerController.localPlayer != null)
        {
            var localCtrl = PlayerController.localPlayer.GetComponent<PlayerController>();
            if (localCtrl != null)
            {
                int opponentScore = 0;
                foreach (var player in FindObjectsOfType<PlayerController>())
                {
                    if (player.gameObject != PlayerController.localPlayer)
                    {
                        opponentScore = player.Score;
                        break;
                    }
                }

                string result = localCtrl.Score > opponentScore ? "Victory" : "Defeat";
                if (matchResultText != null)
                    matchResultText.SetText(result);

                if (matchFinalScoreText != null)
                    matchFinalScoreText.SetText($"{localCtrl.Score} - {opponentScore}");
            }
        }
    }

    private void ShowPowerUpApplication(Fusion.PlayerRef player, string powerUpName, string description)
    {
        Debug.Log($"[UIManager] PowerUp Applied: {powerUpName} - {description}");
    }

    private void OnRestartButtonClicked()
    {
        var gfm = FindObjectOfType<GameFlowManager>();
        if (gfm != null && gfm.HasStateAuthority)
        {
            foreach (var player in FindObjectsOfType<PlayerController>())
            {
                player.ResetScore();
            }
            
            gfm.EvaluateMatchStatus();
            
            if (matchEndPanel != null)
                matchEndPanel.SetActive(false);
        }
    }

    private void OnExitButtonClicked()
    {
        var runner = FindObjectOfType<Fusion.NetworkRunner>();
        if (runner != null)
        {
            runner.Shutdown();
        }
        
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
    }
}

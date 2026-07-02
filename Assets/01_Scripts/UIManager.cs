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

    [Header("Augment Select")]
    [SerializeField] private GameObject upgradeCardPanel;

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
                if (upgradeCardPanel != null)
                {
                    // 패자만 카드를 선택해야 하는 라운드에 승자 쪽에도 카드 화면이
                    // 뜨면, 승자가 카드를 눌러도 서버에서 조용히 무시될 뿐인데
                    // 겉보기엔 "승자도 증강을 고른 것처럼" 보이는 문제가 있었다.
                    // 자격 없는 클라이언트는 패널 자체를 띄우지 않는다.
                    bool eligible = IsLocalPlayerEligibleForAugment();
                    upgradeCardPanel.SetActive(eligible);
                    if (eligible)
                    {
                        // Networked 상태 스냅샷이 RPC보다 늦게 도착하면
                        // UpgradeCardSelect.OnEnable()의 CurrentState 체크가 실패할 수 있어
                        // BeginSelecting()이 안 불릴 수 있다. 여기서 직접 호출해 타이밍 의존 제거.
                        var ucs = upgradeCardPanel.GetComponentInChildren<UpgradeCardSelect>(true);
                        if (ucs != null) ucs.BeginSelecting();
                    }
                }
                break;

            case GameFlowManager.GameState.RoundActive:
                if (loadingPanel != null) loadingPanel.SetActive(false);
                if (upgradeCardPanel != null) upgradeCardPanel.SetActive(false);
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

    private bool IsLocalPlayerEligibleForAugment()
    {
        if (RoundManager.Instance == null) return true;
        if (PlayerController.localPlayer == null) return true;

        var pc = PlayerController.localPlayer.GetComponent<PlayerController>();
        if (pc == null || pc.Runner == null) return true;

        return RoundManager.Instance.IsEligibleThisRound(pc.Runner.LocalPlayer);
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

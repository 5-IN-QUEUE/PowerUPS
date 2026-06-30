# 씬 세팅 체크리스트

## 메인 씬 (MainScene)

### NetworkLauncher 오브젝트 (컴포넌트 4개 필요)
- [ ] `NetworkLauncher` — NetworkRunner Prefab 연결, Battle Scene Path 확인 (`Assets/00_Scenes/JoPockScene.unity`)
- [ ] `NetworkInputHandler`
- [ ] `PlayerSpawner` — Player Prefab 연결
- [ ] `MatchManager` — Waiting UI 오브젝트 연결

### Build Settings
- [ ] `MainScene` 등록
- [ ] `JoPockScene` (전투씬) 등록

---

## 전투 씬 (JoPockScene)

### 씬 내 오브젝트
- [ ] `SpawnPoint_0` 빈 오브젝트 — 플레이어 1 스폰 위치
- [ ] `SpawnPoint_1` 빈 오브젝트 — 플레이어 2 스폰 위치
- [ ] `GameFlowManager` NetworkObject
- [ ] `RoundManager` NetworkObject
- [ ] `PowerUpManager` NetworkObject — PowerUpData ScriptableObject 4개 연결

### UI (Canvas)
- [ ] `UIManager` 컴포넌트
  - HUD: `ammoText`, `ammoMaxText`, `hpText`, `weaponIMG`
  - RoundEnd Panel: `roundResultText`, `roundScoreText`
  - MatchEnd Panel: `matchResultText`, `matchFinalScoreText`, Restart 버튼, Exit 버튼
- [ ] `UpgradeCardSelect` — 카드 RectTransform 4개 연결 (각 카드 자식으로 이름 TMP, 설명 TMP 필요)

---

## 프리팹 세팅

### Player Prefab
- [ ] `PlayerController`
- [ ] `PlayerShoot` — GunData 연결, Bullet Prefab 연결, Muzzle Point Transform 연결
- [ ] `NetworkCharacterController`
- [ ] 자식 오브젝트에 `TextMesh` (이름표용)

### Bullet Prefab
- [ ] `BulletScript` (NetworkObject)
- [ ] Collider

---

## ScriptableObject 목록
- [ ] `GunData` 1개 이상 — PlayerShoot에 할당
- [ ] `PowerUpData` 4개 — PowerUpManager에 할당
  - 각 PowerUpData에서 설정 가능한 효과: 데미지, 체력, 발사속도, 이동속도, 펠릿 수, 리코쳇 횟수, 탄퍼짐, 탄속

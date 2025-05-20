using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Advertisement;
using Advertisement.Keys;
using AGTutorial.Scripts;
using AssetKits.ParticleImage;
using CharInfoUI;
using Coffee.UIExtensions;
using CollectiblesLand.Scripts.Events;
using Cysharp.Threading.Tasks;
using Data;
using DG.Tweening;
using IAP;
using Load;
using Localization;
using RedDots;
using Rooms;
using Rooms.Data;
using RotaryHeart.Lib.SerializableDictionary;
using Share;
using UniRx;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
using Logger = CustomLogger.Logger;


/// <summary>
/// 플로팅 UI 아이템 타입
/// </summary>
public enum FloatingUIItemType
{
    None = -1, // 아무것도 없음
    CareBubble, // 케어 버블
    SleepyEffect, // 졸음 효과
    MiniGameBubble // 미니 게임 버블
}

/// <summary>
/// UI 매니저
/// </summary>
public class UIManager : Singleton<UIManager>, IInit
{
    /// <summary>
    /// 메인 카메라
    /// </summary>
    public Camera MainCamera;

    /// <summary>
    /// 상단 뷰 인스턴스
    /// </summary>
    public TopView TopViewInstance;
    /// <summary>
    /// 하단 뷰 인스턴스
    /// </summary>
    public BottomView BottomViewInstance;
    /// <summary>
    /// 파티클 뷰 인스턴스
    /// </summary>
    public ParticleView ParticleViewInstance;
    /// <summary>
    /// 샤워 뷰 인스턴스
    /// </summary>
    public ShowerView ShowerViewInstance;
    /// <summary>
    /// 장식 뷰 인스턴스
    /// </summary>
    public DecorateView DecorateViewInstance;
    /// <summary>
    /// 진화 뷰 인스턴스
    /// </summary>
    public EvolutionView EvolutionViewInstance;

    /// <summary>
    /// 화면 페이드 이미지
    /// </summary>
    [SerializeField] private Image screenFadeImage = null;
    /// <summary>
    /// 버튼 애니메이터
    /// </summary>
    public RuntimeAnimatorController ButtonAnimator = null;

    /// <summary>
    /// 플로팅 UI 아이템 타입 별 프리팹
    /// </summary>
    public SerializableDictionaryBase<FloatingUIItemType, FloatingUIItem> FloatingUIItemPrefabs;

    /// <summary>
    /// 플로팅 UI 오브젝트 부모 
    /// </summary>
    public Transform UIObjectPool;

    /// <summary>
    /// 로딩 애니메이션 (구름 애니메이션)
    /// </summary>
    public LoadingAnimation loadingAnimation;

    /// <summary>
    /// 미니 게임 버튼 클릭 시 이벤트
    /// </summary>
    public ClickMiniGameUIEvent miniGameClickEvent;

    /// <summary>
    /// 케어 게이지 낮음 임계값
    /// </summary>
    private readonly float LowThreshold = 0.4f;

    /// <summary>
    /// 코인 UI 초기화기
    /// </summary>
    private MoneyUIInitializer _coinInitializer;
    /// <summary>
    /// 구독 리스트
    /// </summary>
    private IEnumerable<IDisposable> _disposables;
    /// <summary>
    /// 젬 UI 초기화기
    /// </summary>
    private MoneyUIInitializer _gemInitializer;

    /// <summary>
    /// 광고 제거 상품 1 구매 여부
    /// </summary>
    private bool _hasRemoveAds1 = false;
    /// <summary>
    /// 광고 제거 상품 2 구매 여부
    /// </summary>
    private bool _hasRemoveAds2 = false;

    /// <summary>
    /// 진화 뷰 상태
    /// </summary>
    private ReactiveProperty<EvolutionViewState> _isEvolutionViewOpened = new(null);

    /// <summary>
    /// 진화 중인지 여부
    /// </summary>
    private bool _isEvolving = false;

    /// <summary>
    /// 메인 카메라 프로퍼티
    /// </summary>
    private ReactiveProperty<Camera> _mainCameraProperty = new(null);

    /// <summary>
    /// 변환 당 생성된 FloatingUIItem 인스턴스를 추적하기 위한 사전
    /// </summary>
    private Dictionary<Transform, List<FloatingUIItem>> activeFloatingUIItems =
        new Dictionary<Transform, List<FloatingUIItem>>();

    /// <summary>
    /// 케어 타입 별 아이템
    /// </summary>
    private Dictionary<CareType, CareItem> careItems;
    /// <summary>
    /// 케어 타입 별 코루틴
    /// </summary>
    private Dictionary<CareType, Coroutine> fillAmountCoroutines = new Dictionary<CareType, Coroutine>();

    /// <summary>
    /// FloatingUIItem 오브젝트 풀
    /// </summary>
    private Dictionary<FloatingUIItemType, ObjectPool<FloatingUIItem>> objectPools =
        new Dictionary<FloatingUIItemType, ObjectPool<FloatingUIItem>>();

    /// <summary>
    /// 플레이 중인 똥 파티클 리스트
    /// </summary>
    List<ParticleImage> playingPoopParticleList = new List<ParticleImage>();

    /// <summary>
    /// 위치 별로 표시된 파티클의 수를 추적하는 딕셔너리
    /// </summary>
    private Dictionary<Vector2, int> positionCounters = new Dictionary<Vector2, int>();

    /// <summary>
    /// UI 애니메이션 시퀀스
    /// </summary>
    private UIAnimationSequence uIAnimationSequence = null;

    private void OnDestroy()
    {
        UnSubscribes();
    }

    /// <summary>
    /// UI 매니저 리셋
    /// </summary>
    public void Reset()
    {
        UnSubscribes();

        foreach (var itemType in FloatingUIItemPrefabs.Keys)
        {
            objectPools[itemType].Dispose();
        }

        foreach (var item in activeFloatingUIItems.Values)
        {
            foreach (var floatingUIItem in item)
            {
                Destroy(floatingUIItem.gameObject);
            }
        }

        activeFloatingUIItems.Clear();
        positionCounters.Clear();
        playingPoopParticleList.Clear();
        fillAmountCoroutines.Clear();

        _coinInitializer?.Reset();
        _gemInitializer?.Reset();
        BottomViewInstance.EvolutionBar.Reset();
        GetComponentsInChildren<RedDot>(true).ToList().ForEach(x => x.Reset());
        GameManager.Instance.RemoveBackFunction(OnEscapeKeyPreesed);
    }

    /// <summary>
    /// UI 매니저 초기화
    /// </summary>
    public void Init()
    {
        SetMainCameraProperty();
        InitFloatingUIPools();
        SetActiveFadeScreen(false);
        Subscribes();
        SetUIAction();
        InitializeEvolutionBar();
        InitializeCareItems();
        InitializeRedDots();

        _coinInitializer = TopViewInstance.Coin.GetComponent<MoneyUIInitializer>().Init();
        _gemInitializer = TopViewInstance.Gem.GetComponent<MoneyUIInitializer>().Init();
        GameManager.Instance.AddBackFunction(OnEscapeKeyPreesed, 0);
    }

    /// <summary>
    /// 메인 카메라 설정
    /// </summary>
    private void SetMainCameraProperty()
    {
        _mainCameraProperty.Value = MainCamera;
    }

    /// <summary>
    /// 레드닷 초기화
    /// </summary>
    private void InitializeRedDots()
    {
        GetComponentsInChildren<RedDot>(true).ToList().ForEach(x => x.Init());
    }

    /// <summary>
    /// 플로팅 UI 아이템 오브젝트 풀 초기화
    /// </summary>
    void InitFloatingUIPools()
    {
        foreach (var itemType in FloatingUIItemPrefabs.Keys)
        {
            objectPools[itemType] = CreateObjectPool(FloatingUIItemPrefabs[itemType]);
        }
    }

    /// <summary>
    /// 구독 해제
    /// </summary>
    private void UnSubscribes()
    {
        _disposables?.DisposeAll();
        _disposables = null;
    }

    /// <summary>
    /// UI에 필요한 데이터 구독하기
    /// </summary>
    private void Subscribes()
    {
        CharacterManager.Instance.onEvolutionStart += OnEvolutionStart;
        CharacterManager.Instance.onEvolutionFinished += OnEvolutionFinished;

        _disposables = new IDisposable[]
        {
            StaticData.Player.SubscribeLevelUpEvent(OnLevelUp).AddTo(this),
            StaticData.Player.SubscribeLevelNExp((level, exp) =>
            {
                var playerNeedExp = StaticData.Player.GetNeedExp();
                Logger.Log("UIManager.Subscribes() - level: " + level + "\n"
                           + "exp: " + exp + "\n"
                           + "max exp: " + playerNeedExp);

                TopViewInstance.LevelInfo.GetText("level").text = level.ToNumber();
                TopViewInstance.LevelInfo.GetImage("gauge").fillAmount = (float)exp / playerNeedExp;
            }).AddTo(this),
            StaticData.Character.CareData.SubscribeIsClear((isClear) =>
                BottomViewInstance.PressTouchText.SetActiveGameObject(isClear)).AddTo(this),
            VisualController.CurrentIngameWaypoint.Subscribe(OnIngameWayPointChanged).AddTo(this),
            (BottomViewInstance.FoodInventory as FoodInventory)?.Subscribe().AddTo(this),
            RoomDecorator.SubscribeDecorateMode(OnDecorateModeChanged).AddTo(this),
            RoomDecorator.SubscribeSelectedRoomItem(OnSelectedRoomItemChanged).AddTo(this),
            IAPManager.Instance.Subscribe(MarketPidKeys.removeAds1,
                OnHasRemoveAds1Changed).AddTo(this),
            IAPManager.Instance.Subscribe(MarketPidKeys.removeAds2,
                OnHasRemoveAds2Changed).AddTo(this),
            SceneController.OnSceneChanged.Add(OnSceneChanged),
            SceneController.OnLoadingStateChanged.Add(OnLoadingStateChanged),
            Disposable.Create(() => CharacterManager.Instance.onEvolutionStart -= OnEvolutionStart),
            Disposable.Create(() => CharacterManager.Instance.onEvolutionFinished -= OnEvolutionFinished)
        };
    }

    /// <summary>
    /// 광고 제거 버튼 활성화 상태 업데이트
    /// </summary>
    void UpdateRemoveAdsButtonVisible()
    {
        var sceneName = SceneController.Instance.CurrentSceneName;

        TopViewInstance.RemoveAds1.SetActiveGameObject(sceneName == SceneName.Ingame && _hasRemoveAds1 == false);
        TopViewInstance.RemoveAds2.SetActiveGameObject(sceneName == SceneName.Ingame && _hasRemoveAds1 &&
                                                       _hasRemoveAds2 == false);
    }

    /// <summary>
    /// 광고 제거 상품 1 구매 여부 변경 시
    /// </summary>
    /// <param name="has"></param>
    private void OnHasRemoveAds1Changed(bool has)
    {
        _hasRemoveAds1 = has;
        UpdateRemoveAdsButtonVisible();
    }

    /// <summary>
    /// 광고 제거 상품 2 구매 여부 변경 시
    /// </summary>
    /// <param name="has"></param>
    private void OnHasRemoveAds2Changed(bool has)
    {
        _hasRemoveAds2 = has;
        UpdateRemoveAdsButtonVisible();
    }

    /// <summary>
    /// 로딩 상태 변경 시
    /// </summary>
    /// <param name="isEnterLoading"></param>
    private void OnLoadingStateChanged(bool isEnterLoading)
    {
        SetUIVisibility(isEnterLoading == false);
    }

    /// <summary>
    /// Scene 변경 시
    /// </summary>
    /// <param name="sceneName"></param>
    public void OnSceneChanged(SceneName sceneName)
    {
        SetTopUIVisibility(true);
        SetBottomUIVisibility(true);

        if (Tutorial.Instance.IsComplete(TutorialType.Initial) == false) return;

        TopViewInstance.ToHomeButton.SetActiveGameObject(sceneName == SceneName.CollectiblesLand);
        TopViewInstance.ToIslandButton.SetActiveGameObject(sceneName == SceneName.Ingame);

        // TopViewInstance.LevelInfo.SetActiveGameObject(sceneName != SceneName.CollectiblesLand);
        TopViewInstance.Coin.SetActiveGameObject(sceneName != SceneName.CollectiblesLand);
        TopViewInstance.Gem.SetActiveGameObject(sceneName != SceneName.CollectiblesLand);
        TopViewInstance.MiniGame.SetActiveGameObject(sceneName == SceneName.CollectiblesLand);

        TopViewInstance.Option.SetActiveGameObject(sceneName != SceneName.CollectiblesLand);
        // TopViewInstance.Shop.SetActiveGameObject(sceneName != SceneName.CollectiblesLand);
        TopViewInstance.DailyMission.SetActiveGameObject(sceneName != SceneName.CollectiblesLand);
        TopViewInstance.AttendanceCheck.SetActiveGameObject(sceneName != SceneName.CollectiblesLand);
        TopViewInstance.CharCollection.SetActiveGameObject(sceneName != SceneName.CollectiblesLand);

        UpdateRemoveAdsButtonVisible();

        if (sceneName.UseDifferentCameraSystem() == false)
            SetMainCameraProperty(MainCamera);
        MainCamera.SetActiveGameObject(sceneName.UseDifferentCameraSystem() == false);

        BottomViewInstance.CareCanvasGroup.AutoSet(sceneName == SceneName.Ingame ? 1f : 0f);
        BottomViewInstance.BedroomCanvasGroup.AutoSet(sceneName == SceneName.Ingame ? 1f : 0f);
        BottomViewInstance.MiniGameCanvasGroup.AutoSet(sceneName == SceneName.Ingame ? 1f : 0f);
        BottomViewInstance.EvolutionBar.SetActiveGameObject(sceneName == SceneName.Ingame);
        BottomViewInstance.DecorateButton.SetActiveGameObject(sceneName == SceneName.Ingame);
        BottomViewInstance.CollectiblesCanvasGroup.AutoSet(sceneName == SceneName.CollectiblesLand ? 1f : 0f);
        BottomViewInstance.FocusingUIGroup.AutoSet(0f);
        BottomViewInstance.BathroomGroup.AutoSet(sceneName == SceneName.Ingame ? 1f : 0f);

        BottomViewInstance.CheatGroup.AutoSet(sceneName == SceneName.Ingame ? 1f : 0f);

        if (uIAnimationSequence == null)
        {
            uIAnimationSequence = GetComponent<UIAnimationSequence>();
        }
        else if (sceneName == SceneName.Ingame)
        {
            uIAnimationSequence.PlayAnimationsInSequence();
        }
    }

    /// <summary>
    /// 미니 게임 UI 설정
    /// </summary>
    /// <param name="setting"></param>
    public void SetMiniGameUI(MiniGameUISetting setting)
    {
        TopViewInstance.MiniGame.GetCustomComponent<Transform>("playable").SetActiveGameObject(setting.IsPlayable);
        TopViewInstance.MiniGame.GetCustomComponent<Transform>("unablePlay")
            .SetActiveGameObject(setting.IsPlayable == false);
        TopViewInstance.MiniGame.GetImage("char").sprite = setting.Thumbnail;
        TopViewInstance.MiniGame.GetImage("uChar").sprite = setting.Thumbnail;
        TopViewInstance.MiniGame.GetCustomComponent<TimerToTMP>("timer").Initialize(setting.Cooldown);
        TopViewInstance.MiniGame.GetComponent<Button>().interactable = setting.IsPlayable;
    }

    /// <summary>
    /// 메인 카메라 설정
    /// </summary>
    /// <param name="mainCam"></param>
    public void SetMainCameraProperty(Camera mainCam)
    {
        _mainCameraProperty.Value = mainCam;
    }

    /// <summary>
    /// 진화 뷰를 보여주기
    /// </summary>
    public void ShowEvolutionView()
    {
        SetUIVisibility(false);
        RemoveAllFloatingUIItems();
        UpdateEvolutionView();
        EvolutionViewInstance._CanvasGroup.AutoSet(1f);
        _isEvolutionViewOpened.Value = new EvolutionViewState(true);
    }

    /// <summary>
    /// 진화 뷰 업데이트
    /// </summary>
    void UpdateEvolutionView()
    {
        var cType = StaticData.Character.GetAppearance().CType;
        var overMaxLevel = StaticData.CharacterCollection.GetOverMaxLevel(cType);
        var level = StaticData.CharacterCollection.GetCharacterLevel(cType);

        var priceDatas = DataTableLoader.Instance.GetDataAsDict<LevelUpPriceData>();
        if (overMaxLevel)
        {
            EvolutionViewInstance.levelUpButton.GetText("price").text = "0";
        }
        else if (priceDatas.TryGetValue(level + 1, out var priceData) == false)
        {
            Logger.LogWarning($"{level + 1}에 해당하는 가격 없음");
        }
        else
        {
            var levelUpCost = priceData.GetValue(cType);
            var splited = levelUpCost.Split('_');
            var amount = splited[0].ToInt();
            var moneyType = splited[1];
            var moneyIcon = UITextureLoader.Instance.GetSprite($"icon_main_{moneyType}");

            EvolutionViewInstance.levelUpButton.GetImage("icon").sprite = moneyIcon;
            EvolutionViewInstance.levelUpButton.GetText("price").text = amount.ToNumber();
        }

        EvolutionViewInstance.levelUpButton.GetCustomComponent<UIEffect>("ui").colorFactor =
            overMaxLevel == false ? 0f : 1f;
        EvolutionViewInstance.levelUpButton.GetText("desc").text =
            overMaxLevel ? "max".ToLocalize() : "levelup_ui".ToLocalize();

        var hasAllSkin = StaticData.CharacterCollection.IsAllCollected();
        EvolutionViewInstance.newSkinButton.GetCustomComponent<UIEffect>("ui").colorFactor =
            hasAllSkin == false ? 0f : 1f;
        EvolutionViewInstance.newSkinButton.SetInteractable(hasAllSkin == false);

        var newSkinAmount = "evolution_need_gem".ToCommon().ToInt();
        EvolutionViewInstance.newSkinButton.GetText("price").text = newSkinAmount.ToNumber();
    }

    /// <summary>
    /// 진화 뷰 닫기
    /// </summary>
    /// <param name="isRequiredMoveToOriginalRoom"></param>
    public void CloseEvolutionView(bool isRequiredMoveToOriginalRoom)
    {
        SetUIVisibility(true);
        EvolutionViewInstance._CanvasGroup.AutoSet(0f);
        _isEvolutionViewOpened.Value = new EvolutionViewState(false, isRequiredMoveToOriginalRoom);
    }

    /// <summary>
    /// 진화 뷰 상태 변경 시
    /// </summary>
    /// <param name="roomItem"></param>
    private void OnSelectedRoomItemChanged(RoomItem roomItem)
    {
        if (roomItem == null) return;

        var originalItem = RoomDecorator.GetOriginalItem();

        DecorateViewInstance.nameText.GetText("text").text = roomItem.itemName.ToLocalize();
        var iconName = roomItem.moneyType == MoneyType.Coin ? "icon_main_coin" : "icon_main_gem";
        DecorateViewInstance.buyButton.GetImage("icon").sprite = UITextureLoader.Instance.GetSprite(iconName);
        DecorateViewInstance.buyButton.GetText("price").text = roomItem.price.ToNumber();
        DecorateViewInstance.notReachedButton.GetText("text").text = "decorate_notReached".ToLocalize(roomItem.needLv);

        var isNotReached = StaticData.Player.GetLevel() < roomItem.needLv;
        var isAlreadyOwned = StaticData.Room.IsAlreadyOwned(roomItem.itemName);
        var isSelected = roomItem == originalItem;
        var isBuyable = !isSelected && !isNotReached && !isAlreadyOwned;

        DecorateViewInstance.lockIcon.SetActiveGameObject(isNotReached);
        DecorateViewInstance.notReachedButton.SetActiveGameObject(isNotReached);
        DecorateViewInstance.selectButton.SetActiveGameObject(!isSelected && isAlreadyOwned);
        DecorateViewInstance.selectedButton.SetActiveGameObject(isSelected);
        DecorateViewInstance.checkIcon.SetActiveGameObject(isSelected);
        DecorateViewInstance.buyButton.SetActiveGameObject(isBuyable);
    }

    /// <summary>
    /// 진화 시작 시
    /// </summary>
    private void OnEvolutionStart()
    {
        _isEvolving = true;

        SetDecorateButton();
    }

    /// <summary>
    /// 진화 완료 시
    /// </summary>
    private void OnEvolutionFinished()
    {
        _isEvolving = false;

        SetDecorateButton();
    }

    /// <summary>
    /// 플레이어 레벨 업 시 
    /// </summary>
    /// <param name="currentLevel"></param>
    /// <param name="rewards"></param>
    private void OnLevelUp(int currentLevel, LevelRewards rewards)
    {
        PopupManager.Instance.ShowLevelUpEvent(currentLevel, rewards);
    }

    /// <summary>
    /// 웨이포인트 변경 시
    /// </summary>
    /// <param name="waypoint"></param>
    private void OnIngameWayPointChanged(IngameWaypoint waypoint)
    {
        UpdateWaypointRelatedUIItems(waypoint);

        playingPoopParticleList.ForEach(x => ParticlePool.Instance.ReturnParticle("poopClear", x.gameObject));
        playingPoopParticleList.Clear();

        CharacterManager.Instance.StopAction();
    }

    /// <summary>
    /// 씻겨주세요 텍스트를 표시 할 조건인지
    /// </summary>
    /// <param name="waypoint"></param>
    /// <returns></returns>
    public bool IsShowWashMeTextCondition(IngameWaypoint waypoint)
    {
        return Tutorial.Instance.IsComplete(TutorialType.Initial) &&
               waypoint == IngameWaypoint.Bathroom &&
               StaticData.Character.CareData.GetCareValue(CareType.Clearness) < 20;
    }

    /// <summary>
    /// 미니 게임 캔버스 그룹을 표시 할 조건인지
    /// </summary>
    /// <param name="waypoint"></param>
    /// <returns></returns>
    public bool IsShowMiniGameCanvasGroupCondition(IngameWaypoint waypoint)
    {
        return Tutorial.Instance.IsComplete(TutorialType.Initial) && waypoint != IngameWaypoint.Bathtub &&
               waypoint != IngameWaypoint.Toilets;
    }

    /// <summary>
    /// 웨이포인트 관련 UI 항목 업데이트
    /// </summary>
    /// <param name="waypoint"></param>
    private void UpdateWaypointRelatedUIItems(IngameWaypoint waypoint)
    {
        SetDecorateButton();
        SetAllUIItemsToFalse();

        TopViewInstance.ToIslandButton.SetActiveGameObject(waypoint != IngameWaypoint.Bathtub &&
                                                           waypoint != IngameWaypoint.Toilets);

        BottomViewInstance.MiniGameCanvasGroup.SetActiveGameObject(IsShowMiniGameCanvasGroupCondition(waypoint));

        BottomViewInstance.BathroomGroup.AutoSet(waypoint == IngameWaypoint.Bathroom ? 1f : 0f);
        BottomViewInstance.WashMeText.SetActiveGameObject(IsShowWashMeTextCondition(waypoint));

        // Enable required UI items for specific waypoints
        switch (waypoint)
        {
            case IngameWaypoint.Bedroom:
                BottomViewInstance.AdSkipSleepTime.SetActiveGameObject(false);
                BottomViewInstance.CoinSkipSleepTime.SetActiveGameObject(false);
                BottomViewInstance.GemSkipSleepTime.SetActiveGameObject(false);
                BottomViewInstance.WakeupTime.SetActiveGameObject(false);
                break;
            case IngameWaypoint.Bathtub:
                ShowerViewInstance.GetComponent<CanvasGroup>().AutoSet(1);
                ShowerViewInstance.DragSoapText.SetActiveGameObject(true);
                break;
        }
    }

    /// <summary>
    /// 모든 UI 항목을 비활성화
    /// </summary>
    private void SetAllUIItemsToFalse()
    {
        BottomViewInstance.WashMeText.SetActiveGameObject(false);
        BottomViewInstance.AdSkipSleepTime.SetActiveGameObject(false);
        BottomViewInstance.CoinSkipSleepTime.SetActiveGameObject(false);
        BottomViewInstance.GemSkipSleepTime.SetActiveGameObject(false);
        BottomViewInstance.WakeupTime.SetActiveGameObject(false);
        BottomViewInstance.FoodInventory.SetActiveGameObject(false);
        ShowerViewInstance.DragSoapText.SetActiveGameObject(false);
        ShowerViewInstance.GetComponent<CanvasGroup>().AutoSet(0);
    }

    /// <summary>
    /// UI 액션 설정하기
    /// </summary>
    private void SetUIAction()
    {
        SetTopViewUIAction();
        SetBottomViewUIAction();
        SetDecorateViewUIAction();
        SetEvolutionViewUIAction();
    }

    /// <summary>
    /// 상단 뷰 UI 액션 설정
    /// </summary>
    void SetTopViewUIAction()
    {
        TopViewInstance.Option.Clickable(OnClickOption);
        TopViewInstance.Shop.Clickable(OnClickShop);
        TopViewInstance.DailyMission.Clickable(OnClickDailyMission);
        TopViewInstance.AttendanceCheck.Clickable(OnClickAttendanceCheck);
        TopViewInstance.Coin.Clickable(OnClickCoin);
        TopViewInstance.Gem.Clickable(OnClickGem);
        TopViewInstance.LevelInfo.Clickable(OnClickLevelInfo);
        TopViewInstance.CharCollection.Clickable(OnClickCharCollection);
        TopViewInstance.RemoveAds1.Clickable(OnClickRemoveAds1);
        TopViewInstance.RemoveAds2.Clickable(OnClickRemoveAds2);
        TopViewInstance.ToIslandButton.Clickable(OnClickToIsland);
        TopViewInstance.ToHomeButton.Clickable(OnClickToHome);
        TopViewInstance.MiniGame.Clickable(OnClickMiniGame);
    }

    /// <summary>
    /// 하단 뷰 UI 액션 설정
    /// </summary>
    void SetBottomViewUIAction()
    {
        BottomViewInstance.CarePlay.Clickable(() => { OnClickCareItem(CareType.Play).Forget(); });
        BottomViewInstance.CareSleepy.Clickable(() => { OnClickCareItem(CareType.Sleepy).Forget(); });
        BottomViewInstance.CareHungry.Clickable(() => { OnClickCareItem(CareType.Hungry).Forget(); });
        BottomViewInstance.CareClearness.Clickable(() => { OnClickCareItem(CareType.Clearness).Forget(); });

        BottomViewInstance.CoinSkipSleepTime.Clickable(OnClickSkipSleepTimeButtonWithCoin);
        BottomViewInstance.GemSkipSleepTime.Clickable(OnClickSkipSleepTimeButtonWithGem);

        BottomViewInstance.AdSkipSleepTime.Clickable(OnClickSkipSleepTimeButtonWithAds);

        BottomViewInstance.MinigameUIButton.Clickable(OnClickMinigameUIButton);
        BottomViewInstance.DecorateButton.Clickable(OnClickDecorateButton);
        BottomViewInstance.SkipButton.Clickable(OnClickSkipButton);
    }

    /// <summary>
    /// 진화 뷰 UI 액션 설정
    /// </summary>
    void SetEvolutionViewUIAction()
    {
        EvolutionViewInstance.levelUpButton.Clickable(OnClickLevelUpButton);
        EvolutionViewInstance.newSkinButton.Clickable(OnClickNewSkinButton);
        EvolutionViewInstance.skinChangeButton.Clickable(OnClickSkinChangeButton);
        EvolutionViewInstance.closeButton.Clickable(OnClickEvolutionClose);
    }

    /// <summary>
    /// 레벨 업 버튼 클릭 시
    /// </summary>
    private void OnClickLevelUpButton()
    {
        if (CharacterManager.Instance.IsEvolutionReady(out var evolutionState) == false)
        {
            PopupManager.Instance.ShowToast("cant_evolve".ToLocalize(), false);

            return;
        }

        var cType = StaticData.Character.GetAppearance().CType;
        var isMaxLevel = StaticData.CharacterCollection.GetOverMaxLevel(cType);
        if (isMaxLevel)
        {
            PopupManager.Instance.ShowToast("charlv_max".ToLocalize(), false);
            return;
        }

        if (ParseLevelUpCost(cType, out var amount, out var moneyType) == false)
        {
            PopupManager.Instance.ShowToast("cant_evolve".ToLocalize(), false);
            return;
        }

        bool tryBuy = false;
        if (moneyType == "coin")
            tryBuy = StaticData.Player.TrySubtractCoin(amount);
        else if (moneyType == "gem")
            tryBuy = StaticData.Player.TrySubtractGem(amount);
        else
        {
            Logger.LogError("Invalid money type");
            return;
        }

        if (tryBuy == false)
        {
            PopupManager.Instance.ShowYes(new PopupYesNo.YesSetting("go_to_store".ToLocalize(), () => GotoStore()));
            return;
        }

        CloseEvolutionView(false);
        CharacterManager.Instance.StartEvolutionIfReady(true);
    }

    /// <summary>
    /// 레벨업 비용 파싱
    /// </summary>
    /// <param name="cType"></param>
    /// <param name="amount"></param>
    /// <param name="moneyType"></param>
    /// <returns></returns>
    private bool ParseLevelUpCost(CharacterType cType, out int amount, out string moneyType)
    {
        amount = 0;
        moneyType = "";

        try
        {
            var level = StaticData.CharacterCollection.GetCharacterLevel(cType);

            var priceDatas = DataTableLoader.Instance.GetDataAsDict<LevelUpPriceData>();
            if (priceDatas.TryGetValue(level + 1, out var priceData) == false)
            {
                Logger.LogWarning($"{level + 1}에 해당하는 가격 없음");
                return false;
            }
            else
            {
                var levelUpCost = priceData.GetValue(cType);
                var splited = levelUpCost.Split('_');
                amount = splited[0].ToInt();
                moneyType = splited[1];
            }

            return true;
        }
        catch (Exception e)
        {
            Logger.LogException(e);
            return false;
        }
    }

    /// <summary>
    /// 새 스킨 버튼 클릭 시
    /// </summary>
    private void OnClickNewSkinButton()
    {
        if (CharacterManager.Instance.IsEvolutionReady(out var evolutionState) == false)
        {
            PopupManager.Instance.ShowToast("cant_evolve".ToLocalize(), false);
            return;
        }

        var cost = "evolution_need_gem".ToCommon().ToInt();

        var tryBuy = StaticData.Player.TrySubtractGem(cost);
        if (tryBuy == false)
        {
            PopupManager.Instance.ShowYes(new PopupYesNo.YesSetting("go_to_store".ToLocalize(), () => GotoStore()));
            return;
        }

        CloseEvolutionView(false);
        CharacterManager.Instance.StartEvolutionIfReady(false);
    }

    /// <summary>
    /// 상점으로 이동
    /// </summary>
    /// <param name="tab"></param>
    private void GotoStore(ShopTab tab = ShopTab.Money)
    {
        PopupManager.Instance.ShowShop(tab);
    }

    /// <summary>
    /// 캐릭터 스킨 페이지로 이동
    /// </summary>
    private void GotoSkinPage()
    {
        PopupManager.Instance.ShowCharInfo(PopupCharInfo.TabPage.Skin);
    }

    /// <summary>
    /// 스킨 변경 버튼 클릭 시
    /// </summary>
    private void OnClickSkinChangeButton()
    {
        GotoSkinPage();
        CloseEvolutionView(true);
    }

    /// <summary>
    /// 진화 뷰 닫기 버튼 클릭 시
    /// </summary>
    private void OnClickEvolutionClose()
    {
        CloseEvolutionView(true);
    }

    /// <summary>
    /// 스킵 버튼 클릭 시
    /// </summary>
    private void OnClickSkipButton()
    {
        BottomViewInstance.SkipButton.GetComponent<EventRaise>()?.Raise();
    }

    /// <summary>
    /// 방 장식 뷰 UI 액션 설정
    /// </summary>
    void SetDecorateViewUIAction()
    {
        DecorateViewInstance.closeButton.Clickable(() =>
            RoomDecorator.SetIsDecorateMode(new DecorateModeSetting(false,
                VisualController.CurrentIngameWaypoint.Value.ToRoomType())));

        DecorateViewInstance.leftButton.Clickable(() => RoomDecorator.PrevRoomItem());
        DecorateViewInstance.rightButton.Clickable(() => RoomDecorator.NextRoomItem());
        DecorateViewInstance.selectButton.Clickable(() => RoomDecorator.SelectRoomItem());
        DecorateViewInstance.buyButton.Clickable(() =>
        {
            var result = RoomDecorator.BuyRoomItem();
            if (result.IsSuccess)
            {
                OnSelectedRoomItemChanged(result.Item);
                PopupManager.Instance.ShowToast("purchase_success_item".ToLocalize(result.Item.itemName.ToLocalize()),
                    true);
                Logger.Log(result.Message);
            }
            else
            {
                Logger.LogWarning(result.Message);
            }
        });
        DecorateViewInstance.selectButton.Clickable(() =>
        {
            var item = RoomDecorator.SelectRoomItem();
            OnSelectedRoomItemChanged(item);
        });
    }

    /// <summary>
    /// 레벨 정보 버튼 클릭 시
    /// </summary>
    private void OnClickLevelInfo()
    {
        PopupManager.Instance.ShowPlayerLevel();
    }

    /// <summary>
    /// 캐릭터 컬렉션 버튼 클릭 시
    /// </summary>
    private void OnClickCharCollection()
    {
        PopupManager.Instance.ShowCharCollection();
    }

    /// <summary>
    /// 광고 제거 상품 1 클릭 시
    /// </summary>
    private void OnClickRemoveAds1()
    {
        PopupManager.Instance.ShowPackage(new PackageSetting(PackageType.RemoveAds1));
    }

    /// <summary>
    /// 광고 제거 상품 2 클릭 시
    /// </summary>
    private void OnClickRemoveAds2()
    {
        PopupManager.Instance.ShowPackage(new PackageSetting(PackageType.RemoveAds2));
    }

    /// <summary>
    /// 섬 버튼 클릭 시
    /// </summary>
    private void OnClickToIsland()
    {
        if (SceneController.Instance.IsLoading)
            return;

        SceneController.Instance.LoadSceneAsync(SceneName.CollectiblesLand, true);
    }

    /// <summary>
    /// 홈 버튼 클릭 시
    /// </summary>
    private void OnClickToHome()
    {
        if (SceneController.Instance.IsLoading)
            return;

        SceneController.Instance.LoadSceneAsync(SceneName.Ingame, true);
        ObjectTouchHandler.Instance.ChangeMode(ObjectTouchHandler.Mode.Single);
    }

    /// <summary>
    /// 상단 미니 게임 버튼 클릭 시
    /// </summary>
    private void OnClickMiniGame()
    {
        miniGameClickEvent.Raise();
    }

    /// <summary>
    /// 재화로 수면 시간 스킵
    /// </summary>
    /// <param name="moneyType"></param>
    private void BuyMoneySkipSleepTime(MoneyType moneyType)
    {
        if (moneyType == MoneyType.Gem && StaticData.Player.TrySubtractGem(1))
        {
        }
        else if (moneyType == MoneyType.Coin && StaticData.Player.TrySubtractCoin(2000))
        {
        }
        else
        {
            PopupManager.Instance.ShowToast("not_enough_gem".ToLocalize(), false);
            return;
        }

        StaticData.Character.CareData.UpdateCareValue(CareType.Sleepy, 100);
    }

    /// <summary>
    /// 케어 아이템 클릭 시
    /// </summary>
    /// <param name="careType"></param>
    /// <returns></returns>
    private async UniTaskVoid OnClickCareItem(CareType careType)
    {
        CareItem careItem = careItems[careType];
        var val = StaticData.Character.CareData.GetCareValue(careType);
        careItem.PercentTextAppearAnimation(val);

        if (StaticData.Character.CareData.ShouldBlockMoveToWaypoint(careType) == false)
        {
            careItems[StaticData.Character.CareData.GetHighestPriorityCareType()].RotateAnimation();
            CharacterManager.Instance.PlayCharacterSound(CharacterSoundType.Refusal);
            CharacterManager.Instance.PlayAction(CharacterActionType.RotateHead, false);
            return;
        }

        IngameWaypoint waypoint;
        switch (careType)
        {
            case CareType.Play:
                waypoint = IngameWaypoint.Mainroom;
                break;
            case CareType.Sleepy:
                waypoint = IngameWaypoint.Bedroom;
                break;
            case CareType.Hungry:
                waypoint = IngameWaypoint.Kitchen;
                break;
            case CareType.Clearness:
                waypoint = IngameWaypoint.Bathroom;
                break;
            default:
                return; // If careType is not handled, do nothing
        }

        await AdManager.Instance.ShowAd(AdKeys.RoomChanged);
        VisualController.Instance.MoveToWaypoint(waypoint);
    }

    /// <summary>
    /// 옵션 버튼 클릭시 호출되는 메서드
    /// </summary>
    private void OnClickOption()
    {
        PopupManager.Instance.ShowOption();
    }

    /// <summary>
    /// 상점 버튼 클릭시 호출되는 메서드
    /// </summary>
    private void OnClickShop()
    {
        PopupManager.Instance.ShowShop();
    }

    /// <summary>
    /// 코인 버튼 클릭시 호출되는 메서드
    /// </summary>
    private void OnClickCoin()
    {
        PopupManager.Instance.ShowShop(ShopTab.Money);
    }

    /// <summary>
    /// 젬 버튼 클릭시 호출되는 메서드
    /// </summary>
    private void OnClickGem()
    {
        PopupManager.Instance.ShowShop(ShopTab.Money);
    }

    /// <summary>
    /// 일일 미션 버튼 클릭시 호출되는 메서드
    /// </summary>
    private void OnClickDailyMission()
    {
        PopupManager.Instance.ShowDailyMission();
    }

    /// <summary>
    /// 출석 체크 버튼 클릭시 호출되는 메서드
    /// </summary>
    private void OnClickAttendanceCheck()
    {
        // 광고 제거 아이템 구매
        PopupManager.Instance.ShowAttendanceCheck();
    }

    /// <summary>
    /// 미션 알림 활성화 설정
    /// </summary>
    /// <param name="isActive"></param>
    public void SetAlertMission(bool isActive)
    {
        TopViewInstance.DailyMission.GetImage("icon_new").SetActiveGameObject(isActive);
    }

    /// <summary>
    /// 출석 체크 알림 활성화 설정
    /// </summary>
    /// <param name="isActive"></param>
    public void SetAlertAttendance(bool isActive)
    {
        TopViewInstance.AttendanceCheck.GetImage("icon_new").SetActiveGameObject(isActive);
    }

    /// <summary>
    /// 하단 미니 게임 버튼 클릭시 호출되는 메서드
    /// </summary>
    private void OnClickMinigameUIButton()
    {
        PopupManager.Instance.ShowMiniGames();
    }

    /// <summary>
    /// 방 장식 모드 변경 시 호출되는 메서드
    /// </summary>
    /// <param name="setting"></param>
    void OnDecorateModeChanged(DecorateModeSetting setting)
    {
        if (setting == null)
        {
            SetDecorateUIVisibility(false);
            return;
        }

        if (setting.IsDecorateMode)
        {
            if (!RoomConfig.Instance.TryGetRoomSetting(setting.RoomType, out var roomSetting))
            {
                Logger.LogError($"RoomSetting not found. RoomType : {setting.RoomType}");
                return;
            }


            var roomCategories = Enumerable.ToHashSet(roomSetting.roomCategories);
            DecorateViewInstance.categoryUIItemGroup.Set(roomCategories);
            DecorateViewInstance.GetComponent<UIAnimationSequence>()?.PlayAnimationsInSequence();
        }
        else
        {
        }

        SetDecorateUIVisibility(setting.IsDecorateMode);
        TopViewInstance.LeftTopButtons.SetActiveGameObject(!setting.IsDecorateMode);
        TopViewInstance.RightTopButtons.SetActiveGameObject(!setting.IsDecorateMode);
        SetBottomUIVisibility(!setting.IsDecorateMode);
    }

    /// <summary>
    /// 코인으로 수면 시간 스킵 버튼 클릭 시
    /// </summary>
    private void OnClickSkipSleepTimeButtonWithCoin()
    {
        BuyMoneySkipSleepTime(MoneyType.Coin);
    }

    /// <summary>
    /// 젬으로 수면 시간 스킵 버튼 클릭 시
    /// </summary>
    private void OnClickSkipSleepTimeButtonWithGem()
    {
        BuyMoneySkipSleepTime(MoneyType.Gem);
    }

    /// <summary>
    /// 광고로 수면 시간 스킵 버튼 클릭 시
    /// </summary>
    private void OnClickSkipSleepTimeButtonWithAds()
    {
        bool _isAdSkipClicked = false;

        async void OnClickYes()
        {
            if (_isAdSkipClicked) return;
            _isAdSkipClicked = true;

            var result = Tutorial.Instance.IsComplete(TutorialType.Initial)
                ? await AdManager.Instance.ShowAd(AdKeys.SleepTimeSkip)
                : new RewardAdResult(true);

            if (result.IsReward)
            {
                StaticData.Character.CareData.UpdateCareValue(CareType.Sleepy, 100);
                PopupManager.Instance.HideYesNo();
            }

            _isAdSkipClicked = false;
        }

        var setting =
            new PopupYesNo.YesNoSetting("reduce_sleep_time".ToLocalize(), OnClickYes, autoHideOnClickYes: false);
        PopupManager.Instance.ShowYesNo(setting);
    }

    /// <summary>
    /// 방 장식 버튼 클릭 시
    /// </summary>
    private void OnClickDecorateButton()
    {
        RoomDecorator.SetIsDecorateMode(new DecorateModeSetting(true,
            VisualController.CurrentIngameWaypoint.Value.ToRoomType()));
    }

    /// <summary>
    /// UI 가시성 설정
    /// </summary>
    /// <param name="isVisible"></param>
    public void SetUIVisibility(bool isVisible)
    {
        SetTopUIVisibility(isVisible);
        SetBottomUIVisibility(isVisible);
    }

    /// <summary>
    /// 상단 UI 가시성 설정
    /// </summary>
    /// <param name="isVisible"></param>
    public void SetTopUIVisibility(bool isVisible)
    {
        TopViewInstance?._CanvasGroup.AutoSet(isVisible ? 1f : 0f);
    }

    /// <summary>
    /// 하단 UI 가시성 설정
    /// </summary>
    /// <param name="isVisible"></param>
    public void SetBottomUIVisibility(bool isVisible)
    {
        BottomViewInstance?._CanvasGroup.AutoSet(isVisible ? 1f : 0f);
    }

    /// <summary>
    /// 장식 버튼 가시성 설정
    /// </summary>
    /// <param name="isVisible"></param>
    public void SetDecorateUIVisibility(bool isVisible)
    {
        DecorateViewInstance?._CanvasGroup.AutoSet(isVisible ? 1f : 0f);
    }

    /// <summary>
    /// 진화 바 초기화
    /// </summary>
    private void InitializeEvolutionBar()
    {
        BottomViewInstance.EvolutionBar.Init();
    }

    /// <summary>
    /// 케어 아이템 초기화
    /// </summary>
    private void InitializeCareItems()
    {
        careItems = new Dictionary<CareType, CareItem>
        {
            { CareType.Play, BottomViewInstance.CarePlay as CareItem },
            { CareType.Hungry, BottomViewInstance.CareHungry as CareItem },
            { CareType.Sleepy, BottomViewInstance.CareSleepy as CareItem },
            { CareType.Clearness, BottomViewInstance.CareClearness as CareItem }
        };

        IEnumerable<IDisposable> CareSub = new List<IDisposable>()
        {
            StaticData.Character.CareData.SubscribeCareValue(CareType.Play,
                x => { SetCareGauge(CareType.Play, x * 0.01f); }).AddTo(this),
            StaticData.Character.CareData.SubscribeCareValue(CareType.Hungry,
                x => { SetCareGauge(CareType.Hungry, x * 0.01f); }).AddTo(this),
            StaticData.Character.CareData.SubscribeCareValue(CareType.Sleepy,
                x => { SetCareGauge(CareType.Sleepy, x * 0.01f); }).AddTo(this),
            StaticData.Character.CareData.SubscribeCareValue(CareType.Clearness,
                x =>
                {
                    BottomViewInstance.WashMeText.SetActiveGameObject(
                        IsShowWashMeTextCondition(VisualController.CurrentIngameWaypoint.Value));
                    SetCareGauge(CareType.Clearness, x * 0.01f);
                }).AddTo(this),

            StaticData.Character.CareData.SubscribeIsSleep(x =>
            {
                if (SceneController.Instance.CurrentSceneName == SceneName.Ingame)
                {
                    int alpha = x ? 0 : 1;
                    TopViewInstance._CanvasGroup.AutoSet(alpha);
                    BottomViewInstance.CareCanvasGroup.AutoSet(alpha);
                    BottomViewInstance.MiniGameCanvasGroup.AutoSet(alpha);
                    BottomViewInstance.EvolutionBar.SetActiveGameObject(alpha == 1);

                    SetDecorateButton();
                }
            }).AddTo(this)
        };

        _disposables = _disposables.Concat(CareSub);
    }

    /// <summary>
    /// 케어 버튼 위 화살표 파티클 업데이트
    /// </summary>
    /// <param name="careItem"></param>
    /// <param name="preVal"></param>
    /// <param name="curVal"></param>
    /// <param name="duration"></param>
    private void UpdateUpArrowParticle(CareItem careItem, float preVal, float curVal, float duration)
    {
        if (preVal < curVal)
        {
            careItem.DownArrowParticle.Stop();
            careItem.DownArrowParticle.Clear();

            careItem.UpArrowParticle.Stop();
            careItem.UpArrowParticle.Clear();
            careItem.UpArrowParticle.Play();
            careItem.PercentTextAppearAnimation((int)(preVal * 100f), (int)(curVal * 100f));
        }
    }

    /// <summary>
    /// 케어 버튼 아래 화살표 파티클 업데이트
    /// </summary>
    /// <param name="careItem"></param>
    /// <param name="preVal"></param>
    /// <param name="curVal"></param>
    /// <param name="duration"></param>
    private void UpdateDownArrowParticle(CareItem careItem, float preVal, float curVal, float duration)
    {
        if (preVal > curVal && curVal < preVal - 0.1f)
        {
            careItem.UpArrowParticle.Stop();
            careItem.UpArrowParticle.Clear();

            careItem.DownArrowParticle.Stop();
            careItem.DownArrowParticle.Clear();
            careItem.DownArrowParticle.Play();
            careItem.PercentTextAppearAnimation((int)(preVal * 100f), (int)(curVal * 100f));
        }
    }

    /// <summary>
    /// 케어 게이지 설정
    /// </summary>
    /// <param name="careType"></param>
    /// <param name="value"></param>
    public void SetCareGauge(CareType careType, float value)
    {
        // Get the CareItem for the given CareType
        if (careItems.TryGetValue(careType, out CareItem cache) && cache != null)
        {
            CareGaugeLevel careGaugeLevel = GetCareGaugeLevel(value);

            cache.ImgCareLowGauge.color = new Color(cache.ImgCareLowGauge.color.r, cache.ImgCareLowGauge.color.g,
                cache.ImgCareLowGauge.color.b, careGaugeLevel == CareGaugeLevel.Low ? 1 : 0);

            // Set fillAmount to 1 if value is 0 and careGaugeLevel is VeryLow
            float adjustedFillAmount = value;

            if (fillAmountCoroutines.ContainsKey(careType) && fillAmountCoroutines[careType] != null)
            {
                StopCoroutine(fillAmountCoroutines[careType]);
            }

            float duration = CalculateDuration(cache.ImgCareGauge.fillAmount, adjustedFillAmount);
            fillAmountCoroutines[careType] =
                StartCoroutine(UpdateFillAmount(careType, cache.ImgCareGauge, adjustedFillAmount, duration));

            UpdateUpArrowParticle(cache, cache.ImgCareGauge.fillAmount, adjustedFillAmount, duration);
            UpdateDownArrowParticle(cache, cache.ImgCareGauge.fillAmount, adjustedFillAmount, duration);
        }
        else
        {
            Logger.LogError("CareItem null");
        }
    }

    /// <summary>
    /// 모든 케어 게이지 즉시 설정
    /// </summary>
    public void SetAllCareGaugesInstantly()
    {
        Array careTypes = Enum.GetValues(typeof(CareType));

        foreach (CareType careType in careTypes)
        {
            if (careType != CareType.None)
            {
                float careValue = StaticData.Character.CareData.GetCareValue(careType);
                SetCareGaugeInstantly(careType, careValue * 0.01f);
            }
        }
    }

    /// <summary>
    /// 케어 게이지 즉시 설정
    /// </summary>
    /// <param name="careType"></param>
    /// <param name="value"></param>
    public void SetCareGaugeInstantly(CareType careType, float value)
    {
        // Get the CareItem for the given CareType
        if (careItems.TryGetValue(careType, out CareItem cache) && cache != null)
        {
            CareGaugeLevel careGaugeLevel = GetCareGaugeLevel(value);

            cache.ImgCareLowGauge.color = new Color(cache.ImgCareLowGauge.color.r, cache.ImgCareLowGauge.color.g,
                cache.ImgCareLowGauge.color.b, careGaugeLevel == CareGaugeLevel.Low ? 1 : 0);

            // Set fillAmount to 1 if value is 0 and careGaugeLevel is VeryLow
            float adjustedFillAmount = value;

            if (fillAmountCoroutines.ContainsKey(careType) && fillAmountCoroutines[careType] != null)
            {
                StopCoroutine(fillAmountCoroutines[careType]);
            }

            // Set the fill amount immediately without animation
            cache.ImgCareGauge.fillAmount = adjustedFillAmount;
        }
        else
        {
            Logger.LogError("CareItem null");
        }
    }

    /// <summary>
    /// 애니메이션 시간 계산
    /// </summary>
    /// <param name="currentFillAmount"></param>
    /// <param name="targetFillAmount"></param>
    /// <returns></returns>
    private float CalculateDuration(float currentFillAmount, float targetFillAmount)
    {
        float baseDuration = 0.5f;
        float minDuration = 0.1f;
        float fillAmountDifference = Mathf.Abs(targetFillAmount - currentFillAmount);
        float calculatedDuration = baseDuration * fillAmountDifference;

        // Ensure the duration is at least the minimum duration
        return Mathf.Max(calculatedDuration, minDuration);
    }

    /// <summary>
    /// 케어 게이지 업데이트 코루틴
    /// </summary>
    /// <param name="careType"></param>
    /// <param name="image"></param>
    /// <param name="targetFillAmount"></param>
    /// <param name="duration"></param>
    /// <returns></returns>
    private IEnumerator UpdateFillAmount(CareType careType, Image image, float targetFillAmount, float duration)
    {
        float elapsedTime = 0f;
        float initialFillAmount = image.fillAmount;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float newFillAmount = Mathf.Lerp(initialFillAmount, targetFillAmount, elapsedTime / duration);
            image.fillAmount = newFillAmount;
            yield return null;
        }

        image.fillAmount = targetFillAmount;

        // Remove the completed coroutine from the dictionary
        if (fillAmountCoroutines.ContainsKey(careType))
        {
            fillAmountCoroutines.Remove(careType);
        }
    }

    /// <summary>
    /// 케어 게이지 레벨 가져오기
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private CareGaugeLevel GetCareGaugeLevel(float value)
    {
        if (value <= LowThreshold)
        {
            return CareGaugeLevel.Low;
        }
        else
        {
            return CareGaugeLevel.Normal;
        }
    }

    /// <summary>
    /// 장식 버튼 활성화 설정
    /// </summary>
    public void SetDecorateButton()
    {
        var waypoint = VisualController.CurrentIngameWaypoint.Value;
        var isSleep = StaticData.Character.CareData.GetIsSleep();
        BottomViewInstance.DecorateButton.SetActiveGameObject(
            !isSleep && waypoint != IngameWaypoint.Toilets && waypoint != IngameWaypoint.Bathtub && !_isEvolving);
    }

    /// <summary>
    /// 스크린 페이드 트윈
    /// </summary>
    /// <param name="alpha"></param>
    /// <param name="duration"></param>
    /// <param name="onComplete"></param>
    /// <returns></returns>
    public Tween FadeToScreen(float alpha, float duration, Action onComplete = null)
    {
        return screenFadeImage.DOFade(alpha, duration).OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 로딩 구름 인
    /// </summary>
    /// <returns></returns>
    public Coroutine LoadingCloudIn()
    {
        return loadingAnimation.CloudIn();
    }

    /// <summary>
    /// 로딩 구름 아웃
    /// </summary>
    /// <returns></returns>
    public Coroutine LoadingCloudOut()
    {
        return loadingAnimation.CloudOut();
    }

    /// <summary>
    /// 화면 페이드 이미지 컬러 설정
    /// </summary>
    /// <param name="color"></param>
    public void SetColorFadeScreen(Color color)
    {
        screenFadeImage.color = color;
    }

    /// <summary>
    /// 화면 페이드 이미지 활성화 설정
    /// </summary>
    /// <param name="active"></param>
    public void SetActiveFadeScreen(bool active)
    {
        screenFadeImage.SetActiveGameObject(active);
    }

    /// <summary>
    /// 스크린 터치 차단
    /// </summary>
    /// <param name="isBlock"></param>
    /// <param name="color"></param>
    public void BlockScreenTouch(bool isBlock, Color? color = null)
    {
        SetColorFadeScreen(color.HasValue ? color.Value : Color.clear);
        SetActiveFadeScreen(isBlock);
    }

    /// <summary>
    /// 지정 시간 동안 화면 터치 차단
    /// </summary>
    /// <param name="duration"></param>
    /// <param name="color"></param>
    /// <param name="action"></param>
    public void BlockScreenTouchForDuration(float duration = 2f, Color? color = null, Action action = null)
    {
        SetColorFadeScreen(color.HasValue ? color.Value : Color.clear);
        SetActiveFadeScreen(true);
        this.WaitSeconds(duration, () =>
        {
            action?.Invoke();
            SetActiveFadeScreen(false);
        });
    }

    /// <summary>
    /// 현재 스크린 터치가 차단되어 있는지 확인
    /// </summary>
    /// <returns></returns>
    public bool IsBlockScreen() => screenFadeImage.gameObject.activeSelf;

    /// <summary>
    /// 뒤로 가기 키 입력 시 호출되는 메서드
    /// </summary>
    /// <returns></returns>
    public bool OnEscapeKeyPreesed() => MoveBack(true);

    /// <summary>
    /// 뒤로 가기
    /// 팝업 뷰가 열려있으면 팝업 뷰를 닫고 true 반환
    /// 진화 뷰가 열려있으면 진화 뷰를 닫고 true 반환
    /// </summary>
    /// <param name="isEscape"></param>
    /// <returns></returns>
    bool MoveBack(bool isEscape)
    {
        var popResult = PopupManager.Instance.Pop(isEscape);
        if (popResult) return true;

        if (IsEvolutionViewOpened())
        {
            CloseEvolutionView(true);
            return true;
        }

        return false;
    }

    /// <summary>
    /// FloatingUIItem 오브젝트 풀을 생성
    /// </summary>
    /// <param name="prefab"></param>
    /// <returns></returns>
    private ObjectPool<FloatingUIItem> CreateObjectPool(FloatingUIItem prefab)
    {
        return new ObjectPool<FloatingUIItem>(() =>
            {
                var obj = Instantiate(prefab);
                obj.SetActiveGameObject(false);
                obj.transform.SetParent(UIObjectPool);
                return obj;
            },
            (FloatingUIItem item) => item.SetActiveGameObject(true),
            (FloatingUIItem item) =>
            {
                item.SetActiveGameObject(false);
                item.transform.SetParent(UIObjectPool);
            },
            (FloatingUIItem item) => Destroy(item.gameObject),
            true);
    }

    /// <summary>
    /// 지정된 itemType 및 target에 대해 FloatingUIItem을 생성하고 관리하는 사전에 추가합니다.
    /// </summary>
    /// <param name="itemType"></param>
    /// <param name="target"></param>
    /// <param name="settings"></param>
    /// <returns></returns>
    public FloatingUIItem CreateFloatingUIItem(FloatingUIItemType itemType, Transform target,
        FloatingUIItemSettings settings = null)
    {
        if (IsEvolutionViewOpened()) return null;
        if (_isEvolving) return null;
        if (objectPools.TryGetValue(itemType, out var pool))
        {
            var obj = pool.Get();
            obj.transform.SetParent(target);
            obj.transform.localPosition = Vector3.zero;

            if (!activeFloatingUIItems.ContainsKey(target))
            {
                activeFloatingUIItems[target] = new List<FloatingUIItem>();
            }

            activeFloatingUIItems[target].Add(obj);

            settings?.ApplySettings(obj);

            return obj;
        }

        return null;
    }

    /// <summary>
    /// target과 연결된 특정 FloatingUIItem을 제거합니다.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="itemToRemove"></param>
    /// <returns></returns>
    public bool RemoveFloatingUIItem(Transform target, FloatingUIItem itemToRemove)
    {
        if (activeFloatingUIItems.TryGetValue(target, out var floatingUIItems))
        {
            if (floatingUIItems.Remove(itemToRemove))
            {
                objectPools[itemToRemove.ItemType].Release(itemToRemove);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// target과 연결된 모든 FloatingUIItem을 제거합니다.
    /// </summary>
    /// <param name="target"></param>
    public void RemoveAllFloatingUIItems(Transform target)
    {
        if (activeFloatingUIItems.TryGetValue(target, out var floatingUIItems))
        {
            foreach (var floatingUIItem in floatingUIItems)
            {
                objectPools[floatingUIItem.ItemType].Release(floatingUIItem);
            }

            floatingUIItems.Clear();
            activeFloatingUIItems.Remove(target);
        }
    }

    /// <summary>
    /// 사전의 모든 FloatingUIItem 인스턴스를 제거합니다.
    /// </summary>
    public void RemoveAllFloatingUIItems()
    {
        foreach (var floatingUIItems in activeFloatingUIItems.Values)
        {
            if (floatingUIItems == null) continue;

            foreach (var floatingUIItem in floatingUIItems)
            {
                objectPools[floatingUIItem.ItemType].Release(floatingUIItem);
            }
        }

        activeFloatingUIItems.Clear();
    }

    /// <summary>
    /// 재화 획득 효과 재생
    /// </summary>
    /// <param name="worldPos"></param>
    /// <param name="amount"></param>
    /// <param name="isInOverlayCanvasObject"></param>
    /// <param name="attractorTarget"></param>
    /// <param name="moneyType"></param>
    public void PlayMoneyEffect(Vector3 worldPos, int amount, bool isInOverlayCanvasObject,
        Transform attractorTarget = null, MoneyType moneyType = MoneyType.Coin)
    {
        Vector2 screenPoint =
            RectTransformUtility.WorldToScreenPoint(isInOverlayCanvasObject ? null : MainCamera, worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(this.ParticleViewInstance.transform.RectT(),
            screenPoint, null, out Vector2 localPoint);

        var moneyParticle = ParticlePool.Instance.GetParticle("MoneyTextEffect").GetComponent<MoneyParticleWithText>();
        moneyParticle.transform.SetParent(this.ParticleViewInstance.transform);
        moneyParticle.transform.SetLocalPositionAndRotation(localPoint, Quaternion.identity);
        moneyParticle.transform.localScale = Vector3.one;
        if (moneyType == MoneyType.Coin)
        {
            moneyParticle.SetIcon(UITextureLoader.Instance.GetSprite("icon_coin"));
            moneyParticle.particleImage.attractorTarget = (attractorTarget == null)
                ? TopViewInstance.Coin.GetImage("icon").transform
                : attractorTarget;
        }
        else
        {
            moneyParticle.SetIcon(UITextureLoader.Instance.GetSprite("icon_gem"));
            moneyParticle.particleImage.attractorTarget = (attractorTarget == null)
                ? TopViewInstance.Gem.GetImage("icon").transform
                : attractorTarget;
        }

        moneyParticle.SetMoneyAmount(amount);
        moneyParticle.imageTextGroup.localScale = Vector3.zero;

        this.WaitOneFrame(() =>
        {
            var sequence = DOTween.Sequence();
            sequence.Append(moneyParticle.imageTextGroup.DOScale(Vector3.one * 1.5f, 0.25f));
            sequence.AppendInterval(1f);
            sequence.Append(moneyParticle.imageTextGroup.DOScale(Vector3.zero, 0.25f));

            moneyParticle.particleImage.Play();

            moneyParticle.WaitUntil(() => moneyParticle.particleImage.isPlaying == false,
                () => { ParticlePool.Instance.ReturnParticle("MoneyTextEffect", moneyParticle.gameObject); });
        });
    }

    /// <summary>
    /// 캔버스 내에서 돈 획득 효과 재생
    /// </summary>
    /// <param name="localPositionInCanvas"></param>
    /// <param name="amount"></param>
    /// <param name="attractorTarget"></param>
    /// <param name="moneyType"></param>
    public void PlayMoneyEffectInCanvas(Vector2 localPositionInCanvas, int amount, Transform attractorTarget = null,
        MoneyType moneyType = MoneyType.Coin)
    {
        // 이 위치에서 이미 표시된 파티클 수를 기반으로 위치를 조정합니다.
        if (positionCounters.ContainsKey(localPositionInCanvas))
        {
            positionCounters[localPositionInCanvas]++;
        }
        else
        {
            positionCounters.TryAdd(localPositionInCanvas, 0);
        }

        // 이 값을 조정하여 파티클 간의 간격을 변경할 수 있습니다.
        int offset = positionCounters[localPositionInCanvas] * 150;
        localPositionInCanvas += new Vector2(0, offset);

        // 파티클 가져오기 및 설정
        var moneyParticle = ParticlePool.Instance.GetParticle("MoneyTextEffect").GetComponent<MoneyParticleWithText>();
        moneyParticle.transform.SetParent(this.ParticleViewInstance.transform);
        moneyParticle.transform.localPosition = localPositionInCanvas;
        moneyParticle.transform.localScale = Vector3.one;

        // 돈 타입(코인 또는 젬)에 따라 아이콘 설정
        if (moneyType == MoneyType.Coin)
        {
            moneyParticle.SetIcon(UITextureLoader.Instance.GetSprite("icon_coin"));
            moneyParticle.particleImage.attractorTarget = (attractorTarget == null)
                ? TopViewInstance.Coin.GetImage("icon").transform
                : attractorTarget;
        }
        else
        {
            moneyParticle.SetIcon(UITextureLoader.Instance.GetSprite("icon_gem"));
            moneyParticle.particleImage.attractorTarget = (attractorTarget == null)
                ? TopViewInstance.Gem.GetImage("icon").transform
                : attractorTarget;
        }

        // 금액 설정
        moneyParticle.SetMoneyAmount(amount);
        moneyParticle.imageTextGroup.localScale = Vector3.zero;

        // 파티클 애니메이션 시작
        this.WaitOneFrame(() =>
        {
            var sequence = DOTween.Sequence();
            sequence.Append(moneyParticle.imageTextGroup.DOScale(Vector3.one * 1.5f, 0.25f));
            sequence.AppendInterval(1f);
            sequence.Append(moneyParticle.imageTextGroup.DOScale(Vector3.zero, 0.25f));

            moneyParticle.particleImage.Play();

            // 파티클 애니메이션이 끝나면 카운터를 감소시키고 필요한 경우 딕셔너리에서 위치 제거
            moneyParticle.WaitUntil(() => moneyParticle.particleImage.isPlaying == false,
                () =>
                {
                    if (positionCounters.ContainsKey(localPositionInCanvas))
                    {
                        positionCounters[localPositionInCanvas]--;
                        if (positionCounters[localPositionInCanvas] == 0)
                        {
                            positionCounters.Remove(localPositionInCanvas);
                        }
                    }

                    ParticlePool.Instance.ReturnParticle("MoneyTextEffect", moneyParticle.gameObject);
                });
        });
    }

    /// <summary>
    /// 똥을 치웠을 때 효과 재생
    /// </summary>
    /// <param name="worldPos"></param>
    /// <param name="isInOverlayCanvasObject"></param>
    public void PlayPoopClearEffect(Vector3 worldPos, bool isInOverlayCanvasObject)
    {
        Vector2 screenPoint =
            RectTransformUtility.WorldToScreenPoint(isInOverlayCanvasObject ? null : MainCamera, worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(this.ParticleViewInstance.transform.RectT(),
            screenPoint, null, out Vector2 localPoint);

        var poopParticle = ParticlePool.Instance.GetParticle("poopClear").GetComponent<ParticleImage>();
        poopParticle.transform.SetParent(this.UIObjectPool);
        poopParticle.transform.SetLocalPositionAndRotation(localPoint, Quaternion.identity);
        poopParticle.transform.localScale = Vector3.one;
        poopParticle.Play();

        playingPoopParticleList.Add(poopParticle);

        poopParticle.WaitUntil(() => poopParticle.isPlaying == false,
            () => { ParticlePool.Instance.ReturnParticle("poopClear", poopParticle.gameObject); });
    }

    /// <summary>
    /// 진화 뷰가 열려 있는지 확인
    /// </summary>
    /// <returns></returns>
    public bool IsEvolutionViewOpened()
    {
        return _isEvolutionViewOpened.Value?.IsOpened ?? false;
    }

    /// <summary>
    /// 진화 뷰 상태 변경 이벤트 구독
    /// </summary>
    /// <param name="onEvolutionViewOpened"></param>
    /// <returns></returns>
    public IDisposable SubscribeEvolutionViewOpened(Action<EvolutionViewState> onEvolutionViewOpened)
    {
        return _isEvolutionViewOpened.Subscribe(onEvolutionViewOpened);
    }

    /// <summary>
    /// 메인 카메라 변경 이벤트 구독
    /// </summary>
    /// <param name="onMainCameraChange"></param>
    /// <returns></returns>
    public IDisposable SubscribeMainCameraChange(Action<Camera> onMainCameraChange)
    {
        return _mainCameraProperty.Subscribe(onMainCameraChange);
    }

    /// <summary>
    /// 진화 뷰 상태
    /// </summary>
    public class EvolutionViewState
    {
        /// <summary>
        /// 진화 뷰가 열려 있는지 여부
        /// </summary>
        public bool IsOpened;

        /// <summary>
        /// 원래 방으로 이동해야 하는지 여부
        /// </summary>
        public bool IsRequireMoveToOriginalRoom;

        /// <summary>
        /// 진화 뷰 상태 생성자
        /// </summary>
        /// <param name="isOpened"></param>
        public EvolutionViewState(bool isOpened)
        {
            IsOpened = isOpened;
        }

        /// <summary>
        /// 진화 뷰 상태 생성자
        /// </summary>
        /// <param name="isOpened"></param>
        /// <param name="isRequireMoveToOriginalRoom"></param>
        public EvolutionViewState(bool isOpened, bool isRequireMoveToOriginalRoom)
        {
            IsOpened = isOpened;
            IsRequireMoveToOriginalRoom = isRequireMoveToOriginalRoom;
        }
    }

    /// <summary>
    /// 미니 게임 UI 설정
    /// </summary>
    public class MiniGameUISetting
    {
        /// <summary>
        /// 쿨다운 시간
        /// </summary>
        public TimeSpan Cooldown;
        /// <summary>
        /// 플레이 가능 여부
        /// </summary>
        public bool IsPlayable;
        /// <summary>
        /// 썸네일
        /// </summary>
        public Sprite Thumbnail;

        /// <summary>
        /// 미니 게임 UI 설정 생성자
        /// </summary>
        /// <param name="thumbnail"></param>
        /// <param name="isPlayable"></param>
        /// <param name="cooldown"></param>
        public MiniGameUISetting(Sprite thumbnail, bool isPlayable, TimeSpan cooldown)
        {
            Thumbnail = thumbnail;
            IsPlayable = isPlayable;
            Cooldown = cooldown;
        }
    }

    /// <summary>
    /// 케어 게이지 레벨
    /// </summary>
    private enum CareGaugeLevel
    {
        Normal = 0, // 정상
        Low = 1 // 낮음
    }
}
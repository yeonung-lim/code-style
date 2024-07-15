using System;
using System.Collections.Generic;
using Advertisement.Keys;
using Advertisement.Util;
using Cysharp.Threading.Tasks;
using GoogleMobileAds.Api;
using Load;
using Localization;
using Sirenix.OdinInspector;
using UniRx;
using UnityEngine;
using Logger = CustomLogger.Logger;

namespace Advertisement
{
    /// <summary>
    /// 배너 속성
    /// </summary>
    public class BannerProperty
    {
        /// <summary>
        /// 배너 위치
        /// </summary>
        public readonly AdPosition BannerPosition;
        /// <summary>
        /// 배너 크기
        /// </summary>
        public readonly Vector2 DeltaSize;
        /// <summary>
        /// 배너가 열려있는지 여부
        /// </summary>
        public readonly bool IsOpened;

        /// <summary>
        /// 배너 속성 생성자
        /// </summary>
        /// <param name="deltaSize"></param>
        /// <param name="bannerPosition"></param>
        /// <param name="isOpened"></param>
        public BannerProperty(Vector2 deltaSize, AdPosition bannerPosition, bool isOpened)
        {
            DeltaSize = deltaSize;
            BannerPosition = bannerPosition;
            IsOpened = isOpened;
        }
    }

    /// <summary>
    /// 광고 관리자
    /// </summary>
    public class AdManager : Singleton<AdManager>, IAsyncInit
    {
        /// <summary>
        /// 현재 배너 속성 (리액티브 프로퍼티)
        /// </summary>
        private readonly ReactiveProperty<BannerProperty> _bannerProperty =
            new(new BannerProperty(Vector2.zero, AdPosition.Top, false));

        /// <summary>
        ///     광고 컨트롤러
        /// </summary>
        private GoogleAdMobController _adMobController;

        /// <summary>
        /// 배너 광고가 열려있는지 여부
        /// </summary>
        private bool _isBannerAdOpened;
        /// <summary>
        /// 배너 광고 요청중인지 여부
        /// </summary>
        private bool _isBannerAdRequested;
        /// <summary>
        /// 전체 화면 광고가 닫혔는지 여부
        /// </summary>
        private bool _isFullContentAdClosed = true;
        /// <summary>
        /// 초기화 여부
        /// </summary>
        private bool _isInitialized;
        /// <summary>
        /// 보상 받았는지 여부
        /// </summary>
        private bool _isRewarded;
        /// <summary>
        /// 마지막 배너 위치
        /// </summary>
        private AdPosition _lastBannerPosition = AdPosition.Top;
        /// <summary>
        /// 구독
        /// </summary>
        private IEnumerable<IDisposable> _subscriptions;

        /// <summary>
        /// 배너 자동 제어 활성화 여부
        /// </summary>
        public bool AutoBannerControlEnabled { get; set; } = true;

        private void Update()
        {
            if (_isInitialized == false) return;

            AutoBannerControl();
        }

        public void Reset()
        {
        }

        /// <summary>
        /// 비동기 작업 가져오기
        /// </summary>
        /// <returns></returns>
        public CustomizableAsyncOperation GetAsyncOperation()
        {
            return CustomizableAsyncOperation.Create(() => _isInitialized, () => _isInitialized ? 1f : 0f);
        }

        /// <summary>
        ///     프로세스 시작
        /// </summary>
        public void StartProcess()
        {
            Init();
        }

        /// <summary>
        ///     배너 속성 구독
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public IDisposable SubscribeBannerProperty(Action<BannerProperty> action)
        {
            return _bannerProperty.Subscribe(action);
        }

        /// <summary>
        ///    배너 속성 가져오기
        /// </summary>
        /// <returns></returns>
        public BannerProperty GetBannerProperty()
        {
            return _bannerProperty?.Value;
        }

        /// <summary>
        ///    배너 위치 변경
        /// </summary>
        /// <param name="adPosition"></param>
        public void ChangeBannerPosition(AdPosition adPosition)
        {
            _lastBannerPosition = adPosition;

            if (_adMobController.ExistBannerView())
                _adMobController.ChangeBannerPosition(adPosition);

            if (_bannerProperty != null)
                _bannerProperty.Value = new BannerProperty(_bannerProperty.Value.DeltaSize, adPosition,
                    _bannerProperty.Value.IsOpened);
        }

        /// <summary>
        ///     광고 보여주기
        /// </summary>
        /// <param name="adKey">광고 키</param>
        /// <returns>광고 결과</returns>
        public async UniTask<AdResult> ShowAd(IAdKey adKey)
        {
            // 스킵 가능한 광고
            if (adKey.SkippAble) return AdUtil.CreateSkippedResult(adKey.AdType);

            if (adKey.AdType == AdType.Banner) return await ShowBannerAD(adKey);

            if (_isFullContentAdClosed == false)
                return new EmptyAdResult(failReason: "FullContentAd is already opened");

            if (!_adMobController.OpenAd(adKey.AdType, out var failReason))
            {
                if (failReason.TryLocalize(out var localizedFailReason))
                    PopupManager.Instance.ShowToast(localizedFailReason, false);

                return AdUtil.CreateFailResult(adKey.AdType, failReason);
            }

            _isRewarded = false;
            _isFullContentAdClosed = false;

            bool IsAdClosed()
            {
                return _isFullContentAdClosed;
            }

            await UniTask.WaitUntil(IsAdClosed, cancellationToken: this.GetCancellationTokenOnDestroy());

            return AdUtil.CreateSuccessResult(adKey.AdType, _isRewarded);
        }

        /// <summary>
        /// 배너 광고 보여주기
        /// </summary>
        /// <param name="adKey"></param>
        /// <returns></returns>
        private async UniTask<AdResult> ShowBannerAD(IAdKey adKey)
        {
            if (_isBannerAdOpened || _isBannerAdRequested)
            {
                Logger.LogWarning("Banner AD is already opened or requested");
                return AdUtil.CreateFailResult(AdType.Banner, AdResultFailReason.AlreadyPlaying);
            }

            _isBannerAdOpened = false;

            if (_adMobController.ExistBannerView())
            {
                _adMobController.ShowBannerView();
                return AdUtil.CreateSuccessResult(AdType.Banner);
            }

            _isBannerAdRequested = true;
            _adMobController.RequestBannerAd(_lastBannerPosition);

            await UniTask.WaitUntil(() => _isBannerAdOpened);

            _isBannerAdRequested = false;
            return AdUtil.CreateSuccessResult(AdType.Banner);
        }

        /// <summary>
        /// 배너 광고 닫기
        /// </summary>
        /// <returns></returns>
        public bool CloseBannerAD()
        {
            if (!_adMobController.ExistBannerView())
            {
                Logger.LogWarning("Banner AD is already closed");
                return false;
            }

            _adMobController.HideBannerView();
            return true;
        }

        /// <summary>
        /// 배너 자동 제어
        /// </summary>
        private void AutoBannerControl()
        {
            if (AutoBannerControlEnabled == false) return;

            if (BannerVisibleCondition() && !_isBannerAdOpened)
            {
                if (!_isBannerAdRequested)
                    ShowAd(AdKeys.Banner).Forget();
            }
            else if (!BannerVisibleCondition() && _isBannerAdOpened)
            {
                _adMobController.HideBannerView();
            }
        }

        /// <summary>
        /// 지금 배너가 보여져야 하는 조건에 충족하는지 여부
        /// </summary>
        /// <returns></returns>
        private bool BannerVisibleCondition()
        {
            return AdUtil.HasRemoveAds1Item() == false &&
                   (SceneController.Instance.CurrentSceneName == SceneName.Ingame ||
                    SceneController.Instance.CurrentSceneName == SceneName.CollectiblesLand) &&
                   UIManager.Instance.DecorateViewInstance._CanvasGroup.alpha <= 0 &&
                   CharacterManager.Instance.IsEvolution == false &&
                   SceneController.Instance.IsLoading == false &&
                   (PopupManager.Instance.PopupStackCount == 0 || PopupManager.Instance.IsPlayingMiniGame);
        }

        /// <summary>
        /// 초기화
        /// </summary>
        private void Init()
        {
            if (_adMobController != null) return;

            _adMobController ??= gameObject.AddOrGetComponent<GoogleAdMobController>();
            _adMobController.onInitializedEvent.AddListener(OnInitialized);
            _adMobController.onBannerAdOpeningEvent.AddListener(OnBannerAdOpening);
            _adMobController.onBannerAdClosedEvent.AddListener(OnBannerAdClosed);
            _adMobController.onUserEarnedRewardEvent.AddListener(OnUserEarnedReward);
            _adMobController.onInterstitialAdClosedEvent.AddListener(InterstitialOnAdClosedEvent);
            _adMobController.onRewardAdClosedEvent.AddListener(RewardedVideoOnAdClosedEvent);
            _adMobController.onBannerAdShowEvent.AddListener(OnBannerShow);
            _adMobController.onBannerAdHideEvent.AddListener(OnBannerHide);

            _adMobController.Initialize();

            SceneController.OnLoadingStateChanged.Add(isChanged =>
            {
                if (isChanged)
                    CloseBannerAD();
            });

            _isInitialized = true;
        }

        /// <summary>
        /// 초기화 이벤트
        /// </summary>
        private void OnInitialized()
        {
            _adMobController.RequestAndLoadInterstitialAd();
            _adMobController.RequestAndLoadRewardedAd();
        }

        /// <summary>
        ///    보상 광고가 닫혔을 때
        /// </summary>
        private void RewardedVideoOnAdClosedEvent()
        {
            _isFullContentAdClosed = true;
        }

        /// <summary>
        /// 전면 광고가 닫혔을 때
        /// </summary>
        private void InterstitialOnAdClosedEvent()
        {
            _isFullContentAdClosed = true;
        }

        /// <summary>
        /// 보상 획득 시
        /// </summary>
        private void OnUserEarnedReward()
        {
            _isRewarded = true;
        }

        /// <summary>
        /// 배너 광고가 닫혔을 때
        /// </summary>
        private void OnBannerAdClosed()
        {
            _isBannerAdOpened = false;
            _bannerProperty.Value = new BannerProperty(Vector2.zero, _lastBannerPosition, false);
        }

        /// <summary>
        /// 배너가 숨겨졌을 때
        /// </summary>
        private void OnBannerHide()
        {
            _isBannerAdOpened = false;
            _bannerProperty.Value = new BannerProperty(Vector2.zero, _lastBannerPosition, false);
        }

        /// <summary>
        /// 배너가 보여졌을 때
        /// </summary>
        /// <param name="deltaSize"></param>
        private void OnBannerShow(Vector2 deltaSize)
        {
            _isBannerAdOpened = true;
            _adMobController.ChangeBannerPosition(_lastBannerPosition);
            _bannerProperty.Value = new BannerProperty(deltaSize, _lastBannerPosition, true);
        }

        /// <summary>
        /// 배너가 로드되었을 때
        /// </summary>
        /// <param name="deltaSize"></param>
        private void OnBannerAdOpening(Vector2 deltaSize)
        {
            _isBannerAdOpened = true;
            _adMobController.ChangeBannerPosition(_lastBannerPosition);
            _bannerProperty.Value = new BannerProperty(deltaSize, _lastBannerPosition, true);
        }

        #region TEST

        /// <summary>
        /// 배너를 상단으로 이동
        /// </summary>
        [Button]
        [ButtonGroup("Banner")]
        private void BannerToTop()
        {
            ChangeBannerPosition(AdPosition.Top);
        }

        /// <summary>
        /// 배너를 하단으로 이동
        /// </summary>
        [Button]
        [ButtonGroup("Banner")]
        private void BannerToBottom()
        {
            ChangeBannerPosition(AdPosition.Bottom);
        }

        #endregion
    }
}
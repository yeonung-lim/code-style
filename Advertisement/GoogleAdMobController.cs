using System;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Mediation.AppLovin.Api;
using UnityEngine;
using UnityEngine.Events;
using Logger = CustomLogger.Logger;

namespace Advertisement
{
    /// <summary>
    /// 구글 애드몹 컨트롤러
    /// </summary>
    public class GoogleAdMobController : Singleton<GoogleAdMobController>
    {
        /// <summary>
        /// 테스트 모드 여부
        /// </summary>
        private static bool _testModeStatic;
        /// <summary>
        /// 초기화 여부
        /// </summary>
        private static bool _isInitialized;

        /// <summary>
        /// 테스트 모드 (인스펙터)
        /// </summary>
        [Header("Initialization")]
        [SerializeField]
        private bool testMode;

        /// <summary>
        /// 초기화 이벤트
        /// </summary>
        public UnityEvent onInitializedEvent = new();
        /// <summary>
        /// 광고 로드 이벤트
        /// </summary>
        public UnityEvent onAdLoadedEvent = new();
        /// <summary>
        /// 광고 로드 실패 이벤트
        /// </summary>
        public UnityEvent onAdFailedToLoadEvent = new();
        /// <summary>
        /// 광고 로드 성공 이벤트
        /// </summary>
        public UnityEvent onAdOpeningEvent = new();
        /// <summary>
        /// 광고 보여주기 실패 이벤트
        /// </summary>
        public UnityEvent onAdFailedToShowEvent = new();
        /// <summary>
        /// 사용자 보상 이벤트
        /// </summary>
        public UnityEvent onUserEarnedRewardEvent = new();
        /// <summary>
        /// 배너 광고 오픈 이벤트
        /// </summary>
        public UnityEvent<Vector2> onBannerAdOpeningEvent = new();
        /// <summary>
        /// 배너 광고 닫힘 이벤트
        /// </summary>
        public UnityEvent onBannerAdClosedEvent = new();
        /// <summary>
        /// 배너 광고 보여주기 이벤트
        /// </summary>
        public UnityEvent<Vector2> onBannerAdShowEvent = new();
        /// <summary>
        /// 배너 광고 숨기기 이벤트
        /// </summary>
        public UnityEvent onBannerAdHideEvent = new();
        /// <summary>
        /// 보상형 광고 닫힘 이벤트
        /// </summary>
        public UnityEvent onRewardAdClosedEvent = new();
        /// <summary>
        /// 전면 광고 닫힘 이벤트
        /// </summary>
        public UnityEvent onInterstitialAdClosedEvent = new();
        private AppOpenAd _appOpenAd;
        private DateTime _appOpenExpireTime;
        private BannerView _bannerView;
        private InterstitialAd _interstitialAd;

        private RewardedAd _rewardedAd;
        private RewardedInterstitialAd _rewardedInterstitialAd;

        /// <summary>
        /// 테스트 모드
        /// </summary>
        public static bool TestMode
        {
            get => _testModeStatic;
            set
            {
                _testModeStatic = value;
                if (_isInitialized == false) return;

                if (Instance.ExistBannerView())
                    Instance.RequestBannerAd();

                Instance.RequestAndLoadInterstitialAd();
                Instance.RequestAndLoadRewardedAd();
            }
        }


        #region AD INSPECTOR

        /// <summary>
        /// 광고 인스펙터 열기
        /// </summary>
        public void OpenAdInspector()
        {
            PrintStatus("Opening Ad inspector.");

            MobileAds.OpenAdInspector(error =>
            {
                if (error != null)
                    PrintStatus("Ad inspector failed to open with error: " + error);
                else
                    PrintStatus("Ad inspector opened successfully.");
            });
        }

        #endregion

        #region Utility

        /// <summary>
        /// 메시지를 기록하고 메인 스레드에 상태 텍스트를 업데이트합니다.
        /// </summary>
        /// <param name="message"></param>
        private void PrintStatus(string message)
        {
            Logger.Log($"[Admob] {message}");
        }

        #endregion

        #region Ironsource

        /// <summary>
        /// IronSource 초기화
        /// </summary>
        private void InitializeIronSource()
        {
            // IronSource.SetConsent(true);
            // IronSource.SetMetaData("do_not_sell", "true");
        }

        #endregion

        #region UnityAds

        /// <summary>
        /// UnityAds 초기화
        /// </summary>
        private void InitializeUnityAds()
        {
            // UnityAds.SetConsentMetaData("gdpr.consent", true);
        }

        #endregion

        #region AppLovin

        /// <summary>
        /// AppLovin 초기화
        /// </summary>
        private void InitializeAppLovin()
        {
            // AppLovin.SetHasUserConsent(true);
            // AppLovin.SetIsAgeRestrictedUser(true);
            // AppLovin.SetDoNotSell(true);
            AppLovin.Initialize();
        }

        #endregion


        #region HELPER METHODS

        /// <summary>
        /// 광고 요청 생성
        /// </summary>
        /// <returns></returns>
        private static AdRequest CreateAdRequest()
        {
            return new AdRequest
            {
                Keywords = new HashSet<string> { "unity-admob-sample" }
            };
        }

        #endregion

        #region UNITY MONOBEHAVIOR METHODS

        /// <summary>
        /// 초기화
        /// </summary>
        public void Initialize()
        {
            MobileAds.SetiOSAppPauseOnBackground(true);

            // When true all events raised by GoogleMobileAds will be raised
            // on the Unity main thread. The default value is false.
            MobileAds.RaiseAdEventsOnUnityMainThread = true;

            TestMode = testMode;

            InitializeIronSource();
            InitializeUnityAds();
            InitializeAppLovin();

            // Configure TagForChildDirectedTreatment and test device IDs.
            MobileAds.SetRequestConfiguration(new RequestConfiguration
            {
                TagForChildDirectedTreatment = TagForChildDirectedTreatment.Unspecified
            });

            // Initialize the Google Mobile Ads SDK.
            MobileAds.Initialize(HandleInitCompleteAction);
        }

        /// <summary>
        /// 초기화 성공 시 호출
        /// </summary>
        /// <param name="initStatus"></param>
        private void HandleInitCompleteAction(InitializationStatus initStatus)
        {
            Logger.Log("Initialization complete.");

            foreach (var status in initStatus.getAdapterStatusMap())
                Logger.Log(
                    $"status: {status.Key} state: {status.Value.InitializationState} description: {status.Value.Description}");

            _isInitialized = true;

            onInitializedEvent.Invoke();
        }

        #endregion

        #region BANNER ADS

        /// <summary>
        /// 배너 광고 위치 변경
        /// </summary>
        /// <param name="adPosition"></param>
        public void ChangeBannerPosition(AdPosition adPosition)
        {
            _bannerView?.SetPosition(adPosition);
        }

        /// <summary>
        /// 배너 광고 요청
        /// </summary>
        /// <param name="position"></param>
        public void RequestBannerAd(AdPosition position = AdPosition.Top)
        {
            PrintStatus("Requesting Banner ad.");

            var adUnitId = "";
            // These ad units are configured to always serve test ads.
#if UNITY_EDITOR
            adUnitId = "unused";
#elif UNITY_ANDROID
            adUnitId = TestMode ? "ca-app-pub-3940256099942544/6300978111" : "ca-app-pub-6304218294936279/2998625161";
#elif UNITY_IOS
             adUnitId = TestMode ? "ca-app-pub-3940256099942544/6300978111" : "ca-app-pub-6304218294936279/6197679089";
#endif

            // Clean up banner before reusing
            if (_bannerView != null) _bannerView.Destroy();

            var adaptiveSize =
                AdSize.GetPortraitAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);

            _bannerView = new BannerView(adUnitId, adaptiveSize, position);

            // Add Event Handlers
            _bannerView.OnBannerAdLoaded += () =>
            {
                PrintStatus("Banner view loaded an ad with response : "
                            + _bannerView.GetResponseInfo());

                var deltaSize = new Vector2(_bannerView.GetWidthInPixels(), _bannerView.GetHeightInPixels());
                PrintStatus($"Ad Height: {deltaSize.y}, width: {deltaSize.x}");
                onAdLoadedEvent.Invoke();

                onBannerAdOpeningEvent.Invoke(deltaSize);
            };
            _bannerView.OnBannerAdLoadFailed += error =>
            {
                PrintStatus("Banner ad failed to load with error: " + error.GetMessage());
                onAdFailedToLoadEvent.Invoke();
            };
            _bannerView.OnAdImpressionRecorded += () => { PrintStatus("Banner ad recorded an impression."); };

            _bannerView.OnAdClicked += () => { PrintStatus("Banner ad recorded a click."); };
            _bannerView.OnAdPaid += adValue =>
            {
                // EventSender.AdRevenue(adValue, bannerView.GetResponseInfo().GetMediationAdapterClassName());
            };

            // Load a banner ad
            _bannerView.LoadAd(CreateAdRequest());
        }

        /// <summary>
        /// 배너 광고 보여주기
        /// </summary>
        [ContextMenu("ShowBannerView")]
        public void ShowBannerView()
        {
            if (_bannerView == null) return;

            _bannerView.Show();
            onBannerAdShowEvent?.Invoke(
                new Vector2(_bannerView.GetWidthInPixels(), _bannerView.GetHeightInPixels()));
        }

        /// <summary>
        /// 배너 광고 숨기기
        /// </summary>
        [ContextMenu("HideBannerView")]
        public void HideBannerView()
        {
            if (_bannerView == null) return;

            _bannerView.Hide();
            onBannerAdHideEvent?.Invoke();
        }

        /// <summary>
        /// 배너 광고 존재 여부
        /// </summary>
        /// <returns></returns>
        public bool ExistBannerView()
        {
            return _bannerView != null;
        }

        /// <summary>
        /// 배너 광고 파괴
        /// </summary>
        public void DestroyBannerAd()
        {
            if (_bannerView != null)
            {
                _bannerView.Destroy();
                _bannerView = null;
                onBannerAdClosedEvent.Invoke();
            }
        }

        #endregion

        #region INTERSTITIAL ADS

        /// <summary>
        /// 전면 광고 요청 및 로드
        /// </summary>
        public void RequestAndLoadInterstitialAd()
        {
            PrintStatus("Requesting Interstitial ad.");
            var adUnitId = "";
#if UNITY_EDITOR
            adUnitId = "unused";
#elif UNITY_ANDROID
            adUnitId = TestMode ? "ca-app-pub-3940256099942544/1033173712" : "ca-app-pub-6304218294936279/2106794443";
#elif UNITY_IOS
            adUnitId = TestMode ? "ca-app-pub-3940256099942544/1033173712" : "ca-app-pub-6304218294936279/1835724253";
#endif

            // Clean up interstitial before using it
            if (_interstitialAd != null) _interstitialAd.Destroy();

            // Load an interstitial ad
            InterstitialAd.Load(adUnitId, CreateAdRequest(),
                (ad, loadError) =>
                {
                    if (loadError != null)
                    {
                        PrintStatus("Interstitial ad failed to load with error: " +
                                    loadError.GetMessage());
                        return;
                    }

                    if (ad == null)
                    {
                        PrintStatus("Interstitial ad failed to load.");
                        return;
                    }

                    PrintStatus("Interstitial ad loaded.");
                    _interstitialAd = ad;

                    ad.OnAdFullScreenContentOpened += () =>
                    {
                        PrintStatus("Interstitial ad opening.");
                        onAdOpeningEvent.Invoke();
                    };
                    ad.OnAdFullScreenContentClosed += () =>
                    {
                        PrintStatus("Interstitial ad closed.");
                        onInterstitialAdClosedEvent.Invoke();
                    };
                    ad.OnAdImpressionRecorded += () => { PrintStatus("Interstitial ad recorded an impression."); };
                    ad.OnAdClicked += () => { PrintStatus("Interstitial ad recorded a click."); };
                    ad.OnAdFullScreenContentFailed += error =>
                    {
                        PrintStatus("Interstitial ad failed to show with error: " +
                                    error.GetMessage());
                    };
                    ad.OnAdPaid += adValue =>
                    {
                        // EventSender.AdRevenue(adValue, interstitialAd.GetResponseInfo().GetMediationAdapterClassName());
                    };

                    RegisterReloadHandler(ad);
                });
        }

        /// <summary>
        /// 전면 광고 재 로드 핸들러 등록
        /// </summary>
        /// <param name="ad"></param>
        private void RegisterReloadHandler(InterstitialAd ad)
        {
            // Raised when the ad closed full screen content.
            ad.OnAdFullScreenContentClosed += () =>
            {
                // Reload the ad so that we can show another as soon as possible.
                RequestAndLoadInterstitialAd();
            };
            // Raised when the ad failed to open full screen content.
            ad.OnAdFullScreenContentFailed += error =>
            {
                // Reload the ad so that we can show another as soon as possible.
                RequestAndLoadInterstitialAd();
            };
        }

        /// <summary>
        /// 전면 광고 준비 여부
        /// </summary>
        /// <returns></returns>
        public bool IsInterstitialAdReady()
        {
            return _interstitialAd != null && _interstitialAd.CanShowAd();
        }

        /// <summary>
        /// 전면 광고 보여주기
        /// </summary>
        /// <returns></returns>
        public bool ShowInterstitialAd()
        {
            if (IsInterstitialAdReady())
            {
                _interstitialAd.Show();
                return true;
            }

            PrintStatus("Interstitial ad is not ready yet.");
            return false;
        }

        /// <summary>
        /// 전면 광고 파괴
        /// </summary>
        public void DestroyInterstitialAd()
        {
            if (_interstitialAd != null) _interstitialAd.Destroy();
        }

        #endregion

        #region REWARDED ADS

        /// <summary>
        /// 보상형 광고 요청 및 로드
        /// </summary>
        public void RequestAndLoadRewardedAd()
        {
            PrintStatus("Requesting Rewarded ad.");

            var adUnitId = "";
#if UNITY_EDITOR
            adUnitId = "unused";
#elif UNITY_ANDROID
            adUnitId = TestMode ? "ca-app-pub-3940256099942544/5224354917" : "ca-app-pub-6304218294936279/5253253412";
#elif UNITY_IOS
            adUnitId = TestMode ? "ca-app-pub-3940256099942544/5224354917" : "ca-app-pub-6304218294936279/9327201460";
#endif

            // create new rewarded ad instance
            RewardedAd.Load(adUnitId, CreateAdRequest(),
                (ad, loadError) =>
                {
                    if (loadError != null)
                    {
                        PrintStatus("Rewarded ad failed to load with error: " +
                                    loadError.GetMessage());
                        return;
                    }

                    if (ad == null)
                    {
                        PrintStatus("Rewarded ad failed to load.");
                        return;
                    }

                    PrintStatus("Rewarded ad loaded.");
                    _rewardedAd = ad;

                    ad.OnAdFullScreenContentOpened += () =>
                    {
                        PrintStatus("Rewarded ad opening.");
                        onAdOpeningEvent.Invoke();
                    };
                    ad.OnAdFullScreenContentClosed += () =>
                    {
                        PrintStatus("Rewarded ad closed.");
                        onRewardAdClosedEvent.Invoke();
                    };

                    ad.OnAdImpressionRecorded += () => { PrintStatus("Rewarded ad recorded an impression."); };
                    ad.OnAdClicked += () => { PrintStatus("Rewarded ad recorded a click."); };
                    ad.OnAdFullScreenContentFailed += error =>
                    {
                        PrintStatus("Rewarded ad failed to show with error: " +
                                    error.GetMessage());
                    };
                    ad.OnAdPaid += adValue =>
                    {
                        // EventSender.AdRevenue(adValue, rewardedAd.GetResponseInfo().GetMediationAdapterClassName());
                    };

                    RegisterReloadHandler(ad);
                });
        }

        /// <summary>
        /// 보상형 광고 재 로드 핸들러 등록
        /// </summary>
        /// <param name="ad"></param>
        private void RegisterReloadHandler(RewardedAd ad)
        {
            // Raised when the ad closed full screen content.
            ad.OnAdFullScreenContentClosed += () =>
            {
                // Reload the ad so that we can show another as soon as possible.
                RequestAndLoadRewardedAd();
            };
            // Raised when the ad failed to open full screen content.
            ad.OnAdFullScreenContentFailed += error =>
            {
                // Reload the ad so that we can show another as soon as possible.
                RequestAndLoadRewardedAd();
            };
        }

        /// <summary>
        /// 보상형 광고 준비 여부
        /// </summary>
        /// <returns></returns>
        public bool IsRewardedAdReady()
        {
            return _rewardedAd != null;
        }

        /// <summary>
        /// 보상형 광고 보여주기
        /// </summary>
        /// <returns></returns>
        public bool ShowRewardedAd()
        {
            if (IsRewardedAdReady())
            {
                _rewardedAd.Show(reward =>
                {
                    PrintStatus("Rewarded ad granted a reward: " + reward.Amount);
                    onUserEarnedRewardEvent.Invoke();
                });
                return true;
            }

            PrintStatus("Rewarded ad is not ready yet.");
            return false;
        }

        /// <summary>
        /// 보상형 전면 광고 로드 및 요청
        /// </summary>
        public void RequestAndLoadRewardedInterstitialAd()
        {
            PrintStatus("Requesting Rewarded Interstitial ad.");

            // These ad units are configured to always serve test ads.
#if UNITY_EDITOR
            var adUnitId = "unused";
#elif UNITY_ANDROID
            string adUnitId = "ca-app-pub-3940256099942544/5354046379";
#elif UNITY_IPHONE
            string adUnitId = "ca-app-pub-3940256099942544/6978759866";
#else
            string adUnitId = "unexpected_platform";
#endif

            // Create a rewarded interstitial.
            RewardedInterstitialAd.Load(adUnitId, CreateAdRequest(),
                (ad, loadError) =>
                {
                    if (loadError != null)
                    {
                        PrintStatus("Rewarded interstitial ad failed to load with error: " +
                                    loadError.GetMessage());
                        return;
                    }

                    if (ad == null)
                    {
                        PrintStatus("Rewarded interstitial ad failed to load.");
                        return;
                    }

                    PrintStatus("Rewarded interstitial ad loaded.");
                    _rewardedInterstitialAd = ad;

                    ad.OnAdFullScreenContentOpened += () =>
                    {
                        PrintStatus("Rewarded interstitial ad opening.");
                        onAdOpeningEvent.Invoke();
                    };
                    ad.OnAdFullScreenContentClosed += () =>
                    {
                        PrintStatus("Rewarded interstitial ad closed.");
                        // OnAdClosedEvent.Invoke();
                    };
                    ad.OnAdImpressionRecorded += () =>
                    {
                        PrintStatus("Rewarded interstitial ad recorded an impression.");
                    };
                    ad.OnAdClicked += () => { PrintStatus("Rewarded interstitial ad recorded a click."); };
                    ad.OnAdFullScreenContentFailed += error =>
                    {
                        PrintStatus("Rewarded interstitial ad failed to show with error: " +
                                    error.GetMessage());
                    };
                    ad.OnAdPaid += adValue =>
                    {
                        var msg = string.Format("{0} (currency: {1}, value: {2}",
                            "Rewarded interstitial ad received a paid event.",
                            adValue.CurrencyCode,
                            adValue.Value);
                        PrintStatus(msg);
                    };
                });
        }

        /// <summary>
        /// 보상형 전면 광고 보여주기
        /// </summary>
        public void ShowRewardedInterstitialAd()
        {
            if (_rewardedInterstitialAd != null)
                _rewardedInterstitialAd.Show(reward =>
                {
                    PrintStatus("Rewarded interstitial granded a reward: " + reward.Amount);
                });
            else
                PrintStatus("Rewarded interstitial ad is not ready yet.");
        }

        #endregion
    }
}
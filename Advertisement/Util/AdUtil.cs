using System;
using IAP;
using IAP.Util;
using UnityEngine;
using Logger = CustomLogger.Logger;

namespace Advertisement.Util
{
    /// <summary>
    /// 광고 유틸
    /// </summary>
    public static class AdUtil
    {
        /// <summary>
        /// 마지막 자동 광고 시간
        /// </summary>
        private static DateTime LastAutoAdShownTime
        {
            get
            {
                var tempNextRewardTimeTicks = Convert.ToInt64(PlayerPrefs.GetString("LastAutoAdShownTime", "0"));
                return tempNextRewardTimeTicks == 0
                    ? DateTime.MinValue
                    : new DateTime(tempNextRewardTimeTicks, DateTimeKind.Utc);
            }
            set => PlayerPrefs.SetString("LastAutoAdShownTime", value.Ticks.ToString());
        }

        /// <summary>
        /// 광고제거 아이템 1을 소지하고 있는지
        /// </summary>
        /// <returns></returns>
        public static bool HasRemoveAds1Item()
        {
            return IAPUtil.HasItem(MarketPidKeys.removeAds1);
        }

        /// <summary>
        /// 광고제거 아이템 2를 소지하고 있는지
        /// </summary>
        /// <returns></returns>
        public static bool HasRemoveAds2Item()
        {
            return IAPUtil.HasItem(MarketPidKeys.removeAds2);
        }

        /// <summary>
        /// 광고 성공 결과 생성
        /// </summary>
        /// <param name="adType"></param>
        /// <param name="isRewarded"></param>
        /// <returns></returns>
        public static AdResult CreateSuccessResult(AdType adType, bool? isRewarded = null)
        {
            switch (adType)
            {
                case AdType.Rewarded:
                    if (isRewarded.HasValue == false)
                    {
                        Logger.LogError("isRewarded is null");
                        return CreateFailResult(adType, AdResultFailReason.DeveloperMistake);
                    }

                    return new RewardAdResult(isRewarded.Value);
                case AdType.Interstitial:
                    return new InterstitialAdResult();
                case AdType.Banner:
                    return new BannerAdResult();
                case AdType.None:
                default:
                    Logger.LogError("Not supported AdType : " + adType);
                    return CreateFailResult(adType, AdResultFailReason.DeveloperMistake);
            }
        }

        /// <summary>
        /// 광고 실패 결과 생성
        /// </summary>
        /// <param name="adType"></param>
        /// <param name="failReason"></param>
        /// <returns></returns>
        public static AdResult CreateFailResult(AdType adType, string failReason)
        {
            switch (adType)
            {
                case AdType.Rewarded:
                    return new RewardAdResult(false, failReason: failReason);
                case AdType.Interstitial:
                    return new InterstitialAdResult(failReason: failReason);
                case AdType.Banner:
                    return new BannerAdResult(failReason: failReason);
                case AdType.None:
                default:
                    Logger.LogError("Not supported AdType : " + adType);
                    return new EmptyAdResult(failReason: AdResultFailReason.DeveloperMistake);
            }
        }

        /// <summary>
        /// 광고 스킵 결과 생성
        /// </summary>
        /// <param name="adType"></param>
        /// <returns></returns>
        public static AdResult CreateSkippedResult(AdType adType)
        {
            switch (adType)
            {
                case AdType.Rewarded:
                    return new RewardAdResult(true, true);
                case AdType.Interstitial:
                    return new InterstitialAdResult(true);
                case AdType.Banner:
                    return new BannerAdResult(true);
                case AdType.None:
                default:
                    Logger.LogError("Not supported AdType : " + adType);
                    return new EmptyAdResult(true, AdResultFailReason.DeveloperMistake);
            }
        }

        /// <summary>
        /// 비디오형 광고 열기
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="adType"></param>
        /// <param name="failReason"></param>
        /// <returns></returns>
        public static bool OpenAd(this GoogleAdMobController controller, AdType adType, out string failReason)
        {
            failReason = "";
            switch (adType)
            {
                case AdType.Rewarded:
                    var result = controller.ShowRewardedAd();
                    if (result) return true;
                    failReason = AdResultFailReason.NoFill;
                    return false;
                case AdType.Interstitial:
                    if (IsAutoAdShowAble() == false)
                    {
                        failReason = AdResultFailReason.NotYetAutoAdTime;
                        return false;
                    }

                    result = controller.ShowInterstitialAd();
                    if (result) return true;
                    failReason = AdResultFailReason.NoFill;
                    return false;
                case AdType.None:
                case AdType.Banner:
                default:
                    Logger.LogError("Not supported AdType : " + adType);
                    failReason = AdResultFailReason.DeveloperMistake;
                    return false;
            }
        }

        /// <summary>
        /// 자동 광고 보여주기 가능한지
        /// </summary>
        /// <returns></returns>
        private static bool IsAutoAdShowAble()
        {
            if (HasRemoveAds1Item()) return false;

            var now = DateTime.UtcNow;
            if (LastAutoAdShownTime == DateTime.MinValue)
            {
                LastAutoAdShownTime = now;
                return true;
            }

            var interval = "auto_ad_interval".ToCommon().ToInt();

            var diff = now - LastAutoAdShownTime;
            if (diff.TotalSeconds < interval) return false;

            LastAutoAdShownTime = now;
            return true;
        }
    }
}
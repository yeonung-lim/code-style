using System;
using GoogleMobileAds.Api;
using UniRx;
using UnityEngine;

namespace Advertisement.Util
{
    /// <summary>
    /// 배너 광고를 위한 SafeArea
    /// </summary>
    public class SafeAreaOnlyBanner : MonoBehaviour
    {
        /// <summary>
        /// 적용 이벤트
        /// </summary>
        public Action<RectTransform> onApplied;
        /// <summary>
        /// 적용할 패널
        /// </summary>
        private RectTransform _panel;

        private void Awake()
        {
            AdManager.Instance.SubscribeBannerProperty(ApplySafeArea).AddTo(this);
        }

        /// <summary>
        ///  배너 광고의 위치에 따라 safe area를 적용한다.
        /// </summary>
        /// <param name="bannerProperty"></param>
        private void ApplySafeArea(BannerProperty bannerProperty)
        {
            _panel ??= GetComponent<RectTransform>();
            var r = Screen.safeArea;

            // Convert safe area rectangle from absolute pixels to normalized anchor coordinates
            var anchorMin = r.position;
            var anchorMax = r.position + r.size;

            anchorMin.x = 0f;
            anchorMax.x = 1f;

            const float bannerCanvasReferenceResolutionY = 1440f;
            var bannerAdHeight = bannerProperty.DeltaSize.y;
            var bannerAdjustedHeight = bannerAdHeight * Screen.height / bannerCanvasReferenceResolutionY;

            if (bannerProperty is { BannerPosition: AdPosition.Bottom })
                anchorMin.y = Mathf.Max(anchorMin.y, bannerAdjustedHeight);

            anchorMin.y /= Screen.height;

            if (bannerProperty is { BannerPosition: AdPosition.Top })
                anchorMax.y = Mathf.Min(anchorMax.y, Screen.height - bannerAdjustedHeight);

            anchorMax.y /= Screen.height;

            _panel.anchorMin = anchorMin;
            _panel.anchorMax = anchorMax;

            onApplied?.Invoke(_panel);
        }
    }
}
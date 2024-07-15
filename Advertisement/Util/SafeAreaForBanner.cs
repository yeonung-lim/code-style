using System;
using GoogleMobileAds.Api;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Advertisement.Util
{
    /// <summary>
    ///     배너 광고 + 노치를 위한 SafeArea
    /// </summary>
    public class SafeAreaForBanner : UIBehaviour
    {
        /// <summary>
        ///     좌우 무시 여부
        /// </summary>
        public bool ignoreLeftNRight;
        /// <summary>
        ///     하단 무시 여부
        /// </summary>
        public bool ignoreBottom;
        /// <summary>
        ///    상하 최대값 일치 여부
        /// </summary>
        public bool matchTopBottomMaxValue;

        /// <summary>
        ///     적용 이벤트
        /// </summary>
        public Action<RectTransform> onApplied;
        /// <summary>
        ///     적용할 패널
        /// </summary>
        private RectTransform _panel;

        protected override void Awake()
        {
            base.Awake();

            AdManager.Instance.SubscribeBannerProperty(bannerProperty =>
                ApplySafeArea(GetSafeArea(), bannerProperty)).AddTo(this);
        }

        /// <summary>
        ///     화면 크기가 변경되었을 때
        /// </summary>
        protected override void OnRectTransformDimensionsChange()
        {
            ApplySafeArea(GetSafeArea(), AdManager.Instance.GetBannerProperty());
        }

        /// <summary>
        ///     safe area를 반환한다.
        /// </summary>
        /// <returns></returns>
        private Rect GetSafeArea()
        {
            return Screen.safeArea;
        }

        /// <summary>
        /// 안전 영역을 적용한다.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="bannerProperty"></param>
        private void ApplySafeArea(Rect r, BannerProperty bannerProperty)
        {
            _panel ??= GetComponent<RectTransform>();

            // Convert safe area rectangle from absolute pixels to normalized anchor coordinates
            var anchorMin = r.position;
            var anchorMax = r.position + r.size;
            if (!ignoreLeftNRight)
            {
                anchorMin.x /= Screen.width;
                anchorMax.x /= Screen.width;
            }
            else
            {
                anchorMin.x = 0f;
                anchorMax.x = 1f;
            }

            // 배너 계산
            var bannerAdjustedHeight = 0f;
#if UNITY_EDITOR
            const float bannerCanvasReferenceResolutionY = 1440f;
            bannerAdjustedHeight = bannerProperty.DeltaSize.y * Screen.height / bannerCanvasReferenceResolutionY;
#else
                bannerAdjustedHeight =
 bannerProperty.IsOpened == false ? 0f : (bannerProperty.DeltaSize.y /  bannerProperty.DeltaSize.x) * Screen.width;
#endif

            if (!ignoreBottom)
            {
                if (bannerProperty is { BannerPosition: AdPosition.Bottom })
                    anchorMin.y += bannerAdjustedHeight;

                anchorMin.y /= Screen.height;
            }
            else
            {
                anchorMin.y = 0f;
            }

            if (bannerProperty is { BannerPosition: AdPosition.Top })
                anchorMax.y -= bannerAdjustedHeight;

            anchorMax.y /= Screen.height;

            if (matchTopBottomMaxValue)
            {
                var top = 1f - anchorMax.y;
                var bottom = anchorMin.y;
                var max = Mathf.Max(top, bottom);
                anchorMax.y = 1f - max;
                anchorMin.y = max;
            }

            _panel.anchorMin = anchorMin;
            _panel.anchorMax = anchorMax;

            onApplied?.Invoke(_panel);
        }
    }
}
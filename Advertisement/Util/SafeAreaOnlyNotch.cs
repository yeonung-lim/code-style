using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Advertisement.Util
{
    /// <summary>
    /// 노치 영역만큼만 화면을 사용하도록 설정하는 클래스
    /// </summary>
    public class SafeAreaOnlyNotch : UIBehaviour
    {
        /// <summary>
        /// 왼쪽, 오른쪽 무시
        /// </summary>
        public bool ignoreLeftNRight;
        /// <summary>
        /// 하단 무시
        /// </summary>
        public bool ignoreBottom;
        /// <summary>
        /// 상하 최대값 일치 여부
        /// </summary>
        public bool matchTopBottomMaxValue;

        /// <summary>
        /// 적용 이벤트
        /// </summary>
        public Action<RectTransform> onApplied;
        /// <summary>
        /// 적용할 패널
        /// </summary>
        private RectTransform _panel;

        /// <summary>
        ///     화면 크기가 변경되었을 때
        /// </summary>
        protected override void OnRectTransformDimensionsChange()
        {
            ApplySafeArea(GetSafeArea());
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
        private void ApplySafeArea(Rect r)
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

            if (!ignoreBottom)
                anchorMin.y /= Screen.height;
            else
                anchorMin.y = 0f;

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
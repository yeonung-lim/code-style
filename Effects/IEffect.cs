using UnityEngine;
using UnityEngine.Events;

namespace Effects
{
    /// <summary>
    ///     이펙트 인터페이스
    /// </summary>
    public interface IEffect
    {
        /// <summary>
        ///     이펙트 트랜스폼
        /// </summary>
        public Transform Transform { get; }

        /// <summary>
        ///     이펙트가 끝났을 때 이벤트
        /// </summary>
        public UnityEvent OnFinished { get; }

        /// <summary>
        ///     이펙트 재생
        /// </summary>
        public void Play();

        /// <summary>
        ///     이펙트 정지
        /// </summary>
        public void Stop();

        /// <summary>
        ///     이펙트 해제
        /// </summary>
        public void Release();
    }
}
using AssetKits.ParticleImage;
using UnityEngine;
using UnityEngine.Events;

namespace Effects.Pooling
{
    /// <summary>
    ///     UI 풀링 파티클
    /// </summary>
    public class UIPoolingParticle : IPoolingEffect
    {
        /// <summary>
        ///     생성자
        /// </summary>
        /// <param name="effectObject">파티클 이미지</param>
        public UIPoolingParticle(ParticleImage effectObject)
        {
            EffectObject = effectObject;
        }

        /// <summary>
        ///     파티클 이미지
        /// </summary>
        public ParticleImage EffectObject { get; }

        /// <summary>
        ///     이펙트 풀
        /// </summary>
        public IEffectPool EffectPool { get; set; }

        /// <summary>
        ///     이펙트 트랜스폼
        /// </summary>
        public Transform Transform => EffectObject.transform;

        /// <summary>
        ///     이펙트 종료 이벤트
        /// </summary>
        public UnityEvent OnFinished => EffectObject.main.onLastParticleFinished;

        /// <summary>
        ///     이펙트 재생
        /// </summary>
        public void Play()
        {
            EffectObject.Play();
        }

        /// <summary>
        ///     이펙트 정지
        /// </summary>
        public void Stop()
        {
            EffectObject.Stop();
        }

        /// <summary>
        ///     이펙트 해제
        /// </summary>
        public void Release()
        {
            EffectPool.Release(this);
        }
    }
}
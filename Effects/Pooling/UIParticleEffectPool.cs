using AssetKits.ParticleImage;
using UnityEngine;
using UnityEngine.Pool;

namespace Effects.Pooling
{
    /// <summary>
    ///     UI 파티클 이펙트 풀
    /// </summary>
    public class UIParticleEffectPool : IEffectPool
    {
        /// <summary>
        ///     이펙트 풀
        /// </summary>
        private readonly ObjectPool<IPoolingEffect> _mPool;

        /// <summary>
        ///     생성자
        /// </summary>
        /// <param name="prefab">파티클 이미지 프리팹</param>
        /// <param name="container">릴리즈시 이펙트가 들어갈 컨테이너</param>
        /// <param name="activeContainer">활성화시 이펙트가 들어갈 컨테이너</param>
        public UIParticleEffectPool(ParticleImage prefab, Transform container, Transform activeContainer)
        {
            _mPool = CreatePool(prefab, container, activeContainer);
        }

        /// <summary>
        ///     이펙트 반환
        /// </summary>
        /// <param name="obj">IPoolingEffect 인터페이스</param>
        public void Release(IPoolingEffect obj)
        {
            _mPool.Release(obj);
        }

        /// <summary>
        ///     이펙트 가져오기
        /// </summary>
        /// <returns>IPoolingEffect 인터페이스</returns>
        public IPoolingEffect Get()
        {
            return _mPool.Get();
        }

        /// <summary>
        ///     풀 생성
        /// </summary>
        /// <param name="prefab">파티클 이미지 프리팹</param>
        /// <param name="container">릴리즈시 이펙트가 들어갈 컨테이너</param>
        /// <param name="activeContainer">활성화시 이펙트가 들어갈 컨테이너</param>
        /// <returns>오브젝트 풀</returns>
        private static ObjectPool<IPoolingEffect> CreatePool(ParticleImage prefab, Transform container,
            Transform activeContainer)

        {
            return new ObjectPool<IPoolingEffect>(CreateFunc, OnGet, OnRelease);

            UIPoolingParticle CreateFunc()
            {
                var effect = Object.Instantiate(prefab, container);
                effect.transform.localScale = prefab.transform.localScale;

                return new UIPoolingParticle(effect);
            }

            void OnGet(IPoolingEffect effect)
            {
                effect.Transform.SetParent(activeContainer, true);
            }

            void OnRelease(IPoolingEffect effect)
            {
                effect.Transform.SetParent(container, true);
            }
        }
    }
}
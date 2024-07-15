using System.Collections.Generic;
using AssetKits.ParticleImage;
using Effects.Pooling;
using RotaryHeart.Lib.SerializableDictionary;
using UnityEngine;
using USingleton.AutoSingleton;

namespace Effects
{
    /// <summary>
    ///     이펙트 매니저
    /// </summary>
    [Singleton(nameof(EffectManager))]
    public class EffectManager : MonoBehaviour
    {
        /// <summary>
        ///     기본 컨테이너
        /// </summary>
        [SerializeField] private Transform mDefaultContainer;

        /// <summary>
        ///     기본 활성화 컨테이너
        /// </summary>
        [SerializeField] private Transform mDefaultActiveContainer;

        /// <summary>
        ///     이펙트 딕셔너리
        /// </summary>
        [SerializeField]
        private SerializableDictionaryBase<EffectType, PoolingMeta<ParticleImage>> mParticleImageDictionary;

        /// <summary>
        ///     풀링 파티클 풀
        /// </summary>
        private readonly Dictionary<EffectType, IEffectPool> _mPoolingParticlePool = new();

        private void Awake()
        {
            InitPool();
        }

        /// <summary>
        ///     풀 초기화
        ///     ParticleImageDictionary에 있는
        ///     EffectType,PoolingMeta로 풀을 초기화합니다.
        /// </summary>
        private void InitPool()
        {
            foreach (var (key, value) in mParticleImageDictionary)
            {
                var isDefaultContainer = value.containerType == PoolingContainerType.Default;
                var container = isDefaultContainer ? mDefaultContainer : value.container;
                var activeContainer = isDefaultContainer ? mDefaultActiveContainer : value.activeContainer;

                _mPoolingParticlePool[key] =
                    CreatePoolingParticlePool(value.prefab, container, activeContainer);
            }
        }

        /// <summary>
        ///     이펙트 가져오기
        /// </summary>
        /// <param name="effectType">이펙트 타입</param>
        /// <returns>IEffect 인터페이스</returns>
        public IEffect GetEffect(EffectType effectType)
        {
            return GetEffect(effectType, true);
        }

        /// <summary>
        ///     이펙트 가져오기
        /// </summary>
        /// <param name="effectType">이펙트 타입</param>
        /// <param name="isAutoRelease">이펙트가 끝나면 자동으로 수거할 지</param>
        /// <returns>IEffect 인터페이스</returns>
        public IEffect GetEffect(EffectType effectType, bool isAutoRelease)
        {
            var container = GetMeta(effectType).containerType == PoolingContainerType.Default
                ? mDefaultActiveContainer
                : mParticleImageDictionary[effectType].activeContainer;

            return GetEffect(effectType, container, isAutoRelease);
        }

        /// <summary>
        ///     이펙트 가져오기
        /// </summary>
        /// <param name="effectType">이펙트 타입</param>
        /// <param name="container">이펙트 부모</param>
        /// <returns>IEffect 인터페이스</returns>
        public IEffect GetEffect(EffectType effectType, Transform container)
        {
            return GetEffect(effectType, container, true);
        }

        /// <summary>
        ///     이펙트 가져오기
        /// </summary>
        /// <param name="effectType">이펙트 타입</param>
        /// <param name="container">이펙트 부모</param>
        /// <param name="worldPosition">월드 포지션</param>
        /// <returns>IEffect 인터페이스</returns>
        public IEffect GetEffect(EffectType effectType, Transform container, Vector3 worldPosition)
        {
            var effect = GetEffect(effectType, container, true);

            effect.Transform.position = new Vector3(worldPosition.x, worldPosition.y, container.position.z);

            return effect;
        }

        /// <summary>
        ///     이펙트 가져오기
        /// </summary>
        /// <param name="effectType">이펙트 타입</param>
        /// <param name="container">이펙트 부모</param>
        /// <param name="isAutoRelease">이펙트가 끝나면 자동으로 수거할 지</param>
        /// <returns>IEffect 인터페이스</returns>
        public IEffect GetEffect(EffectType effectType, Transform container, bool isAutoRelease)
        {
            var prefab = GetMeta(effectType).prefab;

            return new UIParticle(
                Instantiate(prefab, container), isAutoRelease);
        }

        /// <summary>
        ///     파티클 이미지 메타 가져오기
        /// </summary>
        /// <param name="effectType">이펙트 타입</param>
        /// <returns>파티클 이미지 메타</returns>
        private PoolingMeta<ParticleImage> GetMeta(EffectType effectType)
        {
            return mParticleImageDictionary[effectType];
        }

        /// <summary>
        ///     풀링 이펙트 가져오기
        /// </summary>
        /// <param name="effectType">이펙트 타입</param>
        /// <returns>IPoolingEffect 인터페이스</returns>
        public IPoolingEffect GetPoolingEffect(EffectType effectType)
        {
            return GetPoolingEffect(effectType, true);
        }

        /// <summary>
        ///     풀링 이펙트 가져오기
        /// </summary>
        /// <param name="effectType">이펙트 타입</param>
        /// <param name="worldPosition">월드 포지션</param>
        /// <returns>IPoolingEffect 인터페이스</returns>
        public IPoolingEffect GetPoolingEffect(EffectType effectType, Vector3 worldPosition)
        {
            var effect = GetPoolingEffect(effectType, true);
            effect.Transform.position = worldPosition;

            return effect;
        }

        /// <summary>
        ///     풀링 이펙트 가져오기
        /// </summary>
        /// <param name="effectType">이펙트 타입</param>
        /// <param name="activeContainer">풀링 됐을 때 옮겨 갈 부모</param>
        /// <param name="worldPosition">월드 포지션</param>
        /// <returns>IPoolingEffect 인터페이스</returns>
        public IPoolingEffect GetPoolingEffect(EffectType effectType, Transform activeContainer, Vector3 worldPosition)
        {
            var effect = GetPoolingEffect(effectType, worldPosition);
            effect.Transform.SetParent(activeContainer, true);
            return effect;
        }

        /// <summary>
        ///     풀링 이펙트 가져오기
        /// </summary>
        /// <param name="effectType">이펙트 타입</param>
        /// <param name="isAutoRelease">이펙트가 끝나면 자동으로 수거할 지</param>
        /// <returns>IPoolingEffect 인터페이스</returns>
        public IPoolingEffect GetPoolingEffect(EffectType effectType, bool isAutoRelease)
        {
            var effect = _mPoolingParticlePool[effectType].Get();
            effect.EffectPool = _mPoolingParticlePool[effectType];
            effect.OnFinished.RemoveAllListeners();

            if (isAutoRelease) effect.OnFinished.AddListener(effect.Release);

            return effect;
        }

        /// <summary>
        ///     풀링 파티클 풀 생성
        /// </summary>
        /// <param name="particleImagePrefab">파티클 이미지 프리팹</param>
        /// <param name="container">릴리즈 되었을 때 이동할 컨테이너</param>
        /// <param name="activeContainer">풀링 됐을 때 옮겨 갈 부모</param>
        /// <returns></returns>
        private static UIParticleEffectPool CreatePoolingParticlePool(
            ParticleImage particleImagePrefab, Transform container, Transform activeContainer)
        {
            return new UIParticleEffectPool(particleImagePrefab, container, activeContainer);
        }
    }
}
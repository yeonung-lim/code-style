using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Effects.Pooling
{
    /// <summary>
    ///     풀링 메타
    ///     풀링할 프리팹과 컨테이너 정보를 가지고 있는 클래스
    /// </summary>
    /// <typeparam name="T">풀링할 프리팹 타입</typeparam>
    [Serializable]
    public class PoolingMeta<T>
    {
        /// <summary>
        ///     풀링할 프리팹
        /// </summary>
        public T prefab;

        /// <summary>
        ///     풀링 컨테이너 타입
        /// </summary>
        public PoolingContainerType containerType;

        /// <summary>
        ///     특정 풀링 컨테이너
        /// </summary>
        [ShowIf("@containerType == PoolingContainerType.Specific")]
        public Transform container;

        /// <summary>
        ///     특정 활성화 풀링 컨테이너
        /// </summary>
        [ShowIf("@containerType == PoolingContainerType.Specific")]
        public Transform activeContainer;
    }
}
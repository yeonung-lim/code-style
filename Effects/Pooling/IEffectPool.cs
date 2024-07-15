namespace Effects.Pooling
{
    /// <summary>
    ///     이펙트 풀 인터페이스
    /// </summary>
    public interface IEffectPool
    {
        /// <summary>
        ///     이펙트 가져오기
        /// </summary>
        /// <returns>IPoolingEffect 인터페이스</returns>
        public IPoolingEffect Get();

        /// <summary>
        ///     이펙트 반환
        /// </summary>
        /// <param name="obj">IPoolingEffect 인터페이스</param>
        public void Release(IPoolingEffect obj);
    }
}
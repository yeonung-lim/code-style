namespace Effects.Pooling
{
    /// <summary>
    ///     풀링 이펙트 인터페이스
    /// </summary>
    public interface IPoolingEffect : IEffect
    {
        /// <summary>
        ///     이펙트 풀
        /// </summary>
        public IEffectPool EffectPool { get; set; }
    }
}
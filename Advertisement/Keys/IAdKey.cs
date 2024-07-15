namespace Advertisement.Keys
{
    /// <summary>
    /// 광고 키 인터페이스
    /// </summary>
    public interface IAdKey
    {
        /// <summary>
        /// 광고 타입
        /// </summary>
        AdType AdType { get; }
        /// <summary>
        /// 스킵 가능 여부
        /// </summary>
        bool SkippAble { get; }
    }
}
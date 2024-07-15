namespace Advertisement.Keys
{
    /// <summary>
    /// 광고 키 추상 클래스
    /// </summary>
    public abstract class BaseAdKey : IAdKey
    {
        /// <summary>
        /// 광고 타입
        /// </summary>
        protected readonly AdType ADType;

        /// <summary>
        /// 광고 키 생성자
        /// </summary>
        /// <param name="adType"></param>
        protected BaseAdKey(AdType adType)
        {
            ADType = adType;
        }

        /// <summary>
        /// 광고 타입 반환
        /// </summary>
        public abstract AdType AdType { get; }
        /// <summary>
        /// 스킵 가능 여부
        /// </summary>
        public abstract bool SkippAble { get; }
    }
}
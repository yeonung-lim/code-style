namespace Advertisement.Keys
{
    /// <summary>
    /// 광고제거 아이템으로 스킵 불가능한 광고
    /// </summary>
    public class UnSkippAbleAd : BaseAdKey
    {
        public UnSkippAbleAd(AdType adType) : base(adType)
        {
        }

        public override AdType AdType => ADType;
        public override bool SkippAble => false;
    }
}
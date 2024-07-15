using Advertisement.Util;

namespace Advertisement.Keys
{
    /// <summary>
    /// 광고제거 아이템 2로 스킵 가능한 광고
    /// </summary>
    public class SkippAbleAd2 : BaseAdKey
    {
        public SkippAbleAd2(AdType adType) : base(adType)
        {
        }

        public override AdType AdType => ADType;
        public override bool SkippAble => AdUtil.HasRemoveAds2Item();
    }
}
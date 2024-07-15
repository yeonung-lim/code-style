using Advertisement.Util;

namespace Advertisement.Keys
{
    /// <summary>
    /// 광고제거 아이템 아무거나 소지하고 있으면 스킵 가능한 광고
    /// </summary>
    public class SkippAbleAdWithAnyItem : BaseAdKey
    {
        public SkippAbleAdWithAnyItem(AdType adType) : base(adType)
        {
        }

        public override AdType AdType => ADType;
        public override bool SkippAble => AdUtil.HasRemoveAds1Item() || AdUtil.HasRemoveAds2Item();
    }
}
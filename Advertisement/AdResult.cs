namespace Advertisement
{
    /// <summary>
    /// 보상 광고 결과
    /// </summary>
    public class RewardAdResult : AdResult
    {
        public RewardAdResult(bool isReward, bool isSkipped = false, string failReason = "") : base(isReward, isSkipped,
            failReason)
        {
        }
    }

    /// <summary>
    /// 전면 광고 결과
    /// </summary>
    public class InterstitialAdResult : AdResult
    {
        public InterstitialAdResult(bool isSkipped = false, string failReason = "") : base(false, isSkipped, failReason)
        {
        }
    }

    /// <summary>
    /// 배너 광고 결과
    /// </summary>
    public class BannerAdResult : AdResult
    {
        public BannerAdResult(bool isSkipped = false, string failReason = "") : base(false, isSkipped, failReason)
        {
        }
    }

    /// <summary>
    /// 빈 광고 결과
    /// </summary>
    public class EmptyAdResult : AdResult
    {
        public EmptyAdResult(bool isSkipped = false, string failReason = "") : base(false, false, failReason)
        {
        }
    }

    /// <summary>
    /// 광고 결과 추상 클래스
    /// </summary>
    public abstract class AdResult
    {
        /// <summary>
        /// 광고 결과 생성자
        /// </summary>
        /// <param name="isReward">보상 받았는지</param>
        /// <param name="isSkipped">스킵 여부</param>
        /// <param name="failReason">실패 이유</param>
        protected AdResult(bool isReward, bool isSkipped = false, string failReason = "")
        {
            IsReward = isReward;
            FailReason = failReason;
            IsSkipped = isSkipped;
        }

        /// <summary>
        /// 보상을 얻었는지
        /// </summary>
        public bool IsReward { get; set; }

        /// <summary>
        /// 광고 실패 이유
        /// </summary>
        public string FailReason { get; set; }

        /// <summary>
        /// 스킵 여부
        /// </summary>
        public bool IsSkipped { get; set; }
    }

    /// <summary>
    /// 광고 실패 이유
    /// </summary>
    public abstract class AdResultFailReason
    {
        /// <summary>
        /// 인터넷 연결이 없음
        /// </summary>
        public const string NoInternet = "NoInternet";
        /// <summary>
        /// 광고 없음
        /// </summary>
        public const string NoFill = "NoFill";
        /// <summary>
        /// 자동 광고 시간이 아직 안됨
        /// </summary>
        public const string NotYetAutoAdTime = "NotYetAutoAdTime";
        /// <summary>
        /// 이미 광고 중
        /// </summary>
        public const string AlreadyPlaying = "AlreadyPlaying";
        /// <summary>
        /// 개발자 실수
        /// </summary>
        public const string DeveloperMistake = "DeveloperMistake";
        /// <summary>
        /// 알 수 없음
        /// </summary>
        public const string Unknown = "Unknown";
    }
}
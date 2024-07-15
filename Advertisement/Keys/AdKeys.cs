namespace Advertisement.Keys
{
    /// <summary>
    /// 광고 키
    /// </summary>
    public static class AdKeys
    {
        #region Banner

        /// <summary>
        /// 배너 광고
        /// </summary>
        public static readonly SkippAbleAd1 Banner = new(AdType.Banner);

        #endregion

        #region Interstitial

        /// <summary>
        /// 업적에서 보석 획득시 전면 광고
        /// </summary>
        public static readonly SkippAbleAd1 GetGemInAchievements = new(AdType.Interstitial);
        /// <summary>
        /// 진화 후 전면 광고
        /// </summary>
        public static readonly SkippAbleAd1 AfterEvolution = new(AdType.Interstitial);
        /// <summary>
        /// 방 변경시 전면 광고
        /// </summary>
        public static readonly SkippAbleAd1 RoomChanged = new(AdType.Interstitial);
        /// <summary>
        /// 출석 체크 보상 획득시 전면 광고
        /// </summary>
        public static readonly SkippAbleAd1 GetAttendanceCheckRewards = new(AdType.Interstitial);
        /// <summary>
        /// 미니게임 버튼 클릭시 전면 광고
        /// </summary>
        public static readonly SkippAbleAd1 MiniGameButtonClicked = new(AdType.Interstitial);

        #endregion

        #region Rewarded

        /// <summary>
        /// 출석 보상 광고
        /// </summary>
        public static readonly SkippAbleAd1 AttendanceReward = new(AdType.Rewarded);
        /// <summary>
        /// 미션 보상 광고
        /// </summary>
        public static readonly SkippAbleAd1 MissionReward = new(AdType.Rewarded);
        /// <summary>
        /// 리워드 팩 광고
        /// </summary>
        public static readonly SkippAbleAd2 RewardPack = new(AdType.Rewarded);
        /// <summary>
        /// 선물 상자 광고
        /// </summary>
        public static readonly SkippAbleAd2 GiftBox = new(AdType.Rewarded);
        /// <summary>
        /// 수면 스킵 광고
        /// </summary>
        public static readonly SkippAbleAd2 SleepTimeSkip = new(AdType.Rewarded);
        /// <summary>
        /// 미니게임 보상 광고
        /// </summary>
        public static readonly UnSkippAbleAd MiniGameReward = new(AdType.Rewarded);
        /// <summary>
        /// 오프라인 보상 광고
        /// </summary>
        public static readonly UnSkippAbleAd OfflineReward = new(AdType.Rewarded);
        /// <summary>
        /// 일일 코인 광고
        /// </summary>
        public static readonly UnSkippAbleAd DailyCoin = new(AdType.Rewarded);
        /// <summary>
        /// 일일 보석 광고
        /// </summary>
        public static readonly UnSkippAbleAd DailyGem = new(AdType.Rewarded);
        /// <summary>
        /// 레벨업 두 배 보상 광고
        /// </summary>
        public static readonly UnSkippAbleAd LevelUpDoubleReward = new(AdType.Rewarded);

        #endregion
    }
}
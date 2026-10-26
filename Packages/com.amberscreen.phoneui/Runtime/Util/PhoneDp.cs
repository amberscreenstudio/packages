namespace AmberScreen.PhoneUI
{
    /// <summary>Logical density-independent sizes for the phone UI (reference width = 360 dp).</summary>
    public static class PhoneDp
    {
        public const float ReferenceWidth = 360f;

        public const float StatusBar = 24f;
        public const float NavBar = 48f;
        public const float Composer = 48f;
        public const float BezelInset = 8f;

        public const float BodyText = 15f;
        public const float NameText = 13f;
        public const float TimestampText = 11f;
        public const float TitleText = 18f;

        public const float IconSize = 56f;
        public const float IconLabel = 12f;
        public const float IconCellW = 72f;
        public const float IconCellH = 78f;

        public const float BubbleMaxWidthFraction = 0.75f;
        public static float BubbleMaxWidth => ReferenceWidth * BubbleMaxWidthFraction;
    }
}

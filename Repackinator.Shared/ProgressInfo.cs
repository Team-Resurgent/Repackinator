namespace Repackinator.Shared
{
    public struct ProgressInfo
    {
        public float Progress1 { get; set; }

        public string Progress1Text { get; set; }

        public float Progress2 { get; set; }

        public string Progress2Text { get; set; }

        public ProgressInfo()
        {
            Progress1 = 0;
            Progress1Text = string.Empty;
            Progress2 = 0;
            Progress2Text = string.Empty;
        }
    }
}

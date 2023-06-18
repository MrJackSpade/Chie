namespace ChieApi.Shared.Entities
{
    public class UserData
    {
        public int Id { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string UserPrompt { get; set; } = string.Empty;

        public string UserSummary { get; set; } = string.Empty;
    }
}
using System.ComponentModel.DataAnnotations;

namespace ChieApi.Shared.Entities
{
    public class UserData
    {
        public bool IsBot { get; set; }

        public bool Admin { get; set; }

        public bool BeforeMessage { get; set; } = false;

        public bool Blocked { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public DateTime? LastEncountered { get; set; }

        [Key]
        public long Id { get; set; }

        public long LastChatId { get; set; } = 0;

        public string UserId { get; set; } = string.Empty;

        public string UserPrompt { get; set; } = string.Empty;

        public string UserSummary { get; set; } = string.Empty;
    }
}
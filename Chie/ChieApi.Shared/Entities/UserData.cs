using System.ComponentModel.DataAnnotations;

namespace ChieApi.Shared.Entities
{
    public class UserData
    {
        public bool Admin { get; set; }

        public bool BeforeMessage { get; set; } = false;

        public bool Blocked { get; set; }

        [Key]
        public int Id { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string UserPrompt { get; set; } = string.Empty;

        public string UserSummary { get; set; } = string.Empty;
    }
}
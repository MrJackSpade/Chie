using Summary.Models;

namespace Summary.Interfaces
{
    public interface ISummaryApiClient
    {
        Task<SummaryResponse> Summarize(string data, int maxLength);
        Task<TokenizeResponse> Tokenize(string data);
    }
}
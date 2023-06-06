using System.Collections.Generic;
using System.Text;

namespace LLama.Interfaces
{
    public interface IChatModel
    {
        string Name { get; }

        IEnumerable<string> Chat(string text, Encoding encoding, string? prompt = null);

        void InitChatAntiprompt(string[] antiprompt);

        /// <summary>
        /// Init a prompt for chat and automatically produce the next prompt during the chat.
        /// </summary>
        /// <param name="prompt"></param>
        void InitChatPrompt(string prompt, Encoding encoding);
    }
}
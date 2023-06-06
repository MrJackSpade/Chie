using LLama.Types;
using System;
using System.Collections.Generic;
using System.IO;

namespace LLama
{
	public class ChatSession<T> where T : IChatModel
	{
		private readonly IChatModel _model;

		public ChatSession(T model)
		{
			this._model = model;
		}

		private List<ChatMessageRecord> History { get; } = new List<ChatMessageRecord>();

		public IEnumerable<string> Chat(string text, string? prompt = null, string encoding = "UTF-8")
		{
			this.History.Add(new ChatMessageRecord(new ChatCompletionMessage(ChatRole.Human, text), DateTime.Now));
			string totalResponse = string.Empty;
			foreach (string response in this._model.Chat(text, prompt, encoding))
			{
				totalResponse += response;
				yield return response;
			}

			this.History.Add(new ChatMessageRecord(new ChatCompletionMessage(ChatRole.Assistant, totalResponse), DateTime.Now));
		}

		/// <summary>
		/// Set the keyword to split the return value of chat AI.
		/// </summary>
		/// <param name="humanName"></param>
		/// <returns></returns>
		public ChatSession<T> WithAntiprompt(string[] antiprompt)
		{
			this._model.InitChatAntiprompt(antiprompt);
			return this;
		}

		public ChatSession<T> WithPrompt(string prompt, string encoding = "UTF-8")
		{
			this._model.InitChatPrompt(prompt, encoding);
			return this;
		}

		public ChatSession<T> WithPromptFile(string promptFilename, string encoding = "UTF-8") => this.WithPrompt(File.ReadAllText(promptFilename), encoding);
	}
}
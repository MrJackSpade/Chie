using LLama.Interfaces;
using LLama.Models;
using LLama.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

		public IEnumerable<LlamaToken> Chat(string text, Encoding encoding, string? prompt = null)
		{
			this.History.Add(new ChatMessageRecord(new ChatCompletionMessage(ChatRole.Human, text), DateTime.Now));
			string totalResponse = string.Empty;

			foreach (LlamaToken response in this._model.Chat(text, prompt))
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

		public ChatSession<T> WithPrompt(string prompt, Encoding encoding)
		{
			this._model.InitChatPrompt(prompt);
			return this;
		}

		public ChatSession<T> WithPromptFile(string promptFilename, Encoding encoding) => this.WithPrompt(File.ReadAllText(promptFilename), encoding);
	}
}
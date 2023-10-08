using Llama.Data.Interfaces;
using Llama.Data.Models;
using Llama.Data.Native;
using Llama.Native;
using System.Diagnostics;
using System.Text;

namespace Llama.Core.Samplers.Mirostat
{
	public class MirostatTempSampler : ITokenSelector
	{
		private readonly Dictionary<int, bool> _isWords = new();

		private readonly MirostatTempSamplerSettings _settings;

		private float _mu;

		private float _temp;

		private LlamaTokenData[] _tempCandidates;

		public MirostatTempSampler(MirostatTempSamplerSettings settings)
		{
			this._settings = settings;
			this._mu = settings.InitialMu;
			this._temp = settings.InitialTemperature;
		}

		public static int Clamp(float k)
		{
			if (k <= 0)
			{
				return 0;
			}
			else if (k >= int.MaxValue)
			{
				return int.MaxValue;
			}
			else
			{
				return (int)k;
			}
		}

		public string GetDisplayString(SampleContext ctx, LlamaTokenData data)
		{
			LlamaToken token = this.GetToken(ctx, data.id);

			return $"{token.GetEscapedValue()} ({data.p:0.00})";
		}

		public LlamaToken GetToken(SampleContext ctx, int id) => new(id, NativeApi.TokenToPiece(ctx.ContextHandle, id));

		private void Copy(Span<LlamaTokenData> sampleContext)
		{
			this._tempCandidates ??= new LlamaTokenData[sampleContext.Length];
			Span<LlamaTokenData> target = new(this._tempCandidates, 0, this._tempCandidates.Length);
			sampleContext.CopyTo(target);
		}

		private float GetBackupP(int id)
		{
			foreach (LlamaTokenData ltd in this._tempCandidates)
			{
				if(ltd.id == id)
				{
					return ltd.p;
				}
			}

			throw new InvalidDataException();
		}

		public int SampleNext(SampleContext sampleContext)
		{
			//Softmax for backup
			SamplingApi.SoftMax(sampleContext.ContextHandle, sampleContext.Candidates);
			Span<LlamaTokenData> candidateSpan = sampleContext.Candidates.data.Span;
			this.Copy(candidateSpan);

			SamplingApi.Temperature(sampleContext.ContextHandle, sampleContext.Candidates, this._temp);
			SamplingApi.SoftMax(sampleContext.ContextHandle, sampleContext.Candidates);

			float tau = this._settings.Target;
			float eta = this._settings.LearningRate;

			Debug.WriteLine($"Temp: {this._temp}");

			bool topOnly = false;
			int top_x = 0;

			if (this._settings.PreserveWords)
			{
				top_x = SamplingApi.TokenGreedy(sampleContext.ContextHandle, sampleContext.Candidates);
				topOnly = !this.CheckIfWord(sampleContext.ContextHandle, top_x);
			}

			int x;

			if (topOnly)
			{
				x = top_x;
			}
			else
			{
				int n_keep = candidateSpan.Length;
				float min_p = 1;

				if (this._settings.MinP > 0)
				{
					min_p = this._settings.MinP;
				}

				if (this._settings.TopK > 0)
				{
					n_keep = this._settings.TopK;
				}

				// Sample the next word X using top-k sampling
				SamplingApi.TopK(sampleContext.ContextHandle, sampleContext.Candidates, n_keep, 1);

				float cut_p = candidateSpan[0].p * min_p;

				for (int i = 0; i < candidateSpan.Length; i++)
				{
					if (i >= n_keep || candidateSpan[i].p < cut_p)
					{
						//How else do I supress this?
						candidateSpan[i].logit = float.NegativeInfinity;
					}
				}

				SamplingApi.SoftMax(sampleContext.ContextHandle, sampleContext.Candidates);
				x = SamplingApi.Token(sampleContext.ContextHandle, sampleContext.Candidates);
			}

			// Compute error as the difference between observed surprise and target surprise value
			int x_idx = 0;

			for (int i = 0; i < (int)sampleContext.Candidates.size; i++)
			{
				if (candidateSpan[i].id == x)
				{
					x_idx = i;
					break;
				}
			}

			StringBuilder candidateBuilder = new();

			candidateBuilder.Append($"[{this.GetDisplayString(sampleContext, candidateSpan[x_idx])}] || ");

			for (int i = 0; i < (topOnly ? 1 : 10); i++)
			{
				if (candidateSpan[i].p == 0)
				{
					break;
				}

				if (i > 0)
				{
					candidateBuilder.Append(" | ");
				}

				candidateBuilder.Append(this.GetDisplayString(sampleContext, candidateSpan[i]));
			}

			Debug.WriteLine(candidateBuilder.ToString());

			//Calculate surprise based on the original P to
			//ensure that wonky probability fuckery doesn't mess
			//up the surprise calculations
			float original_p = this.GetBackupP(x);

			if (!topOnly || this._settings.FactorPreservedWords)
			{
				float observed_surprise = -(float)(Math.Log(original_p) / Math.Log(2));
				float e = observed_surprise - tau;

				// Update mu using the learning rate and error
				float adj = eta * e;
				float nuMu = this._mu - adj;
				float nuTemp = this._temp - adj * this._settings.TemperatureLearningRate;

				if(nuTemp > 0 && !float.IsNaN(nuTemp) && !float.IsInfinity(nuTemp) && !float.IsNaN(nuMu) && !float.IsInfinity(nuMu))
				{
					this._temp = nuTemp;
					this._mu = nuMu;
				}
			}

			Debug.WriteLine($"mu: {this._mu}");

			return x;
		}

		private bool CheckIfWord(SafeLlamaContextHandle ctx, int id)
		{
			if (!this._isWords.TryGetValue(id, out bool word))
			{
				string value = NativeApi.TokenToPiece(ctx, id);
				word = !string.IsNullOrWhiteSpace(value) && !char.IsLetter(value[0]);
				this._isWords.Add(id, word);
			}

			return word;
		}
	}
}
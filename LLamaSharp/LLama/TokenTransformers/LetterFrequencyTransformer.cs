using Llama.Collections;
using Llama.Interfaces;
using Llama.Models;
using Llama.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Llama.TokenTransformers
{
    internal class LetterFrequencyTransformer : ITokenTransformer
    {
        private readonly Dictionary<char, float> _standardLetterFrequency = new()
        {
            ['E'] = 11.1607f,
            ['M'] = 3.0129f,
            ['A'] = 8.4966f,
            ['H'] = 3.0034f,
            ['R'] = 7.5809f,
            ['G'] = 2.4705f,
            ['I'] = 7.5448f,
            ['B'] = 2.0720f,
            ['O'] = 7.1635f,
            ['F'] = 1.8121f,
            ['T'] = 6.9509f,
            ['Y'] = 1.7779f,
            ['N'] = 6.6544f,
            ['W'] = 1.2899f,
            ['S'] = 5.7351f,
            ['K'] = 1.1016f,
            ['L'] = 5.4893f,
            ['V'] = 1.0074f,
            ['C'] = 4.5388f,
            ['X'] = 0.2902f,
            ['U'] = 3.6308f,
            ['Z'] = 0.2722f,
            ['D'] = 3.3844f,
            ['J'] = 0.1965f,
            ['P'] = 3.1671f,
            ['Q'] = 0.1962f
        };

        public IEnumerable<LlamaToken> TransformToken(LlamaModelSettings settings, IReadOnlyLlamaTokenCollection thisGeneration, SafeLlamaContext context, IEnumerable<LlamaToken> selectedTokens)
        {
            LlamaTokenCollection toSample = new(thisGeneration);

            foreach (LlamaToken token in selectedTokens)
            {
                toSample.Append(token);

                Dictionary<char, float> foundFrequency = this.CalculateFrequency(toSample.ToString());

                StringBuilder sb = new();

                foreach (KeyValuePair<char, float> pair in foundFrequency)
                {
                    char k = pair.Key;
                    float v = pair.Value;

                    string dv = v.ToString("#.00");
                    float d = v - this._standardLetterFrequency[k];
                    string df = d.ToString("#.00");

                    if (d > 0)
                    {
                        df = "+" + df;
                    }

                    sb.Append($"[{k}: {dv} ({df})]");
                }

                string sbString = sb.ToString();

                Debug.WriteLine(sbString);
                string dt = $"[{DateTime.Now:yyyy-mm-dd HH:mm:ss.fff}]";
                System.IO.File.AppendAllText("Frequency.log", $"{dt}{toSample.ToString().Trim()}\n{dt}{sbString}\n");
                yield return token;
            }
        }

        public Dictionary<char, float> CalculateFrequency(string s)
        {
            Dictionary<char, float> toReturn = new();

            int letterCount = 0;
            foreach (KeyValuePair<char, float> existingFreq in this._standardLetterFrequency)
            {
                toReturn.Add(existingFreq.Key, 0);
            }

            char lastLetter = (char)0;

            foreach (char c in s)
            {
                char uc = char.ToUpper(c);

                if (lastLetter == uc)
                {
                    continue;
                }

                lastLetter = uc;

                if (toReturn.ContainsKey(uc))
                {
                    letterCount++;
                    toReturn[uc] = toReturn[uc] + 1;
                }
            }

            foreach (KeyValuePair<char, float> foundFreq in toReturn)
            {
                toReturn[foundFreq.Key] = foundFreq.Value / letterCount;
            }

            return toReturn;
        }
    }
}

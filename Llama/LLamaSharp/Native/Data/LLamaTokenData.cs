﻿using System.Runtime.InteropServices;

namespace Llama.Native.Data
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LlamaTokenData
    {
        /// <summary>
        /// token id
        /// </summary>
        public int id;

        /// <summary>
        /// log-odds of the token
        /// </summary>
        public float logit;

        /// <summary>
        /// probability of the token
        /// </summary>
        public float p;

        public LlamaTokenData(int id, float logit, float p)
        {
            this.id = id;
            this.logit = logit;
            this.p = p;
        }
    }
}
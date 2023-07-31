﻿using LlamaApi.Attributes;
using LlamaApi.Shared.Models.Response;

namespace LlamaApi.Models.Request
{
    public class MirostatSamplerSettings : Llama.Core.Samplers.Mirostat.MirostatSamplerSettings
    {
        [EnumNotZero]
        public MirostatType MirostatType { get; set; }
    }
}
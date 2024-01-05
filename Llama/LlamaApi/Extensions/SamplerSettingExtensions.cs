using Llama.Core.Interfaces;
using Llama.Core.Samplers.FrequencyAndPresence;
using Llama.Core.Samplers.Mirostat;
using Llama.Core.Samplers.Repetition;
using Llama.Data.Interfaces;
using LlamaApi.Shared.Models.Request;
using System.Reflection;
using System.Text.Json;

namespace LlamaApi.Extensions
{
    public static class SamplerSettingExtensions
    {
        public static T Construct<T>(this SamplerSetting samplerSetting)
        {
            foreach (ConstructorInfo ci in typeof(T).GetConstructors())
            {
                ParameterInfo[] parameters = ci.GetParameters();

                if (parameters.Length != 1)
                {
                    continue;
                }

                Type settingsType = parameters[0].ParameterType;

                object settings = JsonSerializer.Deserialize(samplerSetting.Settings, settingsType)!;

                T sampler = (T)Activator.CreateInstance(typeof(T), new object[] { settings })!;

                return sampler;
            }

            throw new NotImplementedException();
        }

        public static ITokenSelector InstantiateSelector(this SamplerSetting samplerSetting)
        {
            if (samplerSetting.IsType<DynamicTempSampler>())
            {
                return Construct<DynamicTempSampler>(samplerSetting);
            }

            throw new NotImplementedException();
        }

        public static T InstantiateSettings<T>(this SamplerSetting samplerSetting)
        {
            return JsonSerializer.Deserialize<T>(samplerSetting.Settings);
        }

        public static ISimpleSampler InstantiateSimple(this SamplerSetting samplerSetting)
        {
            if (samplerSetting.IsType<RepetitionSampler>())
            {
                return Construct<RepetitionSampler>(samplerSetting);
            }

            if (samplerSetting.IsType<ComplexPresenceSampler>())
            {
                return Construct<ComplexPresenceSampler>(samplerSetting);
            }

            throw new NotImplementedException();
        }

        public static bool IsType<T>(this SamplerSetting samplerSetting)
        {
            return string.Equals(samplerSetting.Type, typeof(T).Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
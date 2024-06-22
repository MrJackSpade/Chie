using Llama.Core.Interfaces;
using Llama.Data.Models;
using Llama.Data.Models.Settings;
using Llama.Native;

namespace Llama.Core.Samplers.Temperature
{
    public class TfsSampler : ISimpleSampler
    {
        private readonly TfsSamplerSettings _settings;

        public TfsSampler(TfsSamplerSettings temperatureSamplerSettings)
        {
            this._settings = temperatureSamplerSettings;
        }

        public void SampleNext(SampleContext sampleContext)
        {
            SamplingApi.TailFree(sampleContext.Candidates, this._settings.Tfs, 1);
        }
    }
}
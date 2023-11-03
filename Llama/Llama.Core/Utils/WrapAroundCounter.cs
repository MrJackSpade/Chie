namespace Llama.Core.Utils
{
    internal class WrapAroundCounter
    {
        public WrapAroundCounter(uint min, uint max)
        {
            Max = max;
            Min = min;
        }

        public static implicit operator uint(WrapAroundCounter c) => c.Value;
      

        public WrapAroundCounter(uint max)
        {
            Min = 0;
            Max = max;
        }

        public uint Max { get; private set; }

        public uint Min { get; private set; }

        public uint Value { get; private set; }

        public void Increment()
        {
            Value++;

            if (Value == Max)
            {
                Value = Min;
            }
        }
    }
}
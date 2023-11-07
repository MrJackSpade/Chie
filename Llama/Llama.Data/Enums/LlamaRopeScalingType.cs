namespace Llama.Data.Enums
{
    /// <summary>
    /// Represents the RoPE scaling types for a llama context.
    /// </summary>
    public enum LlamaRopeScalingType
    {
        Unspecified = -1,

        None = 0,

        Linear = 1,

        Yarn = 2,

        MaxValue = Yarn
    }
}
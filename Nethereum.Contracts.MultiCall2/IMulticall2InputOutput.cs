namespace Nethereum.Contracts.QueryHandlers.MultiCall2
{
    public interface IMulticall2InputOutput
    {
        string Target { get; set; }
        bool? Success { get; }
        byte[] GetCallData();
        void Decode(byte[] output);
        void MarkFailed();
    }
}
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Hex.HexConvertors.Extensions;
using System;

namespace Nethereum.Contracts.QueryHandlers.MultiCall2
{
    public class Multicall2InputOutput<TFunctionMessage, TFunctionOutput> : IMulticall2InputOutput
        where TFunctionMessage : FunctionMessage, new()
        where TFunctionOutput : IFunctionOutputDTO, new()
    {
        public Multicall2InputOutput(TFunctionMessage functionMessage, string contractAddressTarget)
        {
            this.Target = contractAddressTarget;
            this.Input = functionMessage;
        }

        public string Target { get; set; }
        public TFunctionMessage Input { get; set; }
        public TFunctionOutput Output { get; private set; }
        public bool? Success { get; private set; }
        public Exception Error { get; private set; } = null;
        public byte[] RawOutput { get; private set; }

        public byte[] GetCallData()
        {
            return Input.GetCallData();
        }

        public void Decode(byte[] output)
        {
            try
            {
                Output = new TFunctionOutput().DecodeOutput(output.ToHex());
                RawOutput = output;
                Success = true;
            }
            catch (Exception ex)
            {
                MarkFailed(ex);
            }
        }

        public void MarkFailed(Exception error = null)
        {
            Success = false;
            Error = error;
        }
    }
}
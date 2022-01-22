using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;

namespace Nethereum.Contracts.QueryHandlers.MultiCall2
{
    public partial class Call : CallBase { }
    public class CallBase
    {
        [Parameter("address", "target", 1)]
        public virtual string Target { get; set; }

        [Parameter("bytes", "callData", 2)]
        public virtual byte[] CallData { get; set; }
    }

    public partial class Result : ResultBase { }
    public class ResultBase
    {
        [Parameter("bool", "success", 1)]
        public virtual bool Success { get; set; }

        [Parameter("bytes", "returnData", 2)]
        public virtual byte[] Bytes { get; set; }
    }


    public partial class TryAggregateFunction : TryAggregateFunctionBase { }

    [Function("tryAggregate", typeof(TryAggregateOutputDTO))]
    public class TryAggregateFunctionBase : FunctionMessage
    {
        [Parameter("bool", "requireSuccess", 1)]
        public virtual bool RequireSuccess { get; set; } = false;

        [Parameter("tuple[]", "calls", 2)]
        public virtual List<Call> Calls { get; set; }
    }

    public partial class TryAggregateOutputDTO : TryAggregateOutputDTOBase { }
    [FunctionOutput]
    public class TryAggregateOutputDTOBase : IFunctionOutputDTO
    {
        [Parameter("tuple[]", "returnData", 1)]
        public virtual List<Result> ReturnData { get; set; }
    }

#if !DOTNET35
    /// <summary>
    /// Creates a multi query handler, to enable execute a single request combining multiple queries to multiple contracts using the Multicall2 contract https://github.com/makerdao/multicall/blob/master/src/Multicall2.sol
    /// This is deployed at https://etherscan.io/address/0x5ba1e12693dc8f9c48aad8770482f4739beed696#contracts
    /// </summary>
    /// <param name="multiContractAdress">The address of the deployed multicall contract</param>
    public class Multi2QueryHandler
    {
        public string ContractAddress { get; set; }
        private readonly QueryToDTOHandler<TryAggregateFunction, TryAggregateOutputDTO> _multiQueryToDtoHandler;
        public Multi2QueryHandler(IClient client, string multiCallContractAdress = "0x5ba1e12693dc8f9c48aad8770482f4739beed696", string defaultAddressFrom = null, BlockParameter defaultBlockParameter = null)
        {
            ContractAddress = multiCallContractAdress;
            _multiQueryToDtoHandler = new QueryToDTOHandler<TryAggregateFunction, TryAggregateOutputDTO>(client, defaultAddressFrom, defaultBlockParameter);
        }

      
        public Task<IMulticall2InputOutput[]> MultiCall2Async(
            params IMulticall2InputOutput[] multiCalls)
        {
            return MultiCall2Async(null, multiCalls);
        }

        public async Task<IMulticall2InputOutput[]> MultiCall2Async(BlockParameter block,
            params IMulticall2InputOutput[] multiCalls)
        {
            var contractCalls = new List<Call>();
            foreach (var multiCall in multiCalls)
            {
                contractCalls.Add(new Call { CallData = multiCall.GetCallData(), Target = multiCall.Target });
            }

            var aggregateFunction = new TryAggregateFunction()
            {
                Calls = contractCalls,
                RequireSuccess = false
            };
            var returnCalls = await _multiQueryToDtoHandler
                .QueryAsync(ContractAddress, aggregateFunction, block)
                .ConfigureAwait(false);

            for (var i = 0; i < returnCalls.ReturnData.Count; i++)
            {
                if (returnCalls.ReturnData[i].Success)
                    multiCalls[i].Decode(returnCalls.ReturnData[i].Bytes);
                else
                    multiCalls[i].MarkFailed();
            }

            return multiCalls;
        }
    }
#endif
}
using Nethereum.Contracts.Services;

namespace Nethereum.Contracts.QueryHandlers.MultiCall2
{
    public static class Extensions
    {
        /// <summary>
        /// Multicall using the contract https://github.com/makerdao/multicall/blob/master/src/Multicall.sol
        /// </summary>
        /// <param name="multiContractAdress">The contracts address of the deployed contract</param>
        /// <returns></returns>
        public static Multi2QueryHandler GetMulti2QueryHandler(this IEthApiContractService _service, string multiContractAdress = "0xeefBa1e63905eF1D7ACbA5a8513c70307C1cE441")
        {
            var service = _service as EthApiContractService;
            return new Multi2QueryHandler(service.Client, multiContractAdress, service.TransactionManager?.Account?.Address,
                service.DefaultBlock);
        }
    }
}

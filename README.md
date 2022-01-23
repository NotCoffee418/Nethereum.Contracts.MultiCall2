
[![Nuget](https://img.shields.io/nuget/v/NotCoffee418.Nethereum.Contracts.MultiCall2?style=for-the-badge "Nuget")](https://www.nuget.org/packages/NotCoffee418.Nethereum.Contracts.MultiCall2)
# Nethereum.Contracts.MultiCall2
Multicall2 tryAggregate() support for Nethereum

# Features
This is very similar to the official Nethereum's Multicall, however, the `TFunctionOutput` here contains a `bool? Success` property to indicate if a single call has failed (false), not been completed yet (null), or succeeded (true).  

Additionally, the whole call won't fail when a single contract call in your multicall fails.  
Instead you still get valid results for all calls which did succeed.

### Example usage
This code will use Multicall2 to get the Symbol and Decimals from a list of ERC20 tokens.  
When the ERC20 token was able to be successfully recovered, we parse it into a DexToken object.


```csharp
using Nethereum.Contracts.QueryHandlers.MultiCall2;

/// <summary>
/// Returns a list of DexToken info
/// <poolAddress>
/// </summary>
/// <returns></returns>
public async Task<List<DexToken>> BulkGetTokenInfo(List<string> tokenAddresses)
{
    // Run symbol requests
    var tokenAddressAndSymbolInput = tokenAddresses
        .Select(x => (x, new SymbolFunctionMessage()))
        .ToList();
    var symbolsResults = await RunMulticall<SymbolFunctionMessage, SymbolOutputDTO>(
        tokenAddressAndSymbolInput, 500);

    // Run decimals request
    var tokenAddressAndDecimalsInput = tokenAddresses
        .Select(x => (x, new DecimalsFunctionMessage()))
        .ToList();
    var decimalsResult = await RunMulticall<DecimalsFunctionMessage, DecimalsOutputDTO>(
        tokenAddressAndDecimalsInput, 500);

    // Generate result
    List<DexToken> result = new List<DexToken>();
    for (int i = 0; i < tokenAddresses.Count; i++)
    {
        // Item1: Input Function Message
        // Item2: Output Data
        // Item3: Is Successful bool
        if (decimalsResult[i].Item3 && symbolsResults[i].Item3) // if success
            result.Add(new DexToken()
            {
                TokenAddress = tokenAddresses[i],
                Decimals = decimalsResult[i].Item2.Decimals,
                Shortname = symbolsResults[i].Item2.Symbol
            });
    }            
    return result;
}

/// <summary>
/// Runs a multicall with one or more contracts, different functions allowed.
/// Length is irrelevant as you can use chunkSize to limit how many calls are made in one go.
/// </summary>
/// <typeparam name="TFunctionMessage">Must be FunctionMessage</typeparam>
/// <typeparam name="TFunctionOutput">Must be IFunctionOutputDTO</typeparam>
/// <param name="inputs">list of FunctionMessage</param>
/// <param name="chunkSize">how many queries to run in one call</param>
/// <returns>Returns mapped input and output for every functionmessage</returns>
public async Task<List<(TFunctionMessage, TFunctionOutput, bool)>>
    RunMulticall<TFunctionMessage, TFunctionOutput>(
    List<(string, TFunctionMessage)> addressesAndInputs, int chunkSize = 100)
    where TFunctionMessage : FunctionMessage, new()
    where TFunctionOutput : IFunctionOutputDTO, new()
{
    // You can find a Multicall2 contract address for your chain here:
    // https://github.com/makerdao/multicall
    string multiCall2ContractAddress = "0x5ba1e12693dc8f9c48aad8770482f4739beed696";

    // Endpoint for your chain
    Uri endpointUri = new Uri("https://mainnet.infura.io/v3/9aa3d95b3bc440fa88ea12eaa4456161");

    // Prep handlers
    var client = new RpcClient(endpointUri);
    var web3 = new Web3(chain);
    var multiQueryHandler = web3.Eth.GetMulti2QueryHandler(multiCall2ContractAddress);

    // map to MulticallInputOutput
    List<(TFunctionMessage, Multicall2InputOutput<TFunctionMessage, TFunctionOutput>)[]> chunks =
        addressesAndInputs.Select(x => (x.Item2, new Multicall2InputOutput<TFunctionMessage, TFunctionOutput>(x.Item2, x.Item1)))
        .ToArray()
        .Chunk(chunkSize)
        .ToList();

    // Run multicalls
    foreach (var chunkMulticallInputOutputs in chunks) 
        await multiQueryHandler.MultiCall2Async(chunkMulticallInputOutputs.Select(x => x.Item2).ToArray());

    // Extract data and return
    List<(TFunctionMessage, TFunctionOutput, bool)> result = new();
    foreach (var chunk in chunks)
        foreach (var data in chunk)
            result.Add((data.Item1, data.Item2.Output, data.Item2.Success.Value));
    return result;
}

// Function input and output defintions
[Function("symbol", "string")]
public class SymbolFunctionMessage : FunctionMessage { }

[FunctionOutput]
public class SymbolOutputDTO : IFunctionOutputDTO
{
    [Parameter("string")] public string Symbol { get; set; }
}

[Function("decimals", "uint8")]
public class DecimalsFunctionMessage : FunctionMessage { }

[FunctionOutput]
public class DecimalsOutputDTO : IFunctionOutputDTO
{
    [Parameter("uint8")] public int Decimals { get; set; }
}

// Output object
public class DexToken : IEquatable<DexToken>
{
    public string Shortname { get; set; }
    public string TokenAddress { get; set; }
    public int Decimals { get; set; }
}
```

using AElf.Boilerplate.TestBase;
using AElf.Contracts.MultiToken;
using AElf.Cryptography.ECDSA;

namespace Portkey.Contracts.Take.Tests;

public class TakeContractTestBase: DAppContractTestBase<TakeContractTestModule>
{
    internal TakeContractContainer.TakeContractStub GetFaucetContractStub(ECKeyPair senderKeyPair)
    {
        return GetTester<TakeContractContainer.TakeContractStub>(DAppContractAddress, senderKeyPair);
    }
        
    internal TokenContractContainer.TokenContractStub GetTokenContractStub(ECKeyPair senderKeyPair)
    {
        return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, senderKeyPair);
    }
}
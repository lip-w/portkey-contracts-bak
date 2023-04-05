using AElf.Boilerplate.TestBase;
using AElf.Contracts.MultiToken;
using AElf.Cryptography.ECDSA;

namespace Portkey.Contracts.TokenClaim.Tests;

public class TakeContractTestBase: DAppContractTestBase<TakeContractTestModule>
{
    internal TokenClaimContractContainer.TokenClaimContractStub GetTakeContractStub(ECKeyPair senderKeyPair)
    {
        return GetTester<TokenClaimContractContainer.TokenClaimContractStub>(DAppContractAddress, senderKeyPair);
    }
        
    internal TokenContractContainer.TokenContractStub GetTokenContractStub(ECKeyPair senderKeyPair)
    {
        return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, senderKeyPair);
    }
}
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.ContractTestBase.ContractTestKit;
using Google.Protobuf.WellKnownTypes;
using Portkey.Contracts.Take.Tests;
using Shouldly;
using Xunit;

namespace Portkey.Contracts.Take.Tests;

public class TakeContractTests : TakeContractTestBase
{
    [Fact]
    public async Task PipelineTest()
    {
        // Get a stub for testing.
        var keyPair = SampleAccount.Accounts.First().KeyPair;
        var adminStub = GetTakeContractStub(keyPair);
        var adminTokenStub = GetTokenContractStub(keyPair);
        var userStub = GetTakeContractStub(SampleAccount.Accounts.Skip(1).First().KeyPair);
        var userTokenStub = GetTokenContractStub(SampleAccount.Accounts.Skip(1).First().KeyPair);

        await adminStub.Initialize.SendAsync(new InitializeInput());

        // User failed to take.
        {
            var executionResult = await userStub.Take.SendWithExceptionAsync(new TakeInput
            {
                Symbol = "ETH",
                Amount = 100_00000000
            });
            executionResult.TransactionResult.Error.ShouldContain("Invalid symbol.");
        }

        // User takes.
        // {
        //     await userStub.Take.SendAsync(new TakeInput
        //     {
        //         Symbol = "ELF",
        //         Amount = 100_00000000
        //     });
        //     // Check user balance.
        //     var balance = (await adminTokenStub.GetBalance.CallAsync(new GetBalanceInput
        //     {
        //         Owner = SampleAccount.Accounts.Skip(1).First().Address,
        //         Symbol = "ELF"
        //     })).Balance;
        //     balance.ShouldBe(100_00000000);
        // }
    }
}
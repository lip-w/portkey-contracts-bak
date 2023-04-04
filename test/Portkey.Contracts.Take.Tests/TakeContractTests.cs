using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Portkey.Contracts.Take.Tests;
using Shouldly;
using Xunit;

namespace Portkey.Contracts.Take.Tests;

public class TakeContractTests : TakeContractTestBase
{
    private TakeContractContainer.TakeContractStub AdminStub { get; set; }
    private TakeContractContainer.TakeContractStub UserStub { get; set; }
    private TokenContractContainer.TokenContractStub AdminTokenStub { get; set; }
    private TokenContractContainer.TokenContractStub UserTokenStub { get; set; }

    public TakeContractTests()
    {
        var keyPair = SampleAccount.Accounts.First().KeyPair;
        var keyPairNext = SampleAccount.Accounts.Skip(1).First().KeyPair;
        AdminStub = GetTakeContractStub(keyPair);
        AdminTokenStub = GetTokenContractStub(keyPair);
        UserStub = GetTakeContractStub(keyPairNext);
        UserTokenStub = GetTokenContractStub(keyPairNext);
    }

    [Fact]
    public async Task Initialize_Test()
    {
        await AdminStub.Initialize.SendAsync(new InitializeInput());

        {
            var executionResult = await UserStub.Take.SendWithExceptionAsync(new TakeInput
            {
                Symbol = "ETH",
                Amount = 100_00000000
            });
            executionResult.TransactionResult.Error.ShouldContain("Invalid symbol.");
        }
    }

    [Fact]
    public async Task Take_Test()
    {
        await AdminStub.Initialize.SendAsync(new InitializeInput());

        await AdminTokenStub.Transfer.SendAsync(new TransferInput
        {
            Amount = 200_00000000,
            Memo = "hi",
            Symbol = "ELF",
            To = Address.FromBase58("2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS")
        });

        await UserStub.Take.SendAsync(new TakeInput
        {
            Symbol = "ELF",
            Amount = 100_00000000
        });
        // Check user balance.
        var balance = (await UserTokenStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = SampleAccount.Accounts.Skip(1).First().Address,
            Symbol = "ELF"
        })).Balance;
        balance.ShouldBe(100_00000000);
    }

    [Fact]
    public async Task Take_Over_Amount_Test()
    {
        await AdminStub.Initialize.SendAsync(new InitializeInput());

        await AdminTokenStub.Transfer.SendAsync(new TransferInput
        {
            Amount = 300_00000000,
            Memo = "hi",
            Symbol = "ELF",
            To = Address.FromBase58("2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS")
        });

        var executionResult = await UserStub.Take.SendWithExceptionAsync(new TakeInput
        {
            Symbol = "ELF",
            Amount = 200_00000000
        });

        executionResult.TransactionResult.Error.ShouldContain("Cannot take");
    }

    [Fact]
    public async Task Take_Twice_Test()
    {
        await AdminStub.Initialize.SendAsync(new InitializeInput());

        await AdminTokenStub.Transfer.SendAsync(new TransferInput
        {
            Amount = 300_00000000,
            Memo = "hi",
            Symbol = "ELF",
            To = Address.FromBase58("2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS")
        });

        await UserStub.Take.SendAsync(new TakeInput
        {
            Symbol = "ELF",
            Amount = 100_00000000
        });

        var executionResult = await UserStub.Take.SendWithExceptionAsync(new TakeInput
        {
            Symbol = "ELF",
            Amount = 100_00000000
        });

        executionResult.TransactionResult.Error.ShouldContain("Can take");
    }

    [Fact]
    public async Task Take_Other_Symbol_Test()
    {
        await AdminStub.Initialize.SendAsync(new InitializeInput());

        var executionResult = await UserStub.Take.SendWithExceptionAsync(new TakeInput
        {
            Symbol = "ETH",
            Amount = 100_00000000
        });
        executionResult.TransactionResult.Error.ShouldContain("Invalid symbol.");
    }
}
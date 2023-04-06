using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace Portkey.Contracts.TokenClaim.Tests;

public class TakeContractTests : TakeContractTestBase
{
    private TokenClaimContractContainer.TokenClaimContractStub AdminStub { get; set; }
    private TokenClaimContractContainer.TokenClaimContractStub UserStub { get; set; }
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
            var executionResult = await UserStub.ClaimToken.SendWithExceptionAsync(new ClaimTokenInput
            {
                Symbol = "ETH",
                Amount = 100_00000000
            });
            executionResult.TransactionResult.Error.ShouldContain("Invalid symbol.");
        }
    }

    [Fact]
    public async Task Uninitialized_Test()
    {
        var executionResult = await UserStub.ClaimToken.SendWithExceptionAsync(new ClaimTokenInput
        {
            Symbol = "ETH",
            Amount = 100_00000000
        });
        executionResult.TransactionResult.Error.ShouldContain("Uninitialized");
    }

    [Fact]
    public async Task ClaimToken_Test()
    {
        await AdminStub.Initialize.SendAsync(new InitializeInput());

        await AdminTokenStub.Transfer.SendAsync(new TransferInput
        {
            Amount = 200_00000000,
            Memo = "hi",
            Symbol = "ELF",
            To = Address.FromBase58("2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS")
        });

        await UserStub.ClaimToken.SendAsync(new ClaimTokenInput
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
    public async Task ClaimToken_Over_Amount_Test()
    {
        await AdminStub.Initialize.SendAsync(new InitializeInput());

        await AdminTokenStub.Transfer.SendAsync(new TransferInput
        {
            Amount = 300_00000000,
            Memo = "hi",
            Symbol = "ELF",
            To = Address.FromBase58("2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS")
        });

        var executionResult = await UserStub.ClaimToken.SendWithExceptionAsync(new ClaimTokenInput
        {
            Symbol = "ELF",
            Amount = 200_00000000
        });

        executionResult.TransactionResult.Error.ShouldContain("Cannot take");
    }

    [Fact]
    public async Task ClaimToken_Twice_Fail_Test()
    {
        await AdminStub.Initialize.SendAsync(new InitializeInput());

        await AdminTokenStub.Transfer.SendAsync(new TransferInput
        {
            Amount = 300_00000000,
            Memo = "hi",
            Symbol = "ELF",
            To = Address.FromBase58("2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS")
        });

        await UserStub.ClaimToken.SendAsync(new ClaimTokenInput
        {
            Symbol = "ELF",
            Amount = 100_00000000
        });

        var executionResult = await UserStub.ClaimToken.SendWithExceptionAsync(new ClaimTokenInput
        {
            Symbol = "ELF",
            Amount = 100_00000000
        });

        executionResult.TransactionResult.Error.ShouldContain("Can take");
    }

    [Fact]
    public async Task ClaimToken_Twice_Success_Test()
    {
        await AdminStub.Initialize.SendAsync(new InitializeInput { IntervalMinutes = 1 });

        await AdminTokenStub.Transfer.SendAsync(new TransferInput
        {
            Amount = 300_00000000,
            Memo = "hi",
            Symbol = "ELF",
            To = Address.FromBase58("2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS")
        });

        await UserStub.ClaimToken.SendAsync(new ClaimTokenInput
        {
            Symbol = "ELF",
            Amount = 100_00000000
        });
        
        await AdminTokenStub.Transfer.SendAsync(new TransferInput
        {
            Amount = 300_00000000,
            Memo = "hi",
            Symbol = "ELF",
            To = Address.FromBase58("2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS")
        });

        await Task.Delay(66_000);
        
        await UserStub.ClaimToken.SendAsync(new ClaimTokenInput
        {
            Symbol = "ELF",
            Amount = 100_00000000
        });

        var balance = (await UserTokenStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = SampleAccount.Accounts.Skip(1).First().Address,
            Symbol = "ELF"
        })).Balance;

        balance.ShouldBe(200_00000000);
    }

    [Fact]
    public async Task ClaimToken_Two_User_Test()
    {
        await AdminStub.Initialize.SendAsync(new InitializeInput());

        await AdminTokenStub.Transfer.SendAsync(new TransferInput
        {
            Amount = 200_00000000,
            Memo = "hi",
            Symbol = "ELF",
            To = Address.FromBase58("2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS")
        });

        await UserStub.ClaimToken.SendAsync(new ClaimTokenInput
        {
            Symbol = "ELF",
            Amount = 100_00000000
        });

        var userStub2 = GetTakeContractStub(SampleAccount.Accounts.Skip(2).First().KeyPair);

        await userStub2.ClaimToken.SendAsync(new ClaimTokenInput
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
    public async Task ClaimToken_Other_Symbol_Test()
    {
        await AdminStub.Initialize.SendAsync(new InitializeInput());

        var executionResult = await UserStub.ClaimToken.SendWithExceptionAsync(new ClaimTokenInput
        {
            Symbol = "ETH",
            Amount = 100_00000000
        });
        executionResult.TransactionResult.Error.ShouldContain("Invalid symbol.");
    }
}
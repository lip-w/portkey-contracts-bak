using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf.Collections;
using Shouldly;
using Xunit;

namespace Portkey.Contracts.CA;

public partial class CAContractTests
{
    [Fact]
    public async Task ValidateCAHolderInfoWithManagerInfosExists_Success()
    {
        await CreateHolder();
        var getHolderInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput
        {
            LoginGuardianIdentifierHash = _guardian
        });

        await CaContractStub.ValidateCAHolderInfoWithManagerInfosExists.SendAsync(
            new ValidateCAHolderInfoWithManagerInfosExistsInput
            {
                CaHash = getHolderInfo.CaHash,
                ManagerInfos = { getHolderInfo.ManagerInfos },
                LoginGuardians = { getHolderInfo.GuardianList.Guardians.Select(g => g.IdentifierHash) }
            });
    }
    
    [Fact]
    public async Task ValidateCAHolderInfoWithManagerInfosExists_Fail_InputInvalid()
    {
        await CreateHolder();
        var getHolderInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput
        {
            LoginGuardianIdentifierHash = _guardian
        });

        var param = new ValidateCAHolderInfoWithManagerInfosExistsInput
        {
            CaHash = null
        };

        var result = await CaContractStub.ValidateCAHolderInfoWithManagerInfosExists.SendWithExceptionAsync(param);
        result.TransactionResult.Error.ShouldContain("input.CaHash is null");
    }
    
    [Fact]
    public async Task ValidateCAHolderInfoWithManagerInfosExists_Fail_HolderNotExists()
    {
        await CreateHolder();

        var param = new ValidateCAHolderInfoWithManagerInfosExistsInput
        {
            CaHash = Hash.Empty
        };

        var result = await CaContractStub.ValidateCAHolderInfoWithManagerInfosExists.SendWithExceptionAsync(param);
        result.TransactionResult.Error.ShouldContain($"Holder by ca_hash: {Hash.Empty} is not found!");
    }
    
    [Fact]
    public async Task ValidateCAHolderInfoWithManagerInfosExists_Fail_LoginGuardianValidationFail()
    {
        await CreateHolder();
        
        var getHolderInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput
        {
            LoginGuardianIdentifierHash = _guardian
        });

        var loginList = getHolderInfo.GuardianList.Guardians.Select(g => g.IdentifierHash).ToList();
        loginList.Add(Hash.Empty);
        
        var param = new ValidateCAHolderInfoWithManagerInfosExistsInput
        {
            CaHash = getHolderInfo.CaHash,
            LoginGuardians = { loginList }
        };

        var result = await CaContractStub.ValidateCAHolderInfoWithManagerInfosExists.SendWithExceptionAsync(param);
        result.TransactionResult.Error.ShouldContain("The amount of LoginGuardianInput not equals to HolderInfo's LoginGuardians");
        
        param = new ValidateCAHolderInfoWithManagerInfosExistsInput
        {
            CaHash = getHolderInfo.CaHash,
            LoginGuardians = { Hash.Empty }
        };
        result = await CaContractStub.ValidateCAHolderInfoWithManagerInfosExists.SendWithExceptionAsync(param);
        result.TransactionResult.Error.ShouldContain($"LoginGuardian:{Hash.Empty} is not in HolderInfo's LoginGuardians");
    }
    
    [Fact]
    public async Task ValidateCAHolderInfoWithManagerInfosExists_Fail_ManagerValidationFail()
    {
        await CreateHolder();
        
        var getHolderInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput
        {
            LoginGuardianIdentifierHash = _guardian
        });

        var managerList = getHolderInfo.ManagerInfos;
        managerList.Add(new ManagerInfo
        {
            Address = User2Address,
            ExtraData = "123"
        });

        var param = new ValidateCAHolderInfoWithManagerInfosExistsInput
        {
            CaHash = getHolderInfo.CaHash,
            LoginGuardians = { getHolderInfo.GuardianList.Guardians.Select(g => g.IdentifierHash) },
            ManagerInfos = { managerList }
        };

        var result = await CaContractStub.ValidateCAHolderInfoWithManagerInfosExists.SendWithExceptionAsync(param);
        result.TransactionResult.Error.ShouldContain("ManagerInfos set is out of time! Please GetHolderInfo again.");

        managerList = new RepeatedField<ManagerInfo>
        {
            new ManagerInfo
            {
                Address = User1Address,
                ExtraData = "1234"
            }
        };
        param = new ValidateCAHolderInfoWithManagerInfosExistsInput
        {
            CaHash = getHolderInfo.CaHash,
            LoginGuardians = { getHolderInfo.GuardianList.Guardians.Select(g => g.IdentifierHash) },
            ManagerInfos = { managerList }
        };
        
        result = await CaContractStub.ValidateCAHolderInfoWithManagerInfosExists.SendWithExceptionAsync(param);
        result.TransactionResult.Error.ShouldContain("ManagerInfos set is out of time! Please GetHolderInfo again.");
    }
}
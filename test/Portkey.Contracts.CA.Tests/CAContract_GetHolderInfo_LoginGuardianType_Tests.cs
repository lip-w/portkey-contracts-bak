using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AElf;
using AElf.CSharp.Core;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace Portkey.Contracts.CA;

public partial class CAContractTests : CAContractTestBase
{
    [Fact]
    public async Task GetHolderInfo_ByCaHash_Test()
    {
        var caHash = await CreateHolder();

        var getHolderInfoOutput = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput
        {
            CaHash = caHash
        });
        getHolderInfoOutput.CaHash.ShouldBe(caHash);
        getHolderInfoOutput.Managers[0].ManagerAddress.ShouldBe(User1Address);
        getHolderInfoOutput.Managers[0].DeviceString.ShouldBe("123");

        var guardiansInfo = getHolderInfoOutput.GuardiansInfo;
        var guardians = guardiansInfo.GuardianAccounts;
        guardians.Count.ShouldBe(1);
        guardians[0].Guardian.Type.ShouldBe(GuardianType.OfEmail);
        guardians[0].Value.ShouldBe(GuardianAccount);

        guardiansInfo.LoginGuardianAccountIndexes.Count.ShouldBe(1);
        guardiansInfo.LoginGuardianAccountIndexes.Contains(0);
    }

    [Fact]
    public async Task GetHolderInfo_ByLoginGuardian_Test()
    {
        var caHash = await CreateHolder();
        var verifierServer = await CaContractStub.GetVerifierServers.CallAsync(new Empty());
        var id = verifierServer.VerifierServers[0].Id;

        var getHolderInfoOutput = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput
        {
            CaHash = null,
            LoginGuardianAccount = GuardianAccount
        });

        getHolderInfoOutput.CaHash.ShouldBe(caHash);
        getHolderInfoOutput.Managers[0].ManagerAddress.ShouldBe(User1Address);
        getHolderInfoOutput.Managers[0].DeviceString.ShouldBe("123");
        var guardiansInfo = getHolderInfoOutput.GuardiansInfo;
        var guardians = guardiansInfo.GuardianAccounts;

        guardians.Count.ShouldBe(1);
        guardians[0].Guardian.Type.ShouldBe(GuardianType.OfEmail);
        guardians[0].Value.ShouldBe(GuardianAccount);

        guardiansInfo.LoginGuardianAccountIndexes.Count.ShouldBe(1);
        guardiansInfo.LoginGuardianAccountIndexes.Contains(0);
    }

    [Fact]
    public async Task GetHolderInfo_ByNULL_Test()
    {
        await CreateHolder();

        var executionResult = await CaContractStub.GetHolderInfo.CallWithExceptionAsync(new GetHolderInfoInput
        {
            CaHash = null,
            LoginGuardianAccount = ""
        });

        executionResult.Value.ShouldContain("CaHash is null, or loginGuardianAccount is empty: , ");
    }

    [Fact]
    public async Task GetHolderInfo_ByInvalidCaHash_Test()
    {
        await CreateHolder();

        var executionResult = await CaContractStub.GetHolderInfo.CallWithExceptionAsync(new GetHolderInfoInput
        {
            CaHash = new Hash()
        });

        executionResult.Value.ShouldContain("Holder by ca_hash:");
    }

    [Fact]
    public async Task GetHolderInfo_ByInvalidLoginGuardianAccount_Test()
    {
        await CreateHolder();

        var verifierServer = await CaContractStub.GetVerifierServers.CallAsync(new Empty());
        var id = verifierServer.VerifierServers[0].Id;
        var executionResult = await CaContractStub.GetHolderInfo.CallWithExceptionAsync(new GetHolderInfoInput
        {
            CaHash = null,
            LoginGuardianAccount = "Invalid"
        });

        executionResult.Value.ShouldContain("Not found ca_hash by a the loginGuardianAccount Invalid");
    }

    [Fact]
    public async Task SetLoginGuardianAccount_Succeed_Test()
    {
        var caHash = await CreateCAHolder_AndGetCaHash_Helper();

        var getHolderInfoOutput = await CaContractStub.GetHolderInfo.SendAsync(new GetHolderInfoInput
        {
            CaHash = caHash
        });

        var guardiansInfo = getHolderInfoOutput.Output.GuardiansInfo;
        var guardians = guardiansInfo.GuardianAccounts;
        guardians.Count.ShouldBe(2);

        guardiansInfo.LoginGuardianAccountIndexes.Count.ShouldBe(1);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldContain(0);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldNotContain(1);

        getHolderInfoOutput = await SetGuardianAccountForLogin_AndGetHolderInfo_Helper(caHash, null);

        guardiansInfo = getHolderInfoOutput.Output.GuardiansInfo;

        guardiansInfo.LoginGuardianAccountIndexes.Count.ShouldBe(2);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldContain(0);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldContain(1);
        
        var verifierServer = await CaContractStub.GetVerifierServers.CallAsync(new Empty());
        var id = verifierServer.VerifierServers[0].Id;
        var id2 = verifierServer.VerifierServers[1].Id;

        // check loginGuardianType -> caHash mapping
        getHolderInfoOutput = await GetHolderInfo_Helper(null, GuardianAccount, id);
        guardiansInfo = getHolderInfoOutput.Output.GuardiansInfo;
        guardiansInfo.ShouldNotBeNull();

        getHolderInfoOutput = await GetHolderInfo_Helper(null, GuardianAccount1, id2);
        guardiansInfo = getHolderInfoOutput.Output.GuardiansInfo;
        guardiansInfo.ShouldNotBeNull();

    }

    [Fact]
    public async Task SetLoginGuardianAccount_Again_Succeed_Test()
    {
        var caHash = await CreateCAHolder_AndGetCaHash_Helper();

        var getHolderInfoOutput = await CaContractStub.GetHolderInfo.SendAsync(new GetHolderInfoInput
        {
            CaHash = caHash
        });

        var guardiansInfo = getHolderInfoOutput.Output.GuardiansInfo;
        var guardians = guardiansInfo.GuardianAccounts;
        guardians.Count.ShouldBe(2);

        guardiansInfo.LoginGuardianAccountIndexes.Count.ShouldBe(1);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldContain(0);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldNotContain(1);

        getHolderInfoOutput = await SetGuardianAccountForLogin_AndGetHolderInfo_Helper(caHash, null);

        guardiansInfo = getHolderInfoOutput.Output.GuardiansInfo;

        guardiansInfo.LoginGuardianAccountIndexes.Count.ShouldBe(2);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldContain(0);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldContain(1);

        getHolderInfoOutput = await SetGuardianAccountForLogin_AndGetHolderInfo_Helper(caHash, null);

        guardiansInfo = getHolderInfoOutput.Output.GuardiansInfo;

        guardiansInfo.LoginGuardianAccountIndexes.Count.ShouldBe(2);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldContain(0);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldContain(1);
    }

    [Fact]
    public async Task SetLoginGuardianAccount_CashNull_Test()
    {
        var caHash = await CreateCAHolder_AndGetCaHash_Helper();


        var executionResult = await CaContractStub.SetGuardianAccountForLogin.SendWithExceptionAsync(
            new SetGuardianAccountForLoginInput
            {
                CaHash = null,
                GuardianAccount = new GuardianAccount
                {
                    Guardian = new Guardian
                    {
                        Type = GuardianType.OfEmail,
                        Verifier = new Verifier()
                    },
                    Value = GuardianAccount1
                }
            });

        executionResult.TransactionResult.Error.ShouldNotBeNull();

    }

    [Fact]
    public async Task SetLoginGuardianAccount_CashNotExits_Test()
    {
        var caHash = await CreateCAHolder_AndGetCaHash_Helper();
        var executionResult = await CaContractStub.SetGuardianAccountForLogin.SendWithExceptionAsync(
            new SetGuardianAccountForLoginInput()
            {
                CaHash = HashHelper.ComputeFrom("123"),
                GuardianAccount = new GuardianAccount
                {
                    Guardian = new Guardian
                    {
                        Type = GuardianType.OfEmail,
                        Verifier = new Verifier()
                    },
                    Value = GuardianAccount1
                }
            });

        executionResult.TransactionResult.Error.ShouldContain("CA Holder is null");

    }

    [Fact]
    public async Task SetLoginGuardianAccount_GuardianTypeNull_Test()
    {
        var caHash = await CreateCAHolder_AndGetCaHash_Helper();
        var executionResult = await CaContractStub.SetGuardianAccountForLogin.SendWithExceptionAsync(
            new SetGuardianAccountForLoginInput()
            {
                CaHash = caHash,
                GuardianAccount = null
            });

        executionResult.TransactionResult.Error.ShouldNotBeNull();

    }

    [Fact]
    public async Task SetLoginGuardianAccount_GuardianTypeEmpty_Test()
    {
        var caHash = await CreateCAHolder_AndGetCaHash_Helper();


        var executionResult = await CaContractStub.SetGuardianAccountForLogin.SendWithExceptionAsync(
            new SetGuardianAccountForLoginInput()
            {
                CaHash = caHash,
                GuardianAccount = new GuardianAccount
                {
                    Guardian = new Guardian
                    {
                        Type = GuardianType.OfEmail,
                        Verifier = new Verifier()
                    },
                    Value = ""
                }
            });

        executionResult.TransactionResult.Error.ShouldContain("Guardian account should not be null");

    }

    [Fact]
    public async Task SetLoginGuardianAccount_GuardianTypeNotExists_Test()
    {
        var caHash = await CreateCAHolder_AndGetCaHash_Helper();

        var guardianAccount = new GuardianAccount
        {
            Guardian = new Guardian
            {
                Type = GuardianType.OfEmail,
                Verifier = new Verifier
                {
                    Id = verifierId
                }
            },
            Value = GuardianAccountNotExist
        };

        var getHolderInfoOutput = await SetGuardianAccountForLogin_AndGetHolderInfo_Helper(caHash, guardianAccount);

        var guardiansInfo = getHolderInfoOutput.Output.GuardiansInfo;

        guardiansInfo.LoginGuardianAccountIndexes.Count.ShouldBe(1);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldContain(0);
        guardiansInfo.GuardianAccounts.Count.ShouldBe(2);
        guardiansInfo.GuardianAccounts[0].Value.ShouldNotContain(GuardianAccountNotExist);



    }

    [Fact]
    public async Task SetLoginGuardianAccount_DuplicatedGuardianType_Test()
    {
        var caHash = await CreateCAHolder_AndGetCaHash_Helper();

        var guardianAccount = new GuardianAccount
        {
            Guardian = new Guardian
            {
                Type = GuardianType.OfEmail,
                Verifier = new Verifier
                {
                    Id = verifierId
                }
            },
            Value = GuardianAccountNotExist
        };

        var getHolderInfoOutput = await SetGuardianAccountForLogin_AndGetHolderInfo_Helper(caHash, guardianAccount);

        var guardiansInfo = getHolderInfoOutput.Output.GuardiansInfo;

        guardiansInfo.LoginGuardianAccountIndexes.Count.ShouldBe(1);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldContain(0);

    }

    /*[Fact]
public async Task SetLoginGuardianAccount_RegisterByOthers()
{
    var caHash = await CreateAHolder_AndGetCash_Helper();
   
    var getHolderInfoOutput = await CaContractStub.GetHolderInfo.SendAsync(new GetHolderInfoInput
    {
        CaHash = caHash,
        LoginGuardianAccount = ""
    });
   
    var guardiansInfo = getHolderInfoOutput.Output.GuardiansInfo;
    var guardians = guardiansInfo.Guardians;
    guardians.Count.ShouldBe(2);
   
    guardiansInfo.LoginGuardianAccountIndexes.Count.ShouldBe(1);
    guardiansInfo.LoginGuardianAccountIndexes.ShouldContain(0);
    guardiansInfo.LoginGuardianAccountIndexes.ShouldNotContain(1);
    var guardianType = new GuardianType
    {
        Type = GuardianTypeType.GuardianTypeOfEmail,
        GuardianType_ = GuardianType1
    };
    getHolderInfoOutput = await SetGuardianAccountForLogin_AndGetHolderInfo_Helper(caHash, guardianType);
    var verificationTime = DateTime.UtcNow;
    var signature1 = await GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianType, 0);
    await CaContractStub.CreateCAHolder.SendAsync(new CreateCAHolderInput
    {
        GuardianApproved = new Guardian
        {
            GuardianType = guardianType,
            Verifier = new Verifier
            {
                Name = VerifierName,
                Signature = signature1,
                VerificationDoc = $"{0},{GuardianType},{verificationTime},{VerifierAddress.ToBase58()}"
            }
        },
        Manager = new Manager
        {
            ManagerAddress = User1Address,
            DeviceString = "123"
        }
    });
    var caHolderInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput()
    {
        LoginGuardianAccount = GuardianType
    });
    await SetGuardianAccountForLogin_AndGetHolderInfo_Helper(caHolderInfo.CaHash, guardianType);
    caHolderInfo.Managers.Count.ShouldBe(2);
    caHolderInfo.GuardiansInfo.Guardians.Count.ShouldBe(2);

}*/




    [Fact]
    public async Task UnsetLoginGuardianAccount_Succeed_Test()
    {
        var caHash = await CreateCAHolder_AndGetCaHash_Helper();

        var getHolderInfoOutput = await SetGuardianAccountForLogin_AndGetHolderInfo_Helper(caHash, null);

        var guardiansInfo = getHolderInfoOutput.Output.GuardiansInfo;

        guardiansInfo.LoginGuardianAccountIndexes.Count.ShouldBe(2);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldContain(0);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldContain(1);

        getHolderInfoOutput = await UnsetGuardianAccountForLogin_AndGetHolderInfo_Helper(caHash, null);

        guardiansInfo = getHolderInfoOutput.Output.GuardiansInfo;

        guardiansInfo.LoginGuardianAccountIndexes.Count.ShouldBe(1);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldContain(0);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldNotContain(1);
        
        // check loginGuardianType mapping is removed.
        var executionResult = await CaContractStub.GetHolderInfo.CallWithExceptionAsync(new GetHolderInfoInput
        {
            CaHash = null,
            LoginGuardianAccount = GuardianAccount1
        });

        executionResult.Value.ShouldContain("Not found ca_hash by a the loginGuardianAccount");
    }


    [Fact]
    public async Task UnsetLoginGuardianAccount_GuardianTypeNotIn_Test()
    {
        var caHash = await CreateCAHolder_AndGetCaHash_Helper();

        var getHolderInfoOutput = await SetGuardianAccountForLogin_AndGetHolderInfo_Helper(caHash, null);

        var guardiansInfo = getHolderInfoOutput.Output.GuardiansInfo;

        guardiansInfo.LoginGuardianAccountIndexes.Count.ShouldBe(2);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldContain(0);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldContain(1);

        await CaContractStub.UnsetGuardianAccountForLogin.SendAsync(new UnsetGuardianAccountForLoginInput
        {
            CaHash = caHash,
            GuardianAccount = new GuardianAccount
            {
                Guardian = new Guardian
                {
                    Type = GuardianType.OfEmail,
                    Verifier = new Verifier()
                },
                Value = GuardianAccountNotExist
            }
        });

        guardiansInfo = getHolderInfoOutput.Output.GuardiansInfo;
        guardiansInfo.LoginGuardianAccountIndexes.Count.ShouldBe(2);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldContain(0);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldContain(1);


    }


    [Fact]
    public async Task UnsetLoginGuardianAccount_Again_Succeed_Test()
    {
        var caHash = await CreateCAHolder_AndGetCaHash_Helper();

        var getHolderInfoOutput = await SetGuardianAccountForLogin_AndGetHolderInfo_Helper(caHash, null);

        var guardiansInfo = getHolderInfoOutput.Output.GuardiansInfo;

        guardiansInfo.LoginGuardianAccountIndexes.Count.ShouldBe(2);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldContain(0);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldContain(1);

        getHolderInfoOutput = await UnsetGuardianAccountForLogin_AndGetHolderInfo_Helper(caHash, null);

        guardiansInfo = getHolderInfoOutput.Output.GuardiansInfo;

        guardiansInfo.LoginGuardianAccountIndexes.Count.ShouldBe(1);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldContain(0);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldNotContain(1);



        var result = await CaContractStub.UnsetGuardianAccountForLogin.SendWithExceptionAsync(
            new UnsetGuardianAccountForLoginInput
            {
                CaHash = caHash,
                GuardianAccount = new GuardianAccount
                {
                    Guardian = new Guardian
                    {
                        Type = GuardianType.OfEmail,
                        Verifier = new Verifier()
                    },
                    Value = GuardianAccount1
                }
            });
        result.TransactionResult.Error.ShouldContain("only one LoginGuardian,can not be Unset");

    }

    [Fact]
    public async Task UnsetLoginGuardianAccount_CashNull_Test()
    {
        var caHash = await CreateCAHolder_AndGetCaHash_Helper();
        var executionResult = await CaContractStub.UnsetGuardianAccountForLogin.SendWithExceptionAsync(
            new UnsetGuardianAccountForLoginInput
            {
                CaHash = null,
                GuardianAccount = new GuardianAccount
                {
                    Guardian = new Guardian
                    {
                        Type = GuardianType.OfEmail,
                        Verifier = new Verifier()
                    },
                    Value = GuardianAccount1
                }
            });

        executionResult.TransactionResult.Error.ShouldNotBeNull();

        var reslut = await CaContractStub.UnsetGuardianAccountForLogin.SendWithExceptionAsync(
            new UnsetGuardianAccountForLoginInput
            {
                CaHash = HashHelper.ComputeFrom("123"),
                GuardianAccount = new GuardianAccount
                {
                    Guardian = new Guardian
                    {
                        Type = GuardianType.OfEmail,
                        Verifier = new Verifier()
                    },
                    Value = GuardianAccount1
                }
            });

        reslut.TransactionResult.Error.ShouldContain("CA holder is null");

    }

    [Fact]
    public async Task UnsetLoginGuardianAccount_GuardianTypeNull_Test()
    {
        var caHash = await CreateCAHolder_AndGetCaHash_Helper();


        var executionResult = await CaContractStub.UnsetGuardianAccountForLogin.SendWithExceptionAsync(
            new UnsetGuardianAccountForLoginInput
            {
                CaHash = caHash,
                GuardianAccount = null
            });

        executionResult.TransactionResult.Error.ShouldNotBeNull();

    }


    [Fact]
    public async Task UnsetLoginGuardianAccount_FailedUniqueLoginguardianType_Test()
    {
        var caHash = await CreateCAHolder_AndGetCaHash_Helper();

        var getHolderInfoOutput = await SetGuardianAccountForLogin_AndGetHolderInfo_Helper(caHash, null);

        var guardiansInfo = getHolderInfoOutput.Output.GuardiansInfo;

        guardiansInfo.LoginGuardianAccountIndexes.Count.ShouldBe(2);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldContain(0);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldContain(1);

        getHolderInfoOutput = await UnsetGuardianAccountForLogin_AndGetHolderInfo_Helper(caHash, null);

        guardiansInfo = getHolderInfoOutput.Output.GuardiansInfo;

        guardiansInfo.LoginGuardianAccountIndexes.Count.ShouldBe(1);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldContain(0);
        guardiansInfo.LoginGuardianAccountIndexes.ShouldNotContain(1);

        var result = await CaContractStub.UnsetGuardianAccountForLogin.SendWithExceptionAsync(
            new UnsetGuardianAccountForLoginInput
            {
                CaHash = caHash,
                GuardianAccount = new GuardianAccount
                {
                    Value = GuardianAccount,
                    Guardian = new Guardian
                    {
                        Type = GuardianType.OfEmail,
                        Verifier = new Verifier
                        {
                            Id = verifierId
                        }
                    }
                }
            });
        result.TransactionResult.Error.ShouldContain("only one LoginGuardian,can not be Unset");
    }

    [Fact]
    public async Task UnsetLoginGuardianAccount_Unique_Test()
    {
        var caHash = await CreateCAHolder_AndGetCaHash_Helper();
        var executionResult = await CaContractStub.UnsetGuardianAccountForLogin.SendWithExceptionAsync(
            new UnsetGuardianAccountForLoginInput
            {
                CaHash = caHash,
                GuardianAccount = new GuardianAccount
                {
                    Value = "",
                    Guardian = new Guardian
                    {
                        Type = GuardianType.OfEmail
                    }
                }
            });

        executionResult.TransactionResult.Error.ShouldNotBeNull();

    }

    [Fact]
    public async Task UnsetLoginGuardianAccount_GuardianTypeEmpty_Test()
    {
        var caHash = await CreateCAHolder_AndGetCaHash_Helper();
        var executionResult = await CaContractStub.UnsetGuardianAccountForLogin.SendWithExceptionAsync(
            new UnsetGuardianAccountForLoginInput
            {
                CaHash = caHash,
                GuardianAccount = new GuardianAccount
                {
                    Value = "",
                    Guardian = new Guardian
                    {
                        Type = GuardianType.OfEmail
                    }
                }
            });

        executionResult.TransactionResult.Error.ShouldNotBeNull();

    }

    [Fact]
    public async Task UnsetLoginGuardianAccount_GuardianTypeNotExitsTest()
    {
        var caHash = await CreateCAHolder_AndGetCaHash_Helper();
        await CaContractStub.SetGuardianAccountForLogin.SendAsync(new SetGuardianAccountForLoginInput
        {
            CaHash = caHash,
            GuardianAccount = new GuardianAccount
            {
                Value = GuardianAccount1,
                Guardian = new Guardian
                {
                    Type = GuardianType.OfEmail,
                    Verifier = new Verifier
                    {
                        Id = verifierId1
                    }
                }
            }
        });
        var result = await CaContractStub.UnsetGuardianAccountForLogin.SendAsync(
            new UnsetGuardianAccountForLoginInput
            {
                CaHash = caHash,
                GuardianAccount = new GuardianAccount
                {
                    Value = "1111@gmail.com",
                    Guardian = new Guardian
                    {
                        Type = GuardianType.OfEmail,
                        Verifier = new Verifier
                        {
                            Id = verifierId2
                        }
                    }
                }
            });
        var getHolderInfoOutput = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput
        {
            CaHash = caHash
        });
        getHolderInfoOutput.GuardiansInfo.GuardianAccounts.Count.ShouldBe(2);

    }


    private async Task<IExecutionResult<GetHolderInfoOutput>> SetGuardianAccountForLogin_AndGetHolderInfo_Helper(
        Hash caHash, GuardianAccount guardianAccount)
    {
        await CaContractStub.SetGuardianAccountForLogin.SendAsync(new SetGuardianAccountForLoginInput
        {
            CaHash = caHash,
            GuardianAccount = guardianAccount ?? new GuardianAccount
            {
                Guardian = new Guardian
                {
                    Type = GuardianType.OfEmail,
                    Verifier = new Verifier
                    {
                        Id = verifierId1
                    }
                },
                Value = GuardianAccount1
            }
        });

        return await GetHolderInfo_Helper(caHash, "", new Hash());
    }

    private async Task<IExecutionResult<GetHolderInfoOutput>> GetHolderInfo_Helper(Hash caHash,
        string loginGuardianType, Hash verifierId)
    {
        var getHolderInfoOutput = await CaContractStub.GetHolderInfo.SendAsync(new GetHolderInfoInput
        {
            CaHash = caHash,
            LoginGuardianAccount = loginGuardianType
        });

        return getHolderInfoOutput;
    }

    private async Task<IExecutionResult<GetHolderInfoOutput>> UnsetGuardianAccountForLogin_AndGetHolderInfo_Helper(
        Hash caHash, GuardianAccount guardianAccount)
    {
        await CaContractStub.UnsetGuardianAccountForLogin.SendAsync(new UnsetGuardianAccountForLoginInput
        {
            CaHash = caHash,
            GuardianAccount = guardianAccount ?? new GuardianAccount
            {
                Guardian = new Guardian
                {
                    Type = GuardianType.OfEmail,
                    Verifier = new Verifier
                    {
                        Id = verifierId1
                    }
                },
                Value = GuardianAccount1
            }
        });

        var getHolderInfoOutput = await CaContractStub.GetHolderInfo.SendAsync(new GetHolderInfoInput
        {
            CaHash = caHash
        });

        return getHolderInfoOutput;
    }


    private async Task<Hash> CreateCAHolder_AndGetCaHash_Helper()
    {
        var caHash = await CreateHolder();

        await AddAGuardian_Helper(caHash);

        return caHash;
    }

    private async Task AddAGuardian_Helper(Hash caHash)
    {
        var verificationTime = DateTime.UtcNow;
        var signature = await GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);
        var signature1 =
            await GenerateSignature(VerifierKeyPair1, VerifierAddress1, verificationTime, GuardianAccount1, 0);
        var guardianApprove = new List<GuardianAccountInfo>
        {
            new ()
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = verifierId,
                    Signature = signature,
                    VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress.ToBase58()}"
                }
            }
        };
        var input = new AddGuardianInput
        {
            CaHash = caHash,
            GuardianToAdd = new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount1,
                VerificationInfo = new VerificationInfo
                {
                    Id = verifierId1,
                    Signature = signature1,
                    VerificationDoc = $"{0},{GuardianAccount1},{verificationTime},{VerifierAddress1.ToBase58()}"
                }
            },
            GuardiansApproved = {guardianApprove}
        };
        await CaContractStub.AddGuardian.SendAsync(input);
        {
            var holderInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput
            {
                CaHash = caHash
            });
            holderInfo.GuardiansInfo.GuardianAccounts.Last().Value.ShouldBe(GuardianAccount1);
        }
    }

}
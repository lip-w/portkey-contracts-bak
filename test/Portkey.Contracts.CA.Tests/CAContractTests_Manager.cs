using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace Portkey.Contracts.CA;

public partial class CAContractTests : CAContractTestBase
{
    private async Task CreateHolderDefault()
    {
        var verificationTime = DateTime.UtcNow;
        await CaContractStub.Initialize.SendAsync(new InitializeInput
        {
            ContractAdmin = DefaultAddress
        });
        {
            await CaContractStub.AddVerifierServerEndPoints.SendAsync(new AddVerifierServerEndPointsInput
            {
                Name = VerifierName,
                ImageUrl = "url",
                EndPoints = { "127.0.0.1" },
                VerifierAddressList = { VerifierAddress }
            });
            await CaContractStub.AddVerifierServerEndPoints.SendAsync(new AddVerifierServerEndPointsInput
            {
                Name = VerifierName1,
                ImageUrl = "url",
                EndPoints = { "127.0.0.1" },
                VerifierAddressList = { VerifierAddress1 }
            });
            await CaContractStub.AddVerifierServerEndPoints.SendAsync(new AddVerifierServerEndPointsInput
            {
                Name = VerifierName2,
                ImageUrl = "url",
                EndPoints = { "127.0.0.1" },
                VerifierAddressList = { VerifierAddress2 }
            });
        }
        var signature = await GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);
        var verifierServer = await CaContractStub.GetVerifierServers.CallAsync(new Empty());
        var id = verifierServer.VerifierServers[0].Id;
        {
            var verifierServers = await CaContractStub.GetVerifierServers.CallAsync(new Empty());
            verifierId = verifierServers.VerifierServers[0].Id;
            verifierId1 = verifierServers.VerifierServers[1].Id;
            verifierId2 = verifierServers.VerifierServers[2].Id;
        }
        await CaContractStub.CreateCAHolder.SendAsync(new CreateCAHolderInput
        {
            GuardianApproved = new GuardianAccountInfo
            {
                Value = GuardianAccount,
                Type = GuardianType.OfEmail,
                VerificationInfo = new VerificationInfo
                {
                    Id = id,
                    Signature = signature,
                    VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress.ToBase58()}"
                }
            },
            Manager = new Manager
            {
                ManagerAddress = User1Address,
                DeviceString = "123"
            }
        });
        var holderInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput
        {
            LoginGuardianAccount = GuardianAccount
        });
        await TokenContractStub.Transfer.SendAsync(new TransferInput
        {
            Amount = 1000000000000,
            Symbol = "ELF",
            To = holderInfo.CaAddress
        });
    }

    [Fact]
    public async Task SocialRecoveryTest()
    {
        await CreateHolderDefault();
        var verificationTime = DateTime.UtcNow;
        var signature = await GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);
        var verifierServer = await CaContractStub.GetVerifierServers.CallAsync(new Empty());
        var id = verifierServer.VerifierServers[0].Id;
        var guardianApprove = new List<GuardianAccountInfo>
        {
            new()
            {
                Value = GuardianAccount,
                Type = GuardianType.OfEmail,
                VerificationInfo = new VerificationInfo
                {
                    Id = id,
                    Signature = signature,
                    VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress.ToBase58()}"
                }
            }
        };

        await CaContractStub.SocialRecovery.SendAsync(new SocialRecoveryInput
        {
            Manager = new Manager
            {
                ManagerAddress = User2Address,
                DeviceString = "567"
            },
            LoginGuardianAccount = GuardianAccount,
            GuardiansApproved = { guardianApprove }
        });

        var caInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput
        {
            LoginGuardianAccount = GuardianAccount
        });
        caInfo.Managers.Count.ShouldBe(2);
        caInfo.GuardiansInfo.GuardianAccounts.Count.ShouldBe(1);

        var delegateAllowance = await TokenContractStub.GetTransactionFeeDelegationsOfADelegatee.CallAsync(
            new GetTransactionFeeDelegationsOfADelegateeInput()
            {
                DelegateeAddress = caInfo.CaAddress,
                DelegatorAddress = User2Address
            });
        delegateAllowance.Delegations["ELF"].ShouldBe(10000000000000000L);
    }
    
    [Fact]
    public async Task SocialRecoveryTest_Delegator()
    {
        await SocialRecoveryTest();
         
        var delegations = await TokenContractStub.GetTransactionFeeDelegationsOfADelegatee.CallAsync(new GetTransactionFeeDelegationsOfADelegateeInput
        {
            DelegateeAddress = CaContractAddress,
            DelegatorAddress = User2Address
        });
         
        delegations.Delegations["ELF"].ShouldBe(100);
    }

    [Fact]
    public async Task SocialRecovery_StrategyTest()
    {
        var hash = await AddGuardianTest();
        var holderInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput
        {
            CaHash = hash
        });

        var verificationTime = DateTime.UtcNow;
        var signature = await GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);
        var verifierServer = await CaContractStub.GetVerifierServers.CallAsync(new Empty());
        var id = verifierServer.VerifierServers[0].Id;
        var guardianApprove = new List<GuardianAccountInfo>
        {
            new()
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = id,
                    Signature = signature,
                    VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress.ToBase58()}"
                }
            }
        };

        var result = await CaContractStub.SocialRecovery.SendWithExceptionAsync(new SocialRecoveryInput
        {
            Manager = new Manager
            {
                ManagerAddress = User2Address,
                DeviceString = "567"
            },
            LoginGuardianAccount = GuardianAccount,
            GuardiansApproved = { guardianApprove }
        });
        result.TransactionResult.Error.ShouldContain("Not Satisfied criterion to create a CA Holder");
    }

    [Fact]
    public async Task SocialRecovery_VerifierServerTest()
    {
        await CreateHolderDefault();
        var verifierServer = await CaContractStub.GetVerifierServers.CallAsync(new Empty());
        var id = verifierServer.VerifierServers[0].Id;
        var input = new RemoveVerifierServerInput
        {
            Id = id
        };
        await CaContractStub.RemoveVerifierServer.SendAsync(input);

        var verificationTime = DateTime.UtcNow;
        var signature = await GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);

        var guardianApprove = new List<GuardianAccountInfo>
        {
            new()
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = id,
                    Signature = signature,
                    VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress.ToBase58()}"
                }
            }
        };

        var result = await CaContractStub.SocialRecovery.SendWithExceptionAsync(new SocialRecoveryInput()
        {
            Manager = new Manager
            {
                ManagerAddress = User2Address,
                DeviceString = "567"
            },
            LoginGuardianAccount = GuardianAccount,
            GuardiansApproved = { guardianApprove }
        });
        result.TransactionResult.Error.ShouldContain("Not Satisfied criterion to create a CA Holder");
    }

    [Fact]
    public async Task SocialRecovery_InvalidateDocTest()
    {
        await CreateHolderDefault();
        var verificationTime = DateTime.UtcNow;
        var signature = await GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);
        var verifierServer = await CaContractStub.GetVerifierServers.CallAsync(new Empty());
        var id = verifierServer.VerifierServers[0].Id;
        var guardianApprove = new List<GuardianAccountInfo>
        {
            new()
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = id,
                    Signature = signature,
                    VerificationDoc = $"{0},{GuardianAccount},{VerifierAddress.ToBase58()}"
                }
            }
        };

        var result = await CaContractStub.SocialRecovery.SendWithExceptionAsync(new SocialRecoveryInput()
        {
            Manager = new Manager
            {
                ManagerAddress = User2Address,
                DeviceString = "567"
            },
            LoginGuardianAccount = GuardianAccount,
            GuardiansApproved = { guardianApprove }
        });
        result.TransactionResult.Error.ShouldContain("Not Satisfied criterion to create a CA Holder");

        var guardianApprove1 = new List<GuardianAccountInfo>
        {
            new()
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = id,
                    Signature = signature
                }
            }
        };

        var executionResult = await CaContractStub.SocialRecovery.SendWithExceptionAsync(new SocialRecoveryInput()
        {
            Manager = new Manager
            {
                ManagerAddress = User2Address,
                DeviceString = "567"
            },
            LoginGuardianAccount = GuardianAccount,
            GuardiansApproved = { guardianApprove1 }
        });
        executionResult.TransactionResult.Error.ShouldContain("Not Satisfied criterion to create a CA Holder");
    }

    [Fact]
    public async Task SocialRecovery_Address_Exists()
    {
        await CreateHolderDefault();
        var verificationTime = DateTime.UtcNow;
        var signature = await GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);
        var verifierServer = await CaContractStub.GetVerifierServers.CallAsync(new Empty());
        var id = verifierServer.VerifierServers[0].Id;
        var guardianApprove = new List<GuardianAccountInfo>
        {
            new()
            {
                Value = GuardianAccount,
                Type = GuardianType.OfEmail,
                VerificationInfo = new VerificationInfo
                {
                    Id = id,
                    Signature = signature,
                    VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress.ToBase58()}"
                }
            }
        };

        var result = await CaContractStub.SocialRecovery.SendWithExceptionAsync(new SocialRecoveryInput
        {
            Manager = new Manager
            {
                ManagerAddress = User1Address,
                DeviceString = "567"
            },
            LoginGuardianAccount = GuardianAccount,
            GuardiansApproved = { guardianApprove }
        });
        result.TransactionResult.Error.ShouldContain("Manager address exists");
    }
    
    [Fact]
    public async Task SocialRecovery_FailedTest()
    {
        await CreateHolderDefault();
        var expiredVerificationTime = DateTime.UtcNow.AddHours(-10);
        var verificationTime = DateTime.UtcNow;
        var signature =
            await GenerateSignature(VerifierKeyPair, VerifierAddress, expiredVerificationTime, GuardianAccount, 0);
        var signature1 =
            await GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);
        var verifierServer = await CaContractStub.GetVerifierServers.CallAsync(new Empty());
        var id = verifierServer.VerifierServers[0].Id;
        var id2 = verifierServer.VerifierServers[1].Id;
        // Verifier signature has expired.
        var guardianApprove = new List<GuardianAccountInfo>
        {
            new()
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = id,
                    Signature = signature,
                    VerificationDoc = $"{0},{GuardianAccount},{expiredVerificationTime},{VerifierAddress.ToBase58()}"
                }
            }
        };

        var result = await CaContractStub.SocialRecovery.SendWithExceptionAsync(new SocialRecoveryInput()
        {
            Manager = new Manager
            {
                ManagerAddress = User2Address,
                DeviceString = "567"
            },
            LoginGuardianAccount = GuardianAccount,
            GuardiansApproved = { guardianApprove }
        });
        result.TransactionResult.Error.ShouldContain("Not Satisfied criterion to create a CA Holder");

        //VerificationDoc parse failed. Invalid guardian type name.
        var guardianApprove1 = new List<GuardianAccountInfo>
        {
            new()
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo()
                {
                    Id = id,
                    Signature = signature1,
                    VerificationDoc = $"{"abc"},{GuardianAccount},{verificationTime},{VerifierAddress.ToBase58()}"
                }
            }
        };

        var executionResult = await CaContractStub.SocialRecovery.SendWithExceptionAsync(new SocialRecoveryInput()
        {
            Manager = new Manager
            {
                ManagerAddress = User2Address,
                DeviceString = "567"
            },
            LoginGuardianAccount = GuardianAccount,
            GuardiansApproved = { guardianApprove1 }
        });
        executionResult.TransactionResult.Error.ShouldContain("Not Satisfied criterion to create a CA Holder");

        //Invalid guardian type.
        var guardianApprove2 = new List<GuardianAccountInfo>
        {
            new()
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = id,
                    Signature = signature1,
                    VerificationDoc = $"{0},{GuardianAccountNotExist},{verificationTime},{VerifierAddress.ToBase58()}"
                }
            }
        };

        var exeRsult = await CaContractStub.SocialRecovery.SendWithExceptionAsync(new SocialRecoveryInput()
        {
            Manager = new Manager
            {
                ManagerAddress = User2Address,
                DeviceString = "567"
            },
            LoginGuardianAccount = GuardianAccount,
            GuardiansApproved = { guardianApprove2 }
        });
        exeRsult.TransactionResult.Error.ShouldContain("Not Satisfied criterion to create a CA Holder");

        var guardianApprove3 = new List<GuardianAccountInfo>
        {
            new()
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = id,
                    Signature = signature,
                    VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress4.ToBase58()}"
                }
            }
        };

        var eresult = await CaContractStub.SocialRecovery.SendWithExceptionAsync(new SocialRecoveryInput()
        {
            Manager = new Manager
            {
                ManagerAddress = User2Address,
                DeviceString = "567"
            },
            LoginGuardianAccount = GuardianAccount,
            GuardiansApproved = { guardianApprove3 }
        });
        eresult.TransactionResult.Error.ShouldContain("Not Satisfied criterion to create a CA Holder：guardianCount");

        var guardianApprove4 = new List<GuardianAccountInfo>
        {
            new()
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = id2,
                    Signature = signature,
                    VerificationDoc = $"{0},{GuardianAccount},{VerifierAddress.ToBase58()}"
                }
            }
        };

        var inputResult = await CaContractStub.SocialRecovery.SendWithExceptionAsync(new SocialRecoveryInput()
        {
            Manager = new Manager
            {
                ManagerAddress = User2Address,
                DeviceString = "567"
            },
            LoginGuardianAccount = GuardianAccount,
            GuardiansApproved = { guardianApprove4 }
        });
        inputResult.TransactionResult.Error.ShouldContain("Not Satisfied criterion to create a CA Holder");
    }


    [Fact]
    public async Task SocialRecoveryTest_GuardiansApproved()
    {
        await CreateHolderDefault();
        var verificationTime = DateTime.UtcNow;
        var signature = await GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);
        var verifierServer = await CaContractStub.GetVerifierServers.CallAsync(new Empty());
        var id = verifierServer.VerifierServers[0].Id;
        var guardianApprove = new List<GuardianAccountInfo>
        {
            new()
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = id,
                    Signature = signature,
                    VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress.ToBase58()}"
                }
            }
        };

        var result = await CaContractStub.SocialRecovery.SendWithExceptionAsync(new SocialRecoveryInput()
        {
            Manager = new Manager
            {
                ManagerAddress = User2Address,
                DeviceString = "567"
            },
            LoginGuardianAccount = GuardianAccount
        });

        var caInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput()
        {
            LoginGuardianAccount = GuardianAccount
        });
        result.TransactionResult.Error.ShouldContain("invalid input Guardians Approved");
    }

    [Fact]
    public async Task SocialRecoveryTest_CaholderIsNotExits()
    {
        var result = await CaContractStub.SocialRecovery.SendWithExceptionAsync(new SocialRecoveryInput()
        {
            Manager = new Manager
            {
                ManagerAddress = User2Address,
                DeviceString = "567"
            },
            LoginGuardianAccount = GuardianAccount
        });
        result.TransactionResult.Error.ShouldContain("CA Holder does not exist");
    }

    [Fact]
    //SocialRecoveryInput is null;
    public async Task SocialRecoveryTest_inputNull()
    {
        await CreateHolderDefault();
        var socialRecoverySendAsync = await CaContractStub.SocialRecovery.SendWithExceptionAsync(
            new SocialRecoveryInput()
            {
            });
        socialRecoverySendAsync.TransactionResult.Error.ShouldContain("invalid input");
    }

    [Fact]
    public async Task SocialRecoveryTest_LoginGuardianAccountIsNull()
    {
        await CreateHolderDefault();
        var result = await CaContractStub.SocialRecovery.SendWithExceptionAsync(new SocialRecoveryInput()
        {
            Manager = new Manager
            {
                ManagerAddress = User2Address,
                DeviceString = "567"
            },
        });
        result.TransactionResult.Error.ShouldContain("invalid input login guardian account");
    }

    //manager is null
    [Fact]
    public async Task SocialRecoveryTest_ManagerIsNull()
    {
        await CreateHolderDefault();
        var result = await CaContractStub.SocialRecovery.SendWithExceptionAsync(new SocialRecoveryInput()
        {
            LoginGuardianAccount = GuardianAccount
        });
        result.TransactionResult.Error.ShouldContain("invalid input");
    }


    [Fact]
    public async Task SocialRecoveryTest_ManagerExits()
    {
        await CreateHolderDefault();
        var verificationTime = DateTime.UtcNow;
        var signature = await GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);
        var verifierServer = await CaContractStub.GetVerifierServers.CallAsync(new Empty());
        var id = verifierServer.VerifierServers[0].Id;
        var guardianApprove = new List<GuardianAccountInfo>
        {
            new()
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = id,
                    Signature = signature,
                    VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress.ToBase58()}"
                }
            }
        };
        var result = await CaContractStub.SocialRecovery.SendWithExceptionAsync(new SocialRecoveryInput()
        {
            Manager = new Manager
            {
                ManagerAddress = User1Address,
                DeviceString = "123"
            },
            LoginGuardianAccount = GuardianAccount,
            GuardiansApproved = { guardianApprove }
        });
        result.TransactionResult.Error.ShouldContain("Manager address exists");

        var caInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput()
        {
            LoginGuardianAccount = GuardianAccount
        });
        caInfo.Managers.Count.ShouldBe(1);
        caInfo.GuardiansInfo.GuardianAccounts.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SocialRecoveryTest_GuardianAccount()
    {
        await CreateHolderDefault();
        // GuardianAccount_ is "";
        var executionResult = await CaContractStub.SocialRecovery.SendWithExceptionAsync(new SocialRecoveryInput()
        {
            Manager = new Manager
            {
                ManagerAddress = User2Address,
                DeviceString = "567"
            },
            LoginGuardianAccount = ""
        });
        executionResult.TransactionResult.Error.ShouldContain("invalid input login guardian account");
    }

    [Fact]
    public async Task SocialRecoveryTest_Manager()
    {
        await CreateHolderDefault();
        //manager is null;
        var exresult = await CaContractStub.SocialRecovery.SendWithExceptionAsync(new SocialRecoveryInput()
        {
            LoginGuardianAccount = GuardianAccount
        });
        exresult.TransactionResult.Error.ShouldContain("invalid input");

        //managerAddress  is  null;
        var exeResult = await CaContractStub.SocialRecovery.SendWithExceptionAsync(new SocialRecoveryInput()
        {
            Manager = new Manager
            {
                ManagerAddress = User2Address,
                DeviceString = ""
            },
            LoginGuardianAccount = GuardianAccount
        });
        exeResult.TransactionResult.Error.ShouldContain("invalid input deviceString");

        var eResult = await CaContractStub.SocialRecovery.SendWithExceptionAsync(new SocialRecoveryInput()
        {
            Manager = new Manager
            {
                ManagerAddress = User2Address,
            },
            LoginGuardianAccount = GuardianAccount
        });
        eResult.TransactionResult.Error.ShouldContain("invalid input deviceString");
        //managerAddress is null
        var result = await CaContractStub.SocialRecovery.SendWithExceptionAsync(new SocialRecoveryInput()
        {
            Manager = new Manager
            {
                ManagerAddress = null,
                DeviceString = "123"
            },
            LoginGuardianAccount = GuardianAccount
        });
        result.TransactionResult.Error.ShouldContain("invalid input");
        //DeviceString is "";
        var executionResult = await CaContractStub.SocialRecovery.SendWithExceptionAsync(new SocialRecoveryInput()
        {
            Manager = new Manager
            {
                ManagerAddress = User2Address,
                DeviceString = ""
            },
            LoginGuardianAccount = GuardianAccount
        });
        executionResult.TransactionResult.Error.ShouldContain("invalid input deviceString");
        //DeviceString is null
        var exceptionAsync = await CaContractStub.SocialRecovery.SendWithExceptionAsync(new SocialRecoveryInput()
        {
            Manager = new Manager
            {
                ManagerAddress = User2Address,
            },
            LoginGuardianAccount = GuardianAccount
        });
        exceptionAsync.TransactionResult.Error.ShouldContain("invalid input deviceString");
    }

    [Fact]
    public async Task AddManagerTest()
    {
        await CreateHolderDefault();
        var verifierServer = await CaContractStub.GetVerifierServers.CallAsync(new Empty());
        var id = verifierServer.VerifierServers[0].Id;
        var caInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput()
        {
            LoginGuardianAccount = GuardianAccount
        });

        //success
        var manager = new Manager()
        {
            ManagerAddress = User2Address,
            DeviceString = "iphone14-2022"
        };
        await CaContractUser1Stub.AddManager.SendAsync(new AddManagerInput()
        {
            CaHash = caInfo.CaHash,
            Manager = manager
        });
        var delegateAllowance = await TokenContractStub.GetTransactionFeeDelegationsOfADelegatee.CallAsync(
            new GetTransactionFeeDelegationsOfADelegateeInput()
            {
                DelegateeAddress = caInfo.CaAddress,
                DelegatorAddress = User2Address
            });
        delegateAllowance.Delegations["ELF"].ShouldBe(10000000000000000L);
        caInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput()
        {
            LoginGuardianAccount = GuardianAccount
        });
        caInfo.Managers.ShouldContain(manager);

        //caHolder not exist
        var notExistedCash = HashHelper.ComputeFrom("Invalid CaHash");
        var txExecutionResult = await CaContractUser1Stub.AddManager.SendWithExceptionAsync(new AddManagerInput()
        {
            CaHash = notExistedCash,
            Manager = new Manager()
            {
                ManagerAddress = User2Address,
                DeviceString = "iphone14-2022"
            }
        });
        txExecutionResult.TransactionResult.Error.ShouldContain("CA holder is null");

        //input caHash is null
        txExecutionResult = await CaContractUser1Stub.AddManager.SendWithExceptionAsync(new AddManagerInput()
        {
            Manager = new Manager()
            {
                ManagerAddress = User2Address,
                DeviceString = "iphone14-2022"
            }
        });
        txExecutionResult.TransactionResult.Error.ShouldContain("invalid input CaHash");

        //input manager is null
        txExecutionResult = await CaContractUser1Stub.AddManager.SendWithExceptionAsync(new AddManagerInput()
        {
            CaHash = caInfo.CaHash
        });
        txExecutionResult.TransactionResult.Error.ShouldContain("invalid input manager");
    }
    
    [Fact]
    public async Task AddManager_Delegator()
    {
        await AddManagerTest();
         
        var delegations = await TokenContractStub.GetTransactionFeeDelegationsOfADelegatee.CallAsync(new GetTransactionFeeDelegationsOfADelegateeInput
        {
            DelegateeAddress = CaContractAddress,
            DelegatorAddress = User2Address
        });
         
        delegations.Delegations["ELF"].ShouldBe(100);
    }

    [Fact]
    public async Task AddManager_NoPermissionTest()
    {
        await CreateHolderNoPermission();
        var verifierServer = await CaContractStub.GetVerifierServers.CallAsync(new Empty());
        var id = verifierServer.VerifierServers[0].Id;
        var caInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput()
        {
            LoginGuardianAccount = GuardianAccount
        });

        //success
        var manager = new Manager()
        {
            ManagerAddress = User3Address,
            DeviceString = "iphone14-2022"
        };
        var result = await CaContractStub.AddManager.SendWithExceptionAsync(new AddManagerInput()
        {
            CaHash = caInfo.CaHash,
            Manager = manager
        });
        result.TransactionResult.Error.ShouldContain("No Permission");
    }

    [Fact]
    public async Task addManager_invalid_input()
    {
        await CreateHolderDefault();
        var verifierServer = await CaContractStub.GetVerifierServers.CallAsync(new Empty());
        var id = verifierServer.VerifierServers[0].Id;
        var caInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput()
        {
            LoginGuardianAccount = GuardianAccount
        });

        //input ManagerAddress is null
        var txExecutionResult = await CaContractUser1Stub.AddManager.SendWithExceptionAsync(new AddManagerInput()
        {
            CaHash = caInfo.CaHash,
            Manager = new Manager()
            {
                ManagerAddress = null,
                DeviceString = "iphone14-2022"
            }
        });
        txExecutionResult.TransactionResult.Error.ShouldContain("invalid input manager");

        //inout deviceString is null
        txExecutionResult = await CaContractUser1Stub.AddManager.SendWithExceptionAsync(new AddManagerInput()
        {
            CaHash = caInfo.CaHash,
            Manager = new Manager()
            {
                ManagerAddress = User2Address
            }
        });
        txExecutionResult.TransactionResult.Error.ShouldContain("invalid input manager");

        //inout deviceString is ""
        txExecutionResult = await CaContractUser1Stub.AddManager.SendWithExceptionAsync(new AddManagerInput()
        {
            CaHash = caInfo.CaHash,
            Manager = new Manager()
            {
                ManagerAddress = User2Address,
                DeviceString = ""
            }
        });
        txExecutionResult.TransactionResult.Error.ShouldContain("invalid input manager");
    }

    [Fact]
    public async Task AddManager_Address_Exists()
    {
        await CreateHolderDefault();
        var output = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput
        {
            LoginGuardianAccount = GuardianAccount
        });
        var result = await CaContractUser1Stub.AddManager.SendWithExceptionAsync(new AddManagerInput
        {
            CaHash = output.CaHash,
            Manager = new Manager
            {
                DeviceString = "test",
                ManagerAddress = User1Address
            }
        });
        result.TransactionResult.Error.ShouldContain("Manager address exists");
    }

    [Fact]
    public async Task RemoveManager_ManagerAddressNotExits()
    {
        await CreateHolderDefault();
        var verifierServer = await CaContractStub.GetVerifierServers.CallAsync(new Empty());
        var id = verifierServer.VerifierServers[0].Id;
        var caInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput()
        {
            LoginGuardianAccount = GuardianAccount
        });
        var manager = new Manager
        {
            ManagerAddress = User3Address,
            DeviceString = "123"
        };
        await CaContractUser1Stub.RemoveManager.SendAsync(new RemoveManagerInput()
        {
            CaHash = caInfo.CaHash,
            Manager = manager
        });

        caInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput()
        {
            LoginGuardianAccount = GuardianAccount
        });
        caInfo.Managers.Count.ShouldBe(1);
    }


    [Fact]
    public async Task RemoveManagerTest()
    {
        await CreateHolderDefault();
        var verifierServer = await CaContractStub.GetVerifierServers.CallAsync(new Empty());
        var id = verifierServer.VerifierServers[0].Id;
        var caInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput()
        {
            LoginGuardianAccount = GuardianAccount
        });
        //caHolder not existed
        var notExistedCash = HashHelper.ComputeFrom("Invalid CaHash");
        var txExecutionResult = await CaContractUser1Stub.RemoveManager.SendWithExceptionAsync(new RemoveManagerInput()
        {
            CaHash = notExistedCash,
            Manager = new Manager()
            {
                ManagerAddress = User2Address,
                DeviceString = "iphone14-2022"
            }
        });
        txExecutionResult.TransactionResult.Error.ShouldContain("CA holder is null.");

        //input caHash is null
        txExecutionResult = await CaContractUser1Stub.RemoveManager.SendWithExceptionAsync(new RemoveManagerInput()
        {
            Manager = new Manager()
            {
                ManagerAddress = User2Address,
                DeviceString = "iphone14-2022"
            }
        });
        txExecutionResult.TransactionResult.Error.ShouldContain("invalid input CaHash");

        //input is null can not be 
        /*var managerSendWithExceptionAsync = await CaContractUser1Stub.RemoveManager.SendWithExceptionAsync(null);
        managerSendWithExceptionAsync.TransactionResult.Error.ShouldContain("invalid input");        */

        //input DeviceString is null
        txExecutionResult = await CaContractUser1Stub.RemoveManager.SendWithExceptionAsync(new RemoveManagerInput()
        {
            CaHash = caInfo.CaHash,
            Manager = new Manager()
            {
                ManagerAddress = User2Address,
            }
        });
        txExecutionResult.TransactionResult.Error.ShouldContain("invalid input Manager");

        //input DeviceString is ""
        txExecutionResult = await CaContractUser1Stub.RemoveManager.SendWithExceptionAsync(new RemoveManagerInput()
        {
            CaHash = caInfo.CaHash,
            Manager = new Manager()
            {
                ManagerAddress = User2Address,
                DeviceString = ""
            }
        });
        txExecutionResult.TransactionResult.Error.ShouldContain("invalid input Manager");

        //input ManagerAddress is null
        txExecutionResult = await CaContractUser1Stub.RemoveManager.SendWithExceptionAsync(new RemoveManagerInput()
        {
            CaHash = caInfo.CaHash,
            Manager = new Manager()
            {
                ManagerAddress = null,
                DeviceString = "iphone14-2022"
            }
        });
        txExecutionResult.TransactionResult.Error.ShouldContain("invalid input Manager");

        //input manager is null
        txExecutionResult = await CaContractUser1Stub.RemoveManager.SendWithExceptionAsync(new RemoveManagerInput()
        {
            CaHash = caInfo.CaHash
        });
        txExecutionResult.TransactionResult.Error.ShouldContain("invalid input manager");

        //manager not exist
        var txResult = await CaContractUser1Stub.RemoveManager.SendAsync(new RemoveManagerInput()
        {
            CaHash = caInfo.CaHash,
            Manager = new Manager()
            {
                ManagerAddress = User2Address,
                DeviceString = "iphone14-2022"
            }
        });
        txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        //success
        var manager = new Manager
        {
            ManagerAddress = User1Address,
            DeviceString = "123"
        };
        await CaContractUser1Stub.RemoveManager.SendAsync(new RemoveManagerInput()
        {
            CaHash = caInfo.CaHash,
            Manager = manager
        });

        caInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput()
        {
            LoginGuardianAccount = GuardianAccount
        });
        caInfo.Managers.ShouldNotContain(manager);
        var delegateAllowance = await TokenContractStub.GetTransactionFeeDelegationsOfADelegatee.CallAsync(
            new GetTransactionFeeDelegationsOfADelegateeInput()
            {
                DelegateeAddress = caInfo.CaAddress,
                DelegatorAddress = User1Address
            });
        delegateAllowance.Delegations.Count.ShouldBe(0);
    }

    [Fact]
    public async Task RemoveManager_NoPermisson_Test()
    {
        await CreateHolderNoPermission();
        var verifierServer = await CaContractStub.GetVerifierServers.CallAsync(new Empty());
        var id = verifierServer.VerifierServers[0].Id;
        var caInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput()
        {
            LoginGuardianAccount = GuardianAccount
        });
        var manager = new Manager
        {
            ManagerAddress = User3Address,
            DeviceString = "123"
        };
        var result = await CaContractStub.RemoveManager.SendWithExceptionAsync(new RemoveManagerInput()
        {
            CaHash = caInfo.CaHash,
            Manager = manager
        });
        result.TransactionResult.Error.ShouldContain("No Permission");
    }

    private async Task CreateHolderNoPermission()
    {
        var verificationTime = DateTime.UtcNow;
        await CaContractStub.Initialize.SendAsync(new InitializeInput
        {
            ContractAdmin = DefaultAddress
        });
        {
            await CaContractStub.AddVerifierServerEndPoints.SendAsync(new AddVerifierServerEndPointsInput
            {
                Name = VerifierName,
                ImageUrl = "url",
                EndPoints = { "127.0.0.1" },
                VerifierAddressList = { VerifierAddress }
            });
            await CaContractStub.AddVerifierServerEndPoints.SendAsync(new AddVerifierServerEndPointsInput
            {
                Name = VerifierName1,
                ImageUrl = "url",
                EndPoints = { "127.0.0.1" },
                VerifierAddressList = { VerifierAddress1 }
            });
            await CaContractStub.AddVerifierServerEndPoints.SendAsync(new AddVerifierServerEndPointsInput
            {
                Name = VerifierName2,
                ImageUrl = "url",
                EndPoints = { "127.0.0.1" },
                VerifierAddressList = { VerifierAddress2 }
            });
        }
        var signature = await GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);
        var verifierServer = await CaContractStub.GetVerifierServers.CallAsync(new Empty());
        var id = verifierServer.VerifierServers[0].Id;
        await CaContractStub.CreateCAHolder.SendAsync(new CreateCAHolderInput
        {
            GuardianApproved = new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = id,
                    Signature = signature,
                    VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress.ToBase58()}"
                }
            },
            Manager = new Manager
            {
                ManagerAddress = User1Address,
                DeviceString = "123"
            }
        });
    }
}
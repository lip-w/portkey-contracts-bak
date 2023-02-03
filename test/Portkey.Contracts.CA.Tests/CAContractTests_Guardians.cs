using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace Portkey.Contracts.CA;

public partial class CAContractTests
{
    private const string GuardianAccount = "test@google.com";
    private const string GuardianAccount1 = "test1@google.com";
    private const string GuardianAccount2 = "test2@google.com";
    private const string GuardianAccountNotExist = "NotExists@google.com";
    private const string VerifierName = "HuoBi";
    private Hash _verifierId;
    private const string VerifierName1 = "PortKey";
    private Hash _verifierId1;
    private const string VerifierName2 = "Binance";
    private Hash _verifierId2;
    private const string ImageUrl = "https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/Portkey.png";


    private ByteString GenerateSignature(ECKeyPair verifier, Address verifierAddress,
        DateTime verificationTime, string guardianType, int type)
    {
        var data = $"{type},{guardianType},{verificationTime},{verifierAddress.ToBase58()}";
        var dataHash = HashHelper.ComputeFrom(data);
        var signature = CryptoHelper.SignWithPrivateKey(verifier.PrivateKey, dataHash.ToByteArray());
        return ByteStringHelper.FromHexString(signature.ToHex());
    }

    private async Task<Hash> CreateHolder()
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
                ImageUrl = ImageUrl,
                EndPoints = {"127.0.0.1"},
                VerifierAddressList = {VerifierAddress}
            });
            await CaContractStub.AddVerifierServerEndPoints.SendAsync(new AddVerifierServerEndPointsInput
            {
                Name = VerifierName1,
                ImageUrl = ImageUrl,
                EndPoints = {"127.0.0.1"},
                VerifierAddressList = {VerifierAddress1}
            });
            await CaContractStub.AddVerifierServerEndPoints.SendAsync(new AddVerifierServerEndPointsInput
            {
                Name = VerifierName2,
                ImageUrl = ImageUrl,
                EndPoints = {"127.0.0.1"},
                VerifierAddressList = {VerifierAddress2}
            });
        }
        {
            var verifierServers = await CaContractStub.GetVerifierServers.CallAsync(new Empty());
            _verifierId = verifierServers.VerifierServers[0].Id;
            _verifierId1 = verifierServers.VerifierServers[1].Id;
            _verifierId2 = verifierServers.VerifierServers[2].Id;
        }
        var signature = GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);
        await CaContractStub.CreateCAHolder.SendAsync(new CreateCAHolderInput
        {
            GuardianApproved = new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId,
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
        holderInfo.GuardiansInfo.GuardianAccounts.First().Value.ShouldBe(GuardianAccount);

        //success
        var manager = new Manager
        {
            ManagerAddress = DefaultAddress,
            DeviceString = "iphone14-2022"
        };
        await TokenContractStub.Transfer.SendAsync(new TransferInput
        {
            Amount = 1000000000000,
            Symbol = "ELF",
            To = holderInfo.CaAddress
        });
        await CaContractUser1Stub.AddManager.SendAsync(new AddManagerInput
        {
            CaHash = holderInfo.CaHash,
            Manager = manager
        });

        return holderInfo.CaHash;
    }

    private async Task<Hash> CreateCAHolderNoPermission()
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
                ImageUrl = ImageUrl,
                EndPoints = {"127.0.0.1"},
                VerifierAddressList = {VerifierAddress}
            });
            await CaContractStub.AddVerifierServerEndPoints.SendAsync(new AddVerifierServerEndPointsInput
            {
                Name = VerifierName1,
                ImageUrl = ImageUrl,
                EndPoints = {"127.0.0.1"},
                VerifierAddressList = {VerifierAddress1}
            });
            await CaContractStub.AddVerifierServerEndPoints.SendAsync(new AddVerifierServerEndPointsInput
            {
                Name = VerifierName2,
                ImageUrl = ImageUrl,
                EndPoints = {"127.0.0.1"},
                VerifierAddressList = {VerifierAddress2}
            });
        }
        {
            var verifierServers = await CaContractStub.GetVerifierServers.CallAsync(new Empty());
            _verifierId = verifierServers.VerifierServers[0].Id;
            _verifierId1 = verifierServers.VerifierServers[1].Id;
            _verifierId2 = verifierServers.VerifierServers[2].Id;
        }
        var signature = GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);
        await CaContractStub.CreateCAHolder.SendAsync(new CreateCAHolderInput
        {
            GuardianApproved = new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId,
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
        holderInfo.GuardiansInfo.GuardianAccounts.First().Value.ShouldBe(GuardianAccount);

        return holderInfo.CaHash;
    }

    [Fact]
    public async Task<Hash> AddGuardianTest()
    {
        var verificationTime = DateTime.UtcNow;
        var caHash = await CreateHolder();
        var signature = GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);
        var signature1 =
            GenerateSignature(VerifierKeyPair1, VerifierAddress1, verificationTime, GuardianAccount1, 0);
        var verificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress.ToBase58()}";
        var guardianApprove = new List<GuardianAccountInfo>
        {
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId,
                    Signature = signature,
                    VerificationDoc = verificationDoc
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
                    Id = _verifierId1,
                    Signature = signature1,
                    VerificationDoc = $"{0},{GuardianAccount1},{verificationTime},{VerifierAddress1.ToBase58()}"
                }
            },
            GuardiansApproved = {guardianApprove}
        };
        await CaContractStubManager1.AddGuardian.SendAsync(input);
        {
            var holderInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput
            {
                CaHash = caHash
            });
            holderInfo.GuardiansInfo.GuardianAccounts.Count.ShouldBe(2);
            holderInfo.GuardiansInfo.GuardianAccounts.Last().Value.ShouldBe(GuardianAccount1);
            holderInfo.GuardiansInfo.LoginGuardianAccountIndexes.Count.ShouldBe(1);
        }
        return caHash;
    }


    [Fact]
    public async Task<Hash> AddGuardianTest_RepeatedGuardianType_DifferentVerifier()
    {
        var verificationTime = DateTime.UtcNow;
        var signature = GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);
        var signature1 =
            GenerateSignature(VerifierKeyPair1, VerifierAddress1, verificationTime, GuardianAccount1, 0);
        var signature2 =
            GenerateSignature(VerifierKeyPair2, VerifierAddress2, verificationTime, GuardianAccount1, 0);
        var caHash = await AddGuardianTest();
        var guardianApprove = new List<GuardianAccountInfo>
        {
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId,
                    Signature = signature,
                    VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress.ToBase58()}"
                }
            },
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount1,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId1,
                    Signature = signature1,
                    VerificationDoc = $"{0},{GuardianAccount1},{verificationTime},{VerifierAddress1.ToBase58()}"
                }
            },
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
                    Id = _verifierId2,
                    Signature = signature2,
                    VerificationDoc = $"{0},{GuardianAccount1},{verificationTime},{VerifierAddress2.ToBase58()}"
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
            holderInfo.GuardiansInfo.GuardianAccounts.Count.ShouldBe(3);
            holderInfo.GuardiansInfo.GuardianAccounts.Last().Value.ShouldBe(GuardianAccount1);
            holderInfo.GuardiansInfo.GuardianAccounts.Last().Guardian.Verifier.Id.ShouldBe(_verifierId2);
        }
        return caHash;
    }

    [Fact]
    public async Task<Hash> AddGuardianTest_Success_GuardianCount4_Approve3()
    {
        var caHash = await AddGuardian();
        var verificationTime = DateTime.UtcNow;
        var signature = GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);
        var signature1 =
            GenerateSignature(VerifierKeyPair1, VerifierAddress1, verificationTime, GuardianAccount1, 0);
        var signature2 =
            GenerateSignature(VerifierKeyPair2, VerifierAddress2, verificationTime, GuardianAccount1, 0);
        var signature3 = GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount2, 0);
        var signature4 =
            GenerateSignature(VerifierKeyPair1, VerifierAddress1, verificationTime, GuardianAccount2, 0);
        var guardianApprove = new List<GuardianAccountInfo>
        {
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId,
                    Signature = signature,
                    VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress.ToBase58()}"
                }
            },
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount1,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId1,
                    Signature = signature1,
                    VerificationDoc = $"{0},{GuardianAccount1},{verificationTime},{VerifierAddress1.ToBase58()}"
                }
            },
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount2,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId,
                    Signature = signature3,
                    VerificationDoc = $"{0},{GuardianAccount2},{verificationTime},{VerifierAddress.ToBase58()}"
                }
            }
        };
        var input = new AddGuardianInput
        {
            CaHash = caHash,
            GuardianToAdd = new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount2,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId1,
                    Signature = signature4,
                    VerificationDoc = $"{0},{GuardianAccount2},{verificationTime},{VerifierAddress1.ToBase58()}"
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
            holderInfo.GuardiansInfo.GuardianAccounts.Count.ShouldBe(5);
            holderInfo.GuardiansInfo.GuardianAccounts.Last().Value.ShouldBe(GuardianAccount2);
            holderInfo.GuardiansInfo.GuardianAccounts.Last().Guardian.Verifier.Id.ShouldBe(_verifierId1);
        }
        return caHash;
    }

    private async Task<Hash> AddGuardian()
    {
        var caHash = await AddGuardianTest_RepeatedGuardianType_DifferentVerifier();
        var verificationTime = DateTime.UtcNow;
        var signature = GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);
        var signature1 =
            GenerateSignature(VerifierKeyPair1, VerifierAddress1, verificationTime, GuardianAccount1, 0);
        var signature2 =
            GenerateSignature(VerifierKeyPair2, VerifierAddress2, verificationTime, GuardianAccount1, 0);
        var signature3 = GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount2, 0);
        var guardianApprove = new List<GuardianAccountInfo>
        {
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId,
                    Signature = signature,
                    VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress.ToBase58()}"
                }
            },
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount1,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId1,
                    Signature = signature1,
                    VerificationDoc = $"{0},{GuardianAccount1},{verificationTime},{VerifierAddress1.ToBase58()}"
                }
            },
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount1,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId2,
                    Signature = signature2,
                    VerificationDoc = $"{0},{GuardianAccount1},{verificationTime},{VerifierAddress2.ToBase58()}"
                }
            }
        };
        var input = new AddGuardianInput
        {
            CaHash = caHash,
            GuardianToAdd = new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount2,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId,
                    Signature = signature3,
                    VerificationDoc = $"{0},{GuardianAccount2},{verificationTime},{VerifierAddress.ToBase58()}"
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
            holderInfo.GuardiansInfo.GuardianAccounts.Count.ShouldBe(4);
            holderInfo.GuardiansInfo.GuardianAccounts.Last().Value.ShouldBe(GuardianAccount2);
            holderInfo.GuardiansInfo.GuardianAccounts.Last().Guardian.Verifier.Id.ShouldBe(_verifierId);
        }
        return caHash;
    }

    // [Fact]
    // public async Task AddGuardianTest_Failed_ApproveCountNotEnough_CountLessThan4()
    // {
    //     var caHash = await AddGuardian();
    //     var verificationTime = DateTime.UtcNow;
    //     var signature = GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianType, 0);
    //     var signature1 =
    //         GenerateSignature(VerifierKeyPair1, VerifierAddress1, verificationTime, GuardianType1, 0);
    //     var signature2 =
    //         GenerateSignature(VerifierKeyPair2, VerifierAddress2, verificationTime, GuardianType1, 0);
    //     var signature4 =
    //         GenerateSignature(VerifierKeyPair1, VerifierAddress1, verificationTime, GuardianType2, 0);
    //     var guardianApprove = new List<Guardian>
    //     {
    //         new Guardian
    //         {
    //             GuardianType = new GuardianType
    //             {
    //                 GuardianType_ = GuardianType,
    //                 Type = 0
    //             },
    //             Verifier = new Verifier
    //             {
    //                 Name = VerifierName,
    //                 Signature = signature,
    //                 VerificationDoc = $"{0},{GuardianType},{verificationTime},{VerifierAddress.ToBase58()}"
    //             }
    //         },
    //         new Guardian
    //         {
    //             GuardianType = new GuardianType
    //             {
    //                 GuardianType_ = GuardianType1,
    //                 Type = 0
    //             },
    //             Verifier = new Verifier
    //             {
    //                 Name = VerifierName1,
    //                 Signature = signature1,
    //                 VerificationDoc = $"{0},{GuardianType1},{verificationTime},{VerifierAddress1.ToBase58()}"
    //             }
    //         }
    //     };
    //     var input = new AddGuardianInput
    //     {
    //         CaHash = caHash,
    //         GuardianToAdd = new Guardian
    //         {
    //             GuardianType = new GuardianType
    //             {
    //                 GuardianType_ = GuardianType2,
    //                 Type = 0
    //             },
    //             Verifier = new Verifier
    //             {
    //                 Name = VerifierName1,
    //                 Signature = signature4,
    //                 VerificationDoc = $"{0},{GuardianType2},{verificationTime},{VerifierAddress1.ToBase58()}"
    //             }
    //         },
    //         GuardiansApproved = {guardianApprove}
    //     };
    //     var executionResult = await CaContractStub.AddGuardian.SendWithExceptionAsync(input);
    //     executionResult.TransactionResult.Error.ShouldContain("Not Satisfied criterion to create a CA Holder");
    // }

    [Fact]
    public async Task AddGuardianTest_Failed_IncorrectData()
    {
        var caHash = await CreateHolder();
        var verificationTime = DateTime.UtcNow;
        var signature = GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount1, 0);
        var signature1 =
            GenerateSignature(VerifierKeyPair1, VerifierAddress1, verificationTime, GuardianAccount1, 0);
        var guardianApprove = new List<GuardianAccountInfo>
        {
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId,
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
                    Id = _verifierId1,
                    Signature = signature1,
                    VerificationDoc = $"{0},{GuardianAccount1},{verificationTime},{VerifierAddress1.ToBase58()}"
                }
            },
            GuardiansApproved = {guardianApprove}
        };
        var executionResult = await CaContractStub.AddGuardian.SendWithExceptionAsync(input);
        executionResult.TransactionResult.Error.ShouldContain("Not Satisfied criterion to create a CA Holder");
    }

    [Fact]
    public async Task AddGuardianTest_Failed_IncorrectAddress()
    {
        var caHash = await CreateHolder();
        var verificationTime = DateTime.UtcNow;
        var signature = GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount1, 0);
        var signature1 =
            GenerateSignature(VerifierKeyPair1, VerifierAddress1, verificationTime, GuardianAccount1, 0);
        var guardianApprove = new List<GuardianAccountInfo>
        {
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId,
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
                    Id = _verifierId,
                    Signature = signature1,
                    VerificationDoc = $"{0},{GuardianAccount1},{verificationTime},{VerifierAddress.ToBase58()}"
                }
            },
            GuardiansApproved = {guardianApprove}
        };
        var executionResult = await CaContractStub.AddGuardian.SendWithExceptionAsync(input);
        executionResult.TransactionResult.Error.ShouldContain("Verification failed.");
    }

    [Fact]
    public async Task AddGuardianTest_AlreadyExist()
    {
        var caHash = await AddGuardianTest();
        var verificationTime = DateTime.UtcNow;
        var signature = GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount1, 0);
        var signature1 =
            GenerateSignature(VerifierKeyPair1, VerifierAddress1, verificationTime, GuardianAccount1, 0);

        {
            var holderInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput
            {
                CaHash = caHash
            });
            holderInfo.GuardiansInfo.GuardianAccounts.Count.ShouldBe(2);
            holderInfo.GuardiansInfo.GuardianAccounts.Last().Value.ShouldBe(GuardianAccount1);
        }
        {
            var guardianApprove = new List<GuardianAccountInfo>
            {
                new GuardianAccountInfo
                {
                    Type = GuardianType.OfEmail,
                    Value = GuardianAccount,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = _verifierId,
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
                        Id = _verifierId1,
                        Signature = signature1,
                        VerificationDoc = $"{0},{GuardianAccount1},{verificationTime},{VerifierAddress.ToBase58()}"
                    }
                },
                GuardiansApproved = {guardianApprove}
            };
            await CaContractStub.AddGuardian.SendAsync(input);
        }
        {
            var holderInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput
            {
                CaHash = caHash
            });
            holderInfo.GuardiansInfo.GuardianAccounts.Count.ShouldBe(2);
            holderInfo.GuardiansInfo.GuardianAccounts.Last().Value.ShouldBe(GuardianAccount1);
        }
    }

    [Fact]
    public async Task AddGuardianTest_Failed_HolderNotExist()
    {
        var verificationTime = DateTime.UtcNow;
        var signature = GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount1, 0);
        var signature1 =
            GenerateSignature(VerifierKeyPair1, VerifierAddress1, verificationTime, GuardianAccount1, 0);
        var guardianApprove = new List<GuardianAccountInfo>
        {
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId,
                    VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress.ToBase58()}",
                    Signature = signature
                }
            }
        };
        var executionResult = await CaContractStub.AddGuardian.SendWithExceptionAsync(new AddGuardianInput
        {
            CaHash = HashHelper.ComputeFrom("aaa"),
            GuardianToAdd = new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount1,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId1,
                    VerificationDoc = $"{0},{GuardianAccount1},{verificationTime},{VerifierAddress.ToBase58()}",
                    Signature = signature1
                }
            },
            GuardiansApproved =
            {
                guardianApprove
            }
        });
        executionResult.TransactionResult.Error.ShouldContain("CA holder does not exist.");
    }

    [Fact]
    public async Task AddGuardianTest_Failed_InvalidInput()
    {
        var caHash = await CreateHolder();
        var verificationTime = DateTime.UtcNow;
        var signature = GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount1, 0);
        var signature1 =
            GenerateSignature(VerifierKeyPair1, VerifierAddress1, verificationTime, GuardianAccount1, 0);
        var guardianApprove = new List<GuardianAccountInfo>
        {
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId,
                    Signature = signature,
                    VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress.ToBase58()}",
                }
            }
        };
        {
            var executionResult = await CaContractStub.AddGuardian.SendWithExceptionAsync(new AddGuardianInput
            {
                GuardianToAdd = new GuardianAccountInfo
                {
                    Type = GuardianType.OfEmail,
                    Value = GuardianAccount1,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = _verifierId1,
                        Signature = signature1,
                        VerificationDoc = $"{0},{GuardianAccount1},{verificationTime},{VerifierAddress.ToBase58()}",
                    }
                },
                GuardiansApproved =
                {
                    guardianApprove
                }
            });
            executionResult.TransactionResult.Error.ShouldContain("Invalid input.");
        }
        {
            var executionResult = await CaContractStub.AddGuardian.SendWithExceptionAsync(new AddGuardianInput
            {
                CaHash = caHash,
                GuardiansApproved =
                {
                    guardianApprove
                }
            });
            executionResult.TransactionResult.Error.ShouldContain("Invalid input.");
        }
        {
            var executionResult = await CaContractStub.AddGuardian.SendWithExceptionAsync(new AddGuardianInput
            {
                CaHash = caHash,
                GuardianToAdd = new GuardianAccountInfo
                {
                    Type = GuardianType.OfEmail,
                    Value = GuardianAccount1,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = _verifierId1,
                        Signature = signature1,
                        VerificationDoc = $"{0},{GuardianAccount1},{verificationTime},{VerifierAddress.ToBase58()}",
                    }
                }
            });
            executionResult.TransactionResult.Error.ShouldContain("Invalid input.");
        }
    }

    [Fact]
    public async Task AddGuardian_failed_for_guardianNotExits()
    {
        var verificationTime = DateTime.UtcNow;
        ;
        var caHash = await CreateHolder();
        var signature = GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);
        var signature1 =
            GenerateSignature(VerifierKeyPair1, VerifierAddress1, verificationTime, GuardianAccount1, 0);
        var guardianApprove = new List<GuardianAccountInfo>
        {
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId1,
                    Signature = signature1,
                    VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress1.ToBase58()}"
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
                    Id = _verifierId1,
                    Signature = signature1,
                    VerificationDoc = $"{0},{GuardianAccount1},{verificationTime},{VerifierAddress1.ToBase58()}"
                }
            },
            GuardiansApproved = {guardianApprove}
        };
        var result = await CaContractStub.AddGuardian.SendWithExceptionAsync(input);
        result.TransactionResult.Error.ShouldContain("Not Satisfied criterion to create a CA Holder");
    }

    [Fact]
    public async Task<Hash> RemoveGuardianTest()
    {
        var caHash = await AddGuardianTest();
        var verificationTime = DateTime.UtcNow;
        ;
        var signature = GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);
        var signature1 =
            GenerateSignature(VerifierKeyPair1, VerifierAddress1, verificationTime, GuardianAccount1, 0);
        var guardianApprove = new List<GuardianAccountInfo>
        {
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId,
                    Signature = signature,
                    VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress.ToBase58()}",
                }
            }
        };
        {
            var holderInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput
            {
                CaHash = caHash
            });
            holderInfo.GuardiansInfo.GuardianAccounts.Count.ShouldBe(2);
            holderInfo.GuardiansInfo.GuardianAccounts.Last().Value.ShouldBe(GuardianAccount1);
        }
        await CaContractStubManager1.RemoveGuardian.SendAsync(new RemoveGuardianInput
        {
            CaHash = caHash,
            GuardianToRemove = new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount1,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId1
                }
            },
            GuardiansApproved = {guardianApprove}
        });
        {
            var holderInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput
            {
                CaHash = caHash
            });
            holderInfo.GuardiansInfo.GuardianAccounts.Count.ShouldBe(1);
            holderInfo.GuardiansInfo.GuardianAccounts.Last().Value.ShouldBe(GuardianAccount);
        }
        return caHash;
    }


    [Fact]
    public async Task RemoveGuardian_failed_guardianNotExits()
    {
        var caHash = await AddGuardianTest();
        var verificationTime = DateTime.UtcNow;
        ;
        var signature = GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);
        var signature1 =
            GenerateSignature(VerifierKeyPair1, VerifierAddress1, verificationTime, GuardianAccount1, 0);
        var guardianApprove = new List<GuardianAccountInfo>
        {
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId1,
                    Signature = signature1,
                    VerificationDoc = $"{0},{GuardianAccount1},{verificationTime},{VerifierAddress.ToBase58()}",
                }
            }
        };

        var result = await CaContractStub.RemoveGuardian.SendWithExceptionAsync(new RemoveGuardianInput
        {
            CaHash = caHash,
            GuardianToRemove = new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount1,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId1
                }
            },
            GuardiansApproved = {guardianApprove}
        });
        result.TransactionResult.Error.ShouldContain("Not Satisfied criterion to create a CA Holder");
    }

    [Fact]
    public async Task RemoveGuardianTest_AlreadyRemoved()
    {
        var caHash = await RemoveGuardianTest();
        var verificationTime = DateTime.UtcNow;
        ;
        var signature = GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount1, 0);
        var signature1 =
            GenerateSignature(VerifierKeyPair1, VerifierAddress1, verificationTime, GuardianAccount1, 0);
        var guardianApprove = new List<GuardianAccountInfo>
        {
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId,
                    Signature = signature,
                    VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress1.ToBase58()}",
                }
            }
        };
        {
            var holderInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput
            {
                CaHash = caHash
            });
            holderInfo.GuardiansInfo.GuardianAccounts.Count.ShouldBe(1);
            holderInfo.GuardiansInfo.GuardianAccounts.Last().Value.ShouldBe(GuardianAccount);
        }
        await CaContractStub.RemoveGuardian.SendAsync(new RemoveGuardianInput
        {
            CaHash = caHash,
            GuardianToRemove = new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount1,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId1,
                    Signature = signature1,
                    VerificationDoc = $"{0},{GuardianAccount1},{verificationTime},{VerifierAddress1.ToBase58()}",
                }
            },
            GuardiansApproved = {guardianApprove}
        });
        {
            var holderInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput
            {
                CaHash = caHash
            });
            holderInfo.GuardiansInfo.GuardianAccounts.Count.ShouldBe(1);
            holderInfo.GuardiansInfo.GuardianAccounts.Last().Value.ShouldBe(GuardianAccount);
        }
    }

    [Fact]
    public async Task RemoveGuardianTest_Failed_HolderNotExist()
    {
        var verificationTime = DateTime.UtcNow;
        ;
        var signature = GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount1, 0);
        var signature1 =
            GenerateSignature(VerifierKeyPair1, VerifierAddress1, verificationTime, GuardianAccount1, 0);
        var guardianApprove = new List<GuardianAccountInfo>
        {
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId,
                    Signature = signature,
                    VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress1.ToBase58()}",
                }
            }
        };
        var executionResult = await CaContractStub.RemoveGuardian.SendWithExceptionAsync(new RemoveGuardianInput
        {
            CaHash = HashHelper.ComputeFrom("aaa"),
            GuardianToRemove = new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount1,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId1,
                    Signature = signature1,
                    VerificationDoc = $"{0},{GuardianAccount1},{verificationTime},{VerifierAddress1.ToBase58()}",
                }
            },
            GuardiansApproved =
            {
                guardianApprove
            }
        });
        executionResult.TransactionResult.Error.ShouldContain("CA holder does not exist.");
    }

    [Fact]
    public async Task RemoveGuardianTest_Failed_InvalidInput()
    {
        var caHash = await CreateHolder();
        var verificationTime = DateTime.UtcNow;
        ;
        var signature = GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount1, 0);
        var signature1 =
            GenerateSignature(VerifierKeyPair1, VerifierAddress1, verificationTime, GuardianAccount1, 0);
        var guardianApprove = new List<GuardianAccountInfo>
        {
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId,
                    Signature = signature,
                    VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress.ToBase58()}",
                }
            }
        };
        {
            var executionResult = await CaContractStub.RemoveGuardian.SendWithExceptionAsync(new RemoveGuardianInput
            {
                GuardianToRemove = new GuardianAccountInfo
                {
                    Type = GuardianType.OfEmail,
                    Value = GuardianAccount1,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = _verifierId1
                    }
                },
                GuardiansApproved =
                {
                    guardianApprove
                }
            });
            executionResult.TransactionResult.Error.ShouldContain("Invalid input.");
        }
        {
            var executionResult = await CaContractStub.RemoveGuardian.SendWithExceptionAsync(new RemoveGuardianInput
            {
                CaHash = caHash,
                GuardiansApproved =
                {
                    guardianApprove
                }
            });
            executionResult.TransactionResult.Error.ShouldContain("Invalid input.");
        }
        {
            var executionResult = await CaContractStub.RemoveGuardian.SendWithExceptionAsync(new RemoveGuardianInput
            {
                CaHash = caHash,
                GuardianToRemove = new GuardianAccountInfo
                {
                    Type = GuardianType.OfEmail,
                    Value = GuardianAccount1,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = _verifierId1
                    }
                }
            });
            executionResult.TransactionResult.Error.ShouldContain("Invalid input.");
        }
    }

    [Fact]
    public async Task RemoveGuardianTest_Failed_LastLoginGuardian()
    {
        var caHash = await AddGuardianTest_RepeatedGuardianType_DifferentVerifier();
        var verificationTime = DateTime.UtcNow;
        var signature = GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);
        var signature1 =
            GenerateSignature(VerifierKeyPair1, VerifierAddress1, verificationTime, GuardianAccount1, 0);
        var signature2 =
            GenerateSignature(VerifierKeyPair2, VerifierAddress2, verificationTime, GuardianAccount1, 0);
        List<GuardianAccountInfo> guardianApprove;
        {
            guardianApprove = new List<GuardianAccountInfo>
            {
                new GuardianAccountInfo
                {
                    Type = GuardianType.OfEmail,
                    Value = GuardianAccount,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = _verifierId,
                        Signature = signature,
                        VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress.ToBase58()}",
                    }
                },
                new GuardianAccountInfo
                {
                    Type = GuardianType.OfEmail,
                    Value = GuardianAccount1,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = _verifierId2,
                        Signature = signature2,
                        VerificationDoc = $"{0},{GuardianAccount1},{verificationTime},{VerifierAddress2.ToBase58()}",
                    }
                }
            };
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
                            Id = _verifierId1
                        }
                    }
                }
            });
            var holderInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput
            {
                CaHash = caHash
            });
            holderInfo.GuardiansInfo.LoginGuardianAccountIndexes.Count.ShouldBe(2);
            var executionResult = await CaContractStub.RemoveGuardian.SendWithExceptionAsync(new RemoveGuardianInput
            {
                CaHash = caHash,
                GuardianToRemove = new GuardianAccountInfo
                {
                    Type = GuardianType.OfEmail,
                    Value = GuardianAccount1,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = _verifierId1
                    }
                },
                GuardiansApproved = {guardianApprove}
            });
            executionResult.TransactionResult.Error.ShouldContain("Cannot remove a Guardian for login, to remove it, unset it first.");
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
                            Id = _verifierId2
                        }
                    }
                }
            });
            await CaContractStub.RemoveGuardian.SendAsync(new RemoveGuardianInput
            {
                CaHash = caHash,
                GuardianToRemove = new GuardianAccountInfo
                {
                    Type = GuardianType.OfEmail,
                    Value = GuardianAccount1,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = _verifierId1
                    }
                },
                GuardiansApproved = {guardianApprove}
            });
            var executionResult1 = await CaContractStub.RemoveGuardian.SendWithExceptionAsync(new RemoveGuardianInput
            {
                CaHash = caHash,
                GuardianToRemove = new GuardianAccountInfo
                {
                    Type = GuardianType.OfEmail,
                    Value = GuardianAccount1,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = _verifierId2
                    }
                },
                GuardiansApproved = {guardianApprove}
            });
            executionResult1.TransactionResult.Error.ShouldContain("Cannot remove a Guardian for login, to remove it, unset it first.");
            holderInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput
            {
                CaHash = caHash
            });
            holderInfo.GuardiansInfo.GuardianAccounts.Count.ShouldBe(2);
            await CaContractStub.UnsetGuardianAccountForLogin.SendAsync(new UnsetGuardianAccountForLoginInput
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
                            Id = _verifierId2
                        }
                    }
                }
            });
            guardianApprove = new List<GuardianAccountInfo>
            {
                new GuardianAccountInfo
                {
                    Type = GuardianType.OfEmail,
                    Value = GuardianAccount,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = _verifierId,
                        Signature = signature,
                        VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress.ToBase58()}",
                    }
                }
            };
            await CaContractStub.RemoveGuardian.SendAsync(new RemoveGuardianInput
            {
                CaHash = caHash,
                GuardianToRemove = new GuardianAccountInfo
                {
                    Type = GuardianType.OfEmail,
                    Value = GuardianAccount1,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = _verifierId2
                    }
                },
                GuardiansApproved = {guardianApprove}
            });
            holderInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput
            {
                CaHash = caHash
            });
            holderInfo.GuardiansInfo.GuardianAccounts.Count.ShouldBe(1);
        }
        {
            guardianApprove = new List<GuardianAccountInfo>
            {
                new GuardianAccountInfo
                {
                    Type = GuardianType.OfEmail,
                    Value = GuardianAccount1,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = _verifierId1,
                        Signature = signature1,
                        VerificationDoc = $"{0},{GuardianAccount1},{verificationTime},{VerifierAddress1.ToBase58()}",
                    }
                },
                new GuardianAccountInfo
                {
                    Type = GuardianType.OfEmail,
                    Value = GuardianAccount1,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = _verifierId2,
                        Signature = signature2,
                        VerificationDoc = $"{0},{GuardianAccount1},{verificationTime},{VerifierAddress2.ToBase58()}",
                    }
                }
            };
            var exception = await CaContractStub.RemoveGuardian.SendWithExceptionAsync(new RemoveGuardianInput
            {
                CaHash = caHash,
                GuardianToRemove = new GuardianAccountInfo
                {
                    Type = GuardianType.OfEmail,
                    Value = GuardianAccount,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = _verifierId
                    }
                },
                GuardiansApproved = {guardianApprove}
            });
            exception.TransactionResult.Error.ShouldContain(
                "Cannot remove a Guardian for login, to remove it, unset it first.");
        }
    }

    [Fact]
    public async Task UpdateGuardianTest()
    {
        var caHash = await AddGuardianTest();
        var verificationTime = DateTime.UtcNow;
        ;
        var signature = GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);
        var signature1 =
            GenerateSignature(VerifierKeyPair1, VerifierAddress1, verificationTime, GuardianAccount1, 0);
        var signature2 =
            GenerateSignature(VerifierKeyPair2, VerifierAddress2, verificationTime, GuardianAccount1, 0);

        var guardianApprove = new List<GuardianAccountInfo>
        {
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId,
                    Signature = signature,
                    VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress.ToBase58()}",
                }
            },
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId,
                    Signature = signature,
                    VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress.ToBase58()}",
                }
            }
        };
        await CaContractStub.UpdateGuardian.SendAsync(new UpdateGuardianInput
        {
            CaHash = caHash,
            GuardianToUpdatePre = new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount1,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId1
                }
            },
            GuardianToUpdateNew = new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount1,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId2
                }
            },
            GuardiansApproved = {guardianApprove}
        });
        {
            var guardian = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput
            {
                CaHash = caHash
            });
            guardian.GuardiansInfo.GuardianAccounts.Count.ShouldBe(2);
            guardian.GuardiansInfo.GuardianAccounts.Last().Value.ShouldBe(GuardianAccount1);
            guardian.GuardiansInfo.GuardianAccounts.Last().Guardian.Verifier.Id.ShouldBe(_verifierId2);
        }
    }

    [Fact]
    public async Task UpdateGuardianTest_AlreadyExist()
    {
        var caHash = await AddGuardianTest_RepeatedGuardianType_DifferentVerifier();
        var verificationTime = DateTime.UtcNow;
        ;
        var signature = GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);
        var signature1 =
            GenerateSignature(VerifierKeyPair1, VerifierAddress1, verificationTime, GuardianAccount1, 0);
        var signature2 =
            GenerateSignature(VerifierKeyPair2, VerifierAddress2, verificationTime, GuardianAccount1, 0);

        var guardianApprove = new List<GuardianAccountInfo>
        {
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId,
                    Signature = signature,
                    VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress.ToBase58()}",
                }
            },
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount1,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId1,
                    Signature = signature1,
                    VerificationDoc = $"{0},{GuardianAccount1},{verificationTime},{VerifierAddress1.ToBase58()}",
                }
            },
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount1,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId2,
                    Signature = signature2,
                    VerificationDoc = $"{0},{GuardianAccount1},{verificationTime},{VerifierAddress2.ToBase58()}",
                }
            },
        };
        await CaContractStub.UpdateGuardian.SendAsync(new UpdateGuardianInput
        {
            CaHash = caHash,
            GuardianToUpdatePre = new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount1,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId1,
                }
            },
            GuardianToUpdateNew = new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount1,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId2
                }
            },
            GuardiansApproved = {guardianApprove}
        });
        {
            var guardian = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput
            {
                CaHash = caHash
            });
            guardian.GuardiansInfo.GuardianAccounts.Count.ShouldBe(3);
            guardian.GuardiansInfo.GuardianAccounts[1].Value.ShouldBe(GuardianAccount1);
            guardian.GuardiansInfo.GuardianAccounts[1].Guardian.Verifier.Id.ShouldBe(_verifierId1);
            guardian.GuardiansInfo.GuardianAccounts.Last().Value.ShouldBe(GuardianAccount1);
            guardian.GuardiansInfo.GuardianAccounts.Last().Guardian.Verifier.Id.ShouldBe(_verifierId2);
        }
    }

    [Fact]
    public async Task UpdateGuardianTest_Failed_InvalidInput()
    {
        var caHash = await AddGuardianTest();
        var verificationTime = DateTime.UtcNow;
        ;
        var signature = GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);
        var signature1 =
            GenerateSignature(VerifierKeyPair1, VerifierAddress1, verificationTime, GuardianAccount1, 0);
        var signature2 =
            GenerateSignature(VerifierKeyPair2, VerifierAddress2, verificationTime, GuardianAccount1, 0);

        var guardianApprove = new List<GuardianAccountInfo>
        {
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId,
                    Signature = signature,
                    VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress.ToBase58()}",
                }
            },
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount1,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId1,
                    Signature = signature1,
                    VerificationDoc = $"{0},{GuardianAccount1},{verificationTime},{VerifierAddress1.ToBase58()}",
                }
            },
        };
        {
            var executionResult = await CaContractStub.UpdateGuardian.SendWithExceptionAsync(new UpdateGuardianInput
            {
                GuardianToUpdatePre = new GuardianAccountInfo
                {
                    Type = GuardianType.OfEmail,
                    Value = GuardianAccount1,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = _verifierId1,
                    }
                },
                GuardianToUpdateNew = new GuardianAccountInfo
                {
                    Type = GuardianType.OfEmail,
                    Value = GuardianAccount1,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = _verifierId2
                    }
                },
                GuardiansApproved = {guardianApprove}
            });
            executionResult.TransactionResult.Error.ShouldContain("Invalid input.");
        }
        {
            var executionResult = await CaContractStub.UpdateGuardian.SendWithExceptionAsync(new UpdateGuardianInput
            {
                CaHash = caHash,
                GuardianToUpdateNew = new GuardianAccountInfo
                {
                    Type = GuardianType.OfEmail,
                    Value = GuardianAccount1,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = _verifierId2
                    }
                },
                GuardiansApproved = {guardianApprove}
            });
            executionResult.TransactionResult.Error.ShouldContain("Invalid input.");
        }
        {
            var executionResult = await CaContractStub.UpdateGuardian.SendWithExceptionAsync(new UpdateGuardianInput
            {
                CaHash = caHash,
                GuardianToUpdatePre = new GuardianAccountInfo
                {
                    Type = GuardianType.OfEmail,
                    Value = GuardianAccount1,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = _verifierId1,
                    }
                },
                GuardianToUpdateNew = new GuardianAccountInfo
                {
                    Type = GuardianType.OfEmail,
                    Value = GuardianAccount1,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = _verifierId2
                    }
                },
            });
            executionResult.TransactionResult.Error.ShouldContain("Invalid input.");
        }
        {
            var executionResult = await CaContractStub.UpdateGuardian.SendWithExceptionAsync(new UpdateGuardianInput
            {
                CaHash = caHash,
                GuardianToUpdatePre = new GuardianAccountInfo
                {
                    Type = GuardianType.OfEmail,
                    Value = GuardianAccount1,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = _verifierId1,
                    }
                },
                GuardianToUpdateNew = new GuardianAccountInfo
                {
                    Type = GuardianType.OfEmail,
                    Value = GuardianAccount,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = _verifierId2
                    }
                },
                GuardiansApproved = {guardianApprove}
            });
            executionResult.TransactionResult.Error.ShouldContain("Inconsistent guardian account.");
        }

        {
            var executionResult = await CaContractStub.UpdateGuardian.SendWithExceptionAsync(new UpdateGuardianInput
            {
                CaHash = caHash,
                GuardianToUpdatePre = new GuardianAccountInfo
                {
                    Type = GuardianType.OfEmail,
                    Value = GuardianAccount1,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = _verifierId1,
                    }
                },
                GuardiansApproved = {guardianApprove}
            });
            executionResult.TransactionResult.Error.ShouldContain("Invalid input.");
        }
        {
            var executionResult = await CaContractStub.UpdateGuardian.SendWithExceptionAsync(new UpdateGuardianInput
            {
                CaHash = HashHelper.ComputeFrom("111111"),
                GuardianToUpdatePre = new GuardianAccountInfo
                {
                    Type = GuardianType.OfEmail,
                    Value = GuardianAccount1,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = _verifierId1,
                    }
                },
                GuardianToUpdateNew = new GuardianAccountInfo
                {
                    Type = GuardianType.OfEmail,
                    Value = GuardianAccount1,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = _verifierId2
                    }
                },
                GuardiansApproved = {guardianApprove}
            });
            executionResult.TransactionResult.Error.ShouldContain("CA holder does not exist.");
        }
    }

    [Fact]
    public async Task UpdateGuardian_GuardianTypeDiff_Test()
    {
        var caHash = await AddGuardianTest();
        var verificationTime = DateTime.UtcNow;
        ;
        var signature = GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);
        var signature1 =
            GenerateSignature(VerifierKeyPair1, VerifierAddress1, verificationTime, GuardianAccount1, 0);
        var signature2 =
            GenerateSignature(VerifierKeyPair2, VerifierAddress2, verificationTime, GuardianAccount1, 0);

        var guardianApprove = new List<GuardianAccountInfo>
        {
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId,
                    Signature = signature,
                    VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress.ToBase58()}"
                }
            },
            new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount1,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId1,
                    Signature = signature1,
                    VerificationDoc = $"{0},{GuardianAccount1},{verificationTime},{VerifierAddress1.ToBase58()}"
                }
            },
        };
        var result = await CaContractStub.UpdateGuardian.SendWithExceptionAsync(new UpdateGuardianInput
        {
            CaHash = caHash,
            GuardianToUpdatePre = new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId1
                }
            },
            GuardianToUpdateNew = new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount1,
                VerificationInfo = new VerificationInfo
                {
                    Id = _verifierId2
                }
            },
            GuardiansApproved = {guardianApprove}
        });
        result.TransactionResult.Error.ShouldContain("Inconsistent guardian account.");
    }
}
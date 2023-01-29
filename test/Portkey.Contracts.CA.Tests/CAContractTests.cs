using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Vote;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace Portkey.Contracts.CA;

public partial class CAContractTests : CAContractTestBase
{
     [Fact]
     public async Task CreateHolderTest()
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
                 EndPoints = { "127.0.0.1" },
                 ImageUrl = "url",
                 VerifierAddressList = { VerifierAddress }
             });
             await CaContractStub.AddVerifierServerEndPoints.SendAsync(new AddVerifierServerEndPointsInput
             {
                 Name = VerifierName1,
                 EndPoints = { "127.0.0.1" },
                 ImageUrl = "url",
                 VerifierAddressList = { VerifierAddress1 }
             });
             await CaContractStub.AddVerifierServerEndPoints.SendAsync(new AddVerifierServerEndPointsInput
             {
                 Name = VerifierName2,
                 EndPoints = { "127.0.0.1" },
                 ImageUrl = "url",
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
         var caInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput()
         {
             LoginGuardianAccount = GuardianAccount
         });
         caInfo.Managers.Count.ShouldBe(1);
         caInfo.GuardiansInfo.GuardianAccounts.Count.ShouldBe(1);

         var delegateAllowance = await TokenContractStub.GetTransactionFeeDelegationsOfADelegatee.CallAsync(new GetTransactionFeeDelegationsOfADelegateeInput()
         {
             DelegateeAddress = caInfo.CaAddress,
             DelegatorAddress = User1Address
         });
         delegateAllowance.Delegations["ELF"].ShouldBe(10000000000);
         /*var signature1 = await GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianType, 0);
         //create second caHolder
         await CaContractStub.CreateCAHolder.SendAsync(new CreateCAHolderInput
         {
             GuardianApproved = new Guardian
             {
                 GuardianType = new GuardianType
                 {
                     GuardianType_ = GuardianType,
                     Type = GuardianTypeType.GuardianTypeOfEmail
                 },
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
             LoginGuardianType = GuardianType
         });
         caHolderInfo.Managers.Count.ShouldBe(1);
         caHolderInfo.GuardiansInfo.Guardians.Count.ShouldBe(1);*/
         /*var manager = new Manager()
         {
             ManagerAddress = DefaultAddress,
             DeviceString = "iphone14-2022"
         };
         await CaContractStub.AddManager.SendAsync(new AddManagerInput()
         {
             CaHash = caHolderInfo.CaHash,
             Manager = manager
         });*/
         //Add guardian to second CaHolder By first Email;
         /*var signature3 = await GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianType, 0);
         var signature4 =
             await GenerateSignature(VerifierKeyPair1, VerifierAddress1, verificationTime, GuardianType1, 0);
         var verificationDoc = $"{0},{"1@google.com"},{verificationTime},{VerifierAddress.ToBase58()}";
         var guardianApprove = new List<Guardian>
         {
             new Guardian
             {
                 GuardianType = new GuardianType
                 {
                     GuardianType_ = GuardianType,
                     Type = GuardianTypeType.GuardianTypeOfEmail
                 },
                 Verifier = new Verifier
                 {
                     Name = VerifierName,
                     Signature = signature3,
                     VerificationDoc = verificationDoc
                 }
             }
         };
         var input = new AddGuardianInput
         {
             CaHash = caHolderInfo.CaHash,
             GuardianToAdd = new Guardian
             {
                 GuardianType = new GuardianType
                 {
                     GuardianType_ = "1@google.com",
                     Type = GuardianTypeType.GuardianTypeOfEmail
                 },
                 Verifier = new Verifier
                 {
                     Name = VerifierName1,
                     Signature = signature4,
                     VerificationDoc = $"{0},{"1@google.com"},{verificationTime},{VerifierAddress1.ToBase58()}"
                 }
             },
             GuardiansApproved = {guardianApprove}
         };
         await CaContractUser1Stub.AddGuardian.SendAsync(input);
         {
             var holderInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput
             {
                 CaHash = caHolderInfo.CaHash
             });
             holderInfo.GuardiansInfo.Guardians.Count.ShouldBe(2);
             holderInfo.GuardiansInfo.Guardians.Last().GuardianType.GuardianType_.ShouldBe(GuardianType1);
         }*/
     }

     [Fact]
     public async Task CreateHolderFailedTest()
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
         var guardian = new GuardianAccount
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
         };
         getHolderInfoOutput = await SetGuardianAccountForLogin_AndGetHolderInfo_Helper(caHash, guardian);
         var verificationTime = DateTime.UtcNow;
         var signature1 = await GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);
        await CaContractStub.CreateCAHolder.SendAsync(new CreateCAHolderInput
        {
            GuardianApproved = new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = verifierId,
                    Signature = signature1,
                    VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress.ToBase58()}"
                }
            },
            Manager = new Manager
            {
                ManagerAddress = User1Address,
                DeviceString = "123"
            }
        });
        var verifierServer = await CaContractStub.GetVerifierServers.CallAsync(new Empty());
        var id = verifierServer.VerifierServers[0].Id;
        var caHolderInfo = await CaContractStub.GetHolderInfo.CallAsync(new GetHolderInfoInput()
        {
            LoginGuardianAccount = GuardianAccount
        });
        caHolderInfo.Managers.Count.ShouldBe(2);
        caHolderInfo.GuardiansInfo.GuardianAccounts.Count.ShouldBe(2);
        caHash.ShouldBe(caHolderInfo.CaHash);

     }

     [Fact]
    public async Task CreateHolderTest_Fail_GuardianApproved_Null()
    {
        await CaContractStub.Initialize.SendAsync(new InitializeInput
        {
            ContractAdmin = DefaultAddress
        });
        var executionResult = await CaContractStub.CreateCAHolder.SendWithExceptionAsync(new CreateCAHolderInput
        {
            GuardianApproved = null,
            Manager = new Manager
            {
                ManagerAddress = User1Address,
                DeviceString = "123"
            }
        });
        executionResult.TransactionResult.Error.ShouldContain("invalid input guardian account");
    }
    
    [Fact]
    public async Task CreateHolderTest_Fail_GuardianType_Null()
    {
        await CaContractStub.Initialize.SendAsync(new InitializeInput
        {
            ContractAdmin = DefaultAddress
        });
        var verificationTime = DateTime.UtcNow;
        var signature = await GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);
        var executionResult = await CaContractStub.CreateCAHolder.SendWithExceptionAsync(new CreateCAHolderInput
        {
            GuardianApproved = new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = "",
                VerificationInfo = new VerificationInfo
                {
                    Id = new Hash(),
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
        executionResult.TransactionResult.Error.ShouldContain("invalid input guardian account");
    }
    
    [Fact]
    public async Task CreateHolderTest_Fail_Manager_Null()
    {
        await CaContractStub.Initialize.SendAsync(new InitializeInput
        {
            ContractAdmin = DefaultAddress
        });
        var verificationTime = DateTime.UtcNow;
        var signature = await GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianAccount, 0);
        var executionResult = await CaContractStub.CreateCAHolder.SendWithExceptionAsync(new CreateCAHolderInput
        {
            GuardianApproved = new GuardianAccountInfo
            {
                Type = GuardianType.OfEmail,
                Value = GuardianAccount,
                VerificationInfo = new VerificationInfo
                {
                    Id = new Hash(),
                    Signature = signature,
                    VerificationDoc = $"{0},{GuardianAccount},{verificationTime},{VerifierAddress.ToBase58()}"
                }
            },
            Manager = null
        });
        executionResult.TransactionResult.Error.ShouldContain("invalid input manager");
    }
    
     /*[Fact]
     public async Task CreateHolder_fail_invalid_guardian()
     {
         // createCaHolder
         var verificationTime = DateTime.UtcNow;
         await CaContractStub.Initialize.SendAsync(new InitializeInput
         {
             ContractAdmin = DefaultAddress
         });
         var signature = await GenerateSignature(VerifierKeyPair, VerifierAddress, verificationTime, GuardianType, 0);
         await CaContractStub.CreateCAHolder.SendAsync(new CreateCAHolderInput
         {
             GuardianApproved = new Guardian
             {
                 GuardianType = new GuardianType
                 {
                     GuardianType_ = GuardianType,
                     Type = 0
                 },
                 Verifier = new Verifier
                 {
                     Name = VerifierName,
                     Signature = signature,
                     VerificationDoc = $"{0},{GuardianType},{verificationTime},{VerifierAddress.ToBase58()}"
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
             LoginGuardianType = GuardianType
         });
         holderInfo.GuardiansInfo.Guardians.First().GuardianType.GuardianType_.ShouldBe(GuardianType);
         var caHash = holderInfo.CaHash;
         // AddGuardian
         // await AddGuardian();
         var guardianApprove = new List<Guardian>
         {
             new Guardian
             {
                 GuardianType = new GuardianType
                 {
                     GuardianType_ = GuardianType,
                     Type = 0
                 },
                 Verifier = new Verifier
                 {
                     Name = VerifierName,
                     Signature = signature,
                     VerificationDoc = $"{0},{GuardianType},{verificationTime},{VerifierAddress.ToBase58()}"
                 }
             },
             
         };
         var input = new AddGuardianInput
         {
             CaHash = caHash,
             GuardianToAdd = new Guardian
             {
                 GuardianType = new GuardianType
                 {
                     GuardianType_ = GuardianType2,
                     Type = 0
                 },
                 Verifier = new Verifier
                 {
                     Name = VerifierName,
                     Signature = signature,
                     VerificationDoc = $"{0},{GuardianType2},{verificationTime},{VerifierAddress.ToBase58()}"
                 }
             },
             GuardiansApproved = {guardianApprove}
         };
         await CaContractStub.AddGuardian.SendAsync(input);
         
         // setLoginTypeForGuardian 
         // await SetLoginGuardianType_Succeed_Test();
         await CaContractStub.SetGuardianAccountForLogin.SendAsync(new SetGuardianAccountForLoginInput
         {
             CaHash = caHash,
             GuardianType =  new GuardianType
             {
                 Type = GuardianTypeType.GuardianTypeOfEmail,
                 GuardianType_ = GuardianType1
             }
         });

         await CaContractStub.CreateCAHolder.SendAsync(new CreateCAHolderInput
         {
             
             GuardianApproved = new Guardian
             {
                 GuardianType = new GuardianType
                 {
                     GuardianType_ = "1@google.com",
                     Type = 0
                 }
             },
             Manager = new Manager
             {
                 ManagerAddress = User1Address,
                 DeviceString = "123"
             }
         });
         

     }*/

     private async Task<TransactionFeeDelegations> GetDelegator_ByCaHash_Helper(Hash caHash)
     {
         var hashCode = caHash.ToString();
         TransactionFeeDelegations delegations = await TokenContractStub.GetTransactionFeeDelegationsOfADelegatee.CallAsync(new GetTransactionFeeDelegationsOfADelegateeInput
         {
             DelegateeAddress = Address.FromBase58(hashCode),
             DelegatorAddress = null
         });
         return delegations;
     }

}
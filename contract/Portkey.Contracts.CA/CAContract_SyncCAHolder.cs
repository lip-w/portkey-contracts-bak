using System.Linq;
using AElf.Sdk.CSharp;
using AElf.Standards.ACS7;
using AElf.Types;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace Portkey.Contracts.CA;

public partial class CAContract
{
    public override Empty ValidateCAHolderInfoWithManagersExists(ValidateCAHolderInfoWithManagersExistsInput input)
    {
        Assert(input != null, "input is null");
        Assert(input!.CaHash != null, "input.CaHash is null");
        Assert(input.Managers != null, "input.Managers is null");

        var holderInfo = State.HolderInfoMap[input.CaHash];
        Assert(holderInfo != null, $"Holder by ca_hash: {input.CaHash} is not found!");

        ValidateLoginGuardianAccount(input.CaHash, holderInfo, input.LoginGuardianAccounts,
            input.NotLoginGuardianAccounts);

        var managers = input.Managers!.Distinct().ToList();

        Assert(holderInfo!.Managers.Count == managers.Count,
            "Managers set is out of time! Please GetHolderInfo again.");

        foreach (var manager in managers)
        {
            if (!CAHolderContainsManager(holderInfo.Managers, manager))
            {
                Assert(false,
                    $"Manager(address:{manager.ManagerAddress},device_string{manager.DeviceString}) is not in this CAHolder.");
            }
        }

        return new Empty();
    }

    private void ValidateLoginGuardianAccount(Hash caHash, HolderInfo holderInfo,
        RepeatedField<string> loginGuardianAccountInput,
        RepeatedField<string> notLoginGuardianAccountInput)
    {
        var loginGuardians = new RepeatedField<string>();
        foreach (var index in holderInfo.GuardiansInfo.LoginGuardianAccountIndexes)
        {
            loginGuardians.Add(holderInfo.GuardiansInfo.GuardianAccounts[index].Value);
        }

        var loginGuardianAccounts = loginGuardianAccountInput.Distinct().ToList();
        var notLoginGuardianAccounts = notLoginGuardianAccountInput.Distinct().ToList();

        Assert(loginGuardians.Count == loginGuardianAccounts.Count,
            "The amount of LoginGuardianAccountInput not equals to HolderInfo's LoginGuardianAccounts");

        foreach (var loginGuardianAccount in loginGuardianAccounts)
        {
            Assert(loginGuardians.Contains(loginGuardianAccount)
                   && State.GuardianAccountMap[loginGuardianAccount] == caHash,
                $"LoginGuardianAccount:{loginGuardianAccount} is not in HolderInfo's LoginGuardianAccounts");
        }

        foreach (var notLoginGuardianAccount in notLoginGuardianAccounts)
        {
            Assert(!loginGuardians.Contains(notLoginGuardianAccount)
                   && (State.GuardianAccountMap[notLoginGuardianAccount] == null
                       || State.GuardianAccountMap[notLoginGuardianAccount] != caHash),
                $"NotLoginGuardianAccount:{notLoginGuardianAccount} is in HolderInfo's LoginGuardianAccounts");
        }
    }

    public override Empty SyncHolderInfo(SyncHolderInfoInput input)
    {
        var originalTransaction = MethodNameVerify(input.VerificationTransactionInfo,
            nameof(ValidateCAHolderInfoWithManagersExists));
        var originalTransactionId = originalTransaction.GetHash();

        TransactionVerify(originalTransactionId, input.VerificationTransactionInfo.ParentChainHeight,
            input.VerificationTransactionInfo.FromChainId, input.VerificationTransactionInfo.MerklePath);
        var transactionInput =
            ValidateCAHolderInfoWithManagersExistsInput.Parser.ParseFrom(originalTransaction.Params);

        var holderId = transactionInput.CaHash;
        var holderInfo = State.HolderInfoMap[holderId] ?? new HolderInfo { CreatorAddress = Context.Sender };

        var managersToAdd = ManagersExcept(transactionInput.Managers, holderInfo.Managers);
        var managersToRemove = ManagersExcept(holderInfo.Managers, transactionInput.Managers);

        holderInfo.Managers.AddRange(managersToAdd);
        SetDelegators(holderId, managersToAdd);

        foreach (var manager in managersToAdd)
        {
            SetContractDelegator(manager);
        }
        
        foreach (var manager in managersToRemove)
        {
            holderInfo.Managers.Remove(manager);
        }

        RemoveDelegators(holderId, managersToRemove);

        SyncLoginGuardianAccount(transactionInput.CaHash, transactionInput.LoginGuardianAccounts,
            transactionInput.NotLoginGuardianAccounts);

        State.HolderInfoMap[holderId] = holderInfo;

        return new Empty();
    }

    private void SyncLoginGuardianAccount(Hash caHash, RepeatedField<string> loginGuardianAccounts,
        RepeatedField<string> notLoginGuardianAccounts)
    {
        if (loginGuardianAccounts != null)
        {
            foreach (var loginGuardianAccount in loginGuardianAccounts)
            {
                if (State.GuardianAccountMap[loginGuardianAccount] == null ||
                    State.GuardianAccountMap[loginGuardianAccount] != caHash)
                {
                    State.GuardianAccountMap.Set(loginGuardianAccount, caHash);
                }
            }
        }

        if (notLoginGuardianAccounts != null)
        {
            foreach (var notLoginGuardianAccount in notLoginGuardianAccounts)
            {
                if (State.GuardianAccountMap[notLoginGuardianAccount] == caHash)
                {
                    State.GuardianAccountMap.Remove(notLoginGuardianAccount);
                }
            }
        }
    }

    private RepeatedField<Manager> ManagersExcept(RepeatedField<Manager> set1, RepeatedField<Manager> set2)
    {
        RepeatedField<Manager> resultSet = new RepeatedField<Manager>();

        foreach (var manager1 in set1)
        {
            bool theSame = false;
            foreach (var manager2 in set2)
            {
                if (manager1.ManagerAddress == manager2.ManagerAddress)
                {
                    theSame = true;
                    break;
                }
            }

            if (!theSame)
            {
                resultSet.Add(manager1);
            }
        }

        return resultSet;
    }

    private Transaction MethodNameVerify(VerificationTransactionInfo info, string methodNameExpected)
    {
        var originalTransaction = Transaction.Parser.ParseFrom(info.TransactionBytes);
        Assert(originalTransaction.MethodName == methodNameExpected, $"Invalid transaction method.");

        return originalTransaction;
    }

    private void TransactionVerify(Hash transactionId, long parentChainHeight, int chainId, MerklePath merklePath)
    {
        var verificationInput = new VerifyTransactionInput
        {
            TransactionId = transactionId,
            ParentChainHeight = parentChainHeight,
            VerifiedChainId = chainId,
            Path = merklePath
        };
        //
        var crossChainAddress = Context.GetContractAddressByName(SmartContractConstants.CrossChainContractSystemName);
        var verificationResult = Context.Call<BoolValue>(crossChainAddress,
            nameof(ACS7Container.ACS7ReferenceState.VerifyTransaction), verificationInput);
        Assert(verificationResult.Value, "transaction verification failed.");
    }


    private bool CAHolderContainsManager(RepeatedField<Manager> managers, Manager targetManager)
    {
        foreach (var manager in managers)
        {
            if (manager.ManagerAddress == targetManager.ManagerAddress
                && manager.DeviceString == targetManager.DeviceString)
            {
                return true;
            }
        }

        return false;
    }
}
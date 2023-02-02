using System.Linq;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace Portkey.Contracts.CA;

public partial class CAContract
{
    // Add a guardian, if already added, return 
    public override Empty AddGuardian(AddGuardianInput input)
    {
        Assert(input.CaHash != null && input.GuardianToAdd != null && input.GuardiansApproved.Count != 0,
            "Invalid input.");
        Assert(State.HolderInfoMap[input.CaHash] != null, "CA holder does not exist.");
        Assert(State.HolderInfoMap[input.CaHash].GuardiansInfo != null, "No guardians under the holder.");
        CheckManagerPermission(input.CaHash, Context.Sender);
        var holderInfo = State.HolderInfoMap[input.CaHash];

        //Whether the guardian account to be added has already in the holder info.
        //Filter: guardianAccount.type && guardianAccount.value && Verifier.Id
        var toAddGuardian = holderInfo.GuardiansInfo.GuardianAccounts.FirstOrDefault(g =>
            g.Guardian.Type == input.GuardianToAdd.Type &&
            g.Value == input.GuardianToAdd.Value &&
            g.Guardian.Verifier.Id == input.GuardianToAdd.VerificationInfo.Id);

        if (toAddGuardian != null)
        {
            return new Empty();
        }

        //Check the verifier signature and data of the guardian to be added.
        Assert(CheckVerifierSignatureAndData(input.GuardianToAdd), "Guardian to add verification failed.");

        var guardianApprovedAmount = 0;
        var guardianApprovedList = input.GuardiansApproved.DistinctBy(g => $"{g.Type}{g.Value}{g.VerificationInfo.Id}")
            .ToList();
        foreach (var guardian in guardianApprovedList)
        {
            //Whether the guardian exists in the holder info.
            if (!IsGuardianExist(input.CaHash, guardian)) continue;
            //Check the verifier signature and data of the guardian to be approved.
            var isApproved = CheckVerifierSignatureAndData(guardian);
            if (isApproved)
            {
                guardianApprovedAmount++;
            }
        }

        //Whether the approved guardians count is satisfied.
        IsJudgementStrategySatisfied(holderInfo.GuardiansInfo.GuardianAccounts.Count, guardianApprovedAmount,
            holderInfo.JudgementStrategy);

        //var loginGuardianAccounts = GetLoginGuardianAccounts(holderInfo.GuardiansInfo);

        var guardianAdded = new GuardianAccount
        {
            Value = input.GuardianToAdd.Value,
            Guardian = new Guardian
            {
                Type = input.GuardianToAdd.Type,
                Verifier = new Verifier
                {
                    Id = input.GuardianToAdd.VerificationInfo.Id
                }
            }
        };
        State.HolderInfoMap[input.CaHash].GuardiansInfo?.GuardianAccounts.Add(guardianAdded);

        //ReIndexLoginGuardianAccount(loginGuardianAccounts, holderInfo.GuardiansInfo);


        Context.Fire(new GuardianAdded
        {
            CaHash = input.CaHash,
            CaAddress = Context.ConvertVirtualAddressToContractAddress(input.CaHash),
            GuardianAdded_ = guardianAdded
        });
        return new Empty();
    }

    // Remove a Guardian, if already removed, return 
    public override Empty RemoveGuardian(RemoveGuardianInput input)
    {
        Assert(input.CaHash != null && input.GuardianToRemove != null && input.GuardiansApproved.Count != 0,
            "Invalid input.");
        Assert(State.HolderInfoMap[input.CaHash] != null, "CA holder does not exist.");
        Assert(State.HolderInfoMap[input.CaHash].GuardiansInfo != null, "No guardians under the holder.");
        CheckManagerPermission(input.CaHash, Context.Sender);
        var holderInfo = State.HolderInfoMap[input.CaHash];
        //Select satisfied guardian to remove.
        //Filter: guardianAccount.type && guardianAccount.guardianType && Verifier.name
        var toRemoveGuardian = holderInfo.GuardiansInfo.GuardianAccounts
            .FirstOrDefault(g =>
                g.Guardian.Type == input.GuardianToRemove.Type &&
                g.Value == input.GuardianToRemove.Value &&
                g.Guardian.Verifier.Id == input.GuardianToRemove.VerificationInfo.Id);

        if (toRemoveGuardian == null)
        {
            return new Empty();
        }

        //   Get all loginGuardianAccount.
        var loginGuardianAccount = GetLoginGuardianAccounts(holderInfo.GuardiansInfo);
        //   If the guardianAccount to be removed is a loginGuardianAccount, ...
        if (loginGuardianAccount.Contains(toRemoveGuardian))
        {
            var loginGuardianAccountCount = loginGuardianAccount.Count(g => g.Value == toRemoveGuardian.Value);
            //   and it is the only one, refuse. If you really wanna to remove it, unset it first.
            Assert(loginGuardianAccountCount > 1,
                $"Cannot remove a Guardian for login, to remove it, unset it first. {input.GuardianToRemove?.Value} is a guardian account for login.");
        }

        var guardianApprovedAmount = 0;
        var guardianApprovedList = input.GuardiansApproved.DistinctBy(g => $"{g.Type}{g.Value}{g.VerificationInfo.Id}")
            .ToList();
        foreach (var guardian in guardianApprovedList)
        {
            Assert(
                !(guardian.Type == toRemoveGuardian.Guardian.Type &&
                  guardian.Value == toRemoveGuardian.Value &&
                  guardian.VerificationInfo.Id == toRemoveGuardian.Guardian.Verifier.Id),
                "Guardian approved list contains to removed guardian.");
            //Whether the guardian exists in the holder info.
            if (!IsGuardianExist(input.CaHash, guardian)) continue;
            //Check the verifier signature and data of the guardian to be approved.
            var isApproved = CheckVerifierSignatureAndData(guardian);
            if (isApproved)
            {
                guardianApprovedAmount++;
            }
        }

        //Whether the approved guardians count is satisfied.
        IsJudgementStrategySatisfied(holderInfo.GuardiansInfo.GuardianAccounts.Count.Sub(1), guardianApprovedAmount,
            holderInfo.JudgementStrategy);

        State.HolderInfoMap[input.CaHash].GuardiansInfo?.GuardianAccounts.Remove(toRemoveGuardian);

        ReIndexLoginGuardianAccount(loginGuardianAccount, holderInfo.GuardiansInfo);

        Context.Fire(new GuardianRemoved
        {
            CaHash = input.CaHash,
            CaAddress = Context.ConvertVirtualAddressToContractAddress(input.CaHash),
            GuardianRemoved_ = toRemoveGuardian
        });

        return new Empty();
    }

    private RepeatedField<GuardianAccount> GetLoginGuardianAccounts(GuardiansInfo guardiansInfo)
    {
        var loginGuardianAccounts = new RepeatedField<GuardianAccount>();
        foreach (var index in guardiansInfo.LoginGuardianAccountIndexes)
        {
            loginGuardianAccounts.Add(guardiansInfo.GuardianAccounts[index]);
        }

        return loginGuardianAccounts;
    }

    private void ReIndexLoginGuardianAccount(RepeatedField<GuardianAccount> loginGuardianAccounts,
        GuardiansInfo guardiansInfo)
    {
        guardiansInfo.LoginGuardianAccountIndexes.Clear();

        foreach (var loginGuardianAccount in loginGuardianAccounts)
        {
            FindGuardianAccountAndSet(guardiansInfo, loginGuardianAccount);
        }
    }

    public override Empty UpdateGuardian(UpdateGuardianInput input)
    {
        Assert(input.CaHash != null && input.GuardianToUpdatePre != null
                                    && input.GuardianToUpdateNew != null && input.GuardiansApproved.Count != 0,
            "Invalid input.");
        Assert(State.HolderInfoMap[input.CaHash] != null, "CA holder does not exist.");
        Assert(input.GuardianToUpdatePre?.Type == input.GuardianToUpdateNew?.Type &&
               input.GuardianToUpdatePre?.Value == input.GuardianToUpdateNew?.Value, "Inconsistent guardian account.");
        Assert(State.HolderInfoMap[input.CaHash].GuardiansInfo != null, "No guardians under the holder.");
        CheckManagerPermission(input.CaHash, Context.Sender);
        var holderInfo = State.HolderInfoMap[input.CaHash];

        //Whether the guardian account to be updated in the holder info.
        //Filter: guardianAccount.type && guardianAccount.guardianType && Verifier.name
        var existPreGuardian = holderInfo.GuardiansInfo.GuardianAccounts.FirstOrDefault(g =>
            g.Guardian.Type == input.GuardianToUpdatePre.Type &&
            g.Value == input.GuardianToUpdatePre.Value &&
            g.Guardian.Verifier.Id == input.GuardianToUpdatePre.VerificationInfo.Id);

        var toUpdateGuardian = holderInfo.GuardiansInfo.GuardianAccounts.FirstOrDefault(g =>
            g.Guardian.Type == input.GuardianToUpdateNew.Type &&
            g.Value == input.GuardianToUpdateNew.Value &&
            g.Guardian.Verifier.Id == input.GuardianToUpdateNew.VerificationInfo.Id);

        if (existPreGuardian == null || toUpdateGuardian != null)
        {
            return new Empty();
        }

        var preGuardian = existPreGuardian.Clone();

        //Check verifier id is exist.
        Assert(State.VerifiersServerList.Value.VerifierServers.FirstOrDefault(v =>
            v.Id == input.GuardianToUpdateNew.VerificationInfo.Id) != null, "Verifier is not exist.");

        var guardianApprovedAmount = 0;
        var guardianApprovedList = input.GuardiansApproved.DistinctBy(g => $"{g.Type}{g.Value}{g.VerificationInfo.Id}")
            .ToList();
        foreach (var guardian in guardianApprovedList)
        {
            Assert(
                !(guardian.Type == existPreGuardian.Guardian.Type &&
                  guardian.Value == existPreGuardian.Value &&
                  guardian.VerificationInfo.Id == existPreGuardian.Guardian.Verifier.Id),
                "Guardian approved list contains to updated guardian.");
            //Whether the guardian exists in the holder info.
            if (!IsGuardianExist(input.CaHash, guardian)) continue;
            //Check the verifier signature and data of the guardian to be approved.
            var isApproved = CheckVerifierSignatureAndData(guardian);
            if (isApproved)
            {
                guardianApprovedAmount++;
            }
        }

        //Whether the approved guardians count is satisfied.
        IsJudgementStrategySatisfied(holderInfo.GuardiansInfo.GuardianAccounts.Count.Sub(1), guardianApprovedAmount,
            holderInfo.JudgementStrategy);

        existPreGuardian.Guardian.Verifier.Id = input.GuardianToUpdateNew?.VerificationInfo.Id;

        Context.Fire(new GuardianUpdated
        {
            CaHash = input.CaHash,
            CaAddress = Context.ConvertVirtualAddressToContractAddress(input.CaHash),
            GuardianUpdatedPre = preGuardian,
            GuardianUpdatedNew = existPreGuardian
        });

        return new Empty();
    }
}
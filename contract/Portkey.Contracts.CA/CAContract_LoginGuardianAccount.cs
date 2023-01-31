using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Virgil.CryptoAPI;

namespace Portkey.Contracts.CA;

public partial class CAContract
{
    // Set a GuardianAccount for login, if already set, return ture
    public override Empty SetGuardianAccountForLogin(SetGuardianAccountForLoginInput input)
    {
        Assert(input != null, "input should not be null");
        Assert(input!.CaHash != null, "Cash should not be null");
        // GuardianAccount should be valid, not null, and be with non-null Value
        Assert(input.GuardianAccount != null, "GuardianAccount should not be null");
        Assert(!string.IsNullOrEmpty(input.GuardianAccount?.Value), "Guardian account should not be null");
        CheckManagerPermission(input.CaHash, Context.Sender);
        
        var holderInfo = State.HolderInfoMap[input.CaHash];
        Assert(holderInfo != null, $"Not found holderInfo by caHash: {input.CaHash}");
        
        var loginGuardian = input.GuardianAccount;

        var isOccupied = CheckLoginGuardianIsNotOccupied(loginGuardian, input.CaHash);

        Assert(isOccupied != CAContractConstants.LoginGuardianAccountIsOccupiedByOthers,
            $"The login guardian type --{loginGuardian.Value}-- is occupied by others!");

        // for idempotent
        if (isOccupied == CAContractConstants.LoginGuardianAccountIsYours)
        {
            return new Empty();
        }

        Assert(isOccupied == CAContractConstants.LoginGuardianAccountIsNotOccupied,
            "Internal error, how can it be?");
        Assert(holderInfo!.GuardiansInfo != null, $"No guardians found in this holder by caHash: {input.CaHash}");
        if (!LoginGuardianAccountIsInGuardians(holderInfo.GuardiansInfo!.GuardianAccounts, input.GuardianAccount))
        {
            return new Empty();
        }

        FindGuardianAccountAndSet(holderInfo.GuardiansInfo, input.GuardianAccount);

        State.LoginGuardianAccountMap[loginGuardian.Value][loginGuardian.Guardian.Verifier.Id] = input.CaHash;

        State.GuardianAccountMap[loginGuardian.Value] = input.CaHash;

        Context.Fire(new LoginGuardianAccountAdded
        {
            CaHash = input.CaHash,
            CaAddress = Context.ConvertVirtualAddressToContractAddress(input.CaHash),
            LoginGuardianAccount = input.GuardianAccount,
            Manager = Context.Sender
        });

        return new Empty();
    }

    // Unset a GuardianAccount for login, if already unset, return ture
    public override Empty UnsetGuardianAccountForLogin(UnsetGuardianAccountForLoginInput input)
    {
        Assert(input != null, "Invalid input");
        Assert(input!.CaHash != null, "CaHash can not be null");
        // GuardianAccount should be valid, not null, and be with non-null Value
        Assert(input.GuardianAccount != null, "GuardianAccount can not be null");
        Assert(!string.IsNullOrEmpty(input.GuardianAccount!.Value), "GuardianAccount. Value can not be null");
        CheckManagerPermission(input.CaHash, Context.Sender);

        var holderInfo = State.HolderInfoMap[input.CaHash];
        Assert(holderInfo != null, $"Not found holderInfo by caHash: {input.CaHash}");
        Assert(holderInfo!.GuardiansInfo != null, $"No guardians found in this holder by caHash: {input.CaHash}");
        
        // if CAHolder only have one LoginGuardian,not Allow Unset;
        Assert(holderInfo.GuardiansInfo!.LoginGuardianAccountIndexes.Count > 1,
            "only one LoginGuardian,can not be Unset");
        var loginGuardianAccount = input.GuardianAccount;
        // Try to find the index of the GuardianAccount
        var guardians = holderInfo.GuardiansInfo.GuardianAccounts;
        var index = FindGuardianAccount(guardians, loginGuardianAccount);

        // not found, quit to be idempotent
        if (index >= guardians.Count)
        {
            return new Empty();
        }

        // Remove index from LoginGuardianAccountIndexes set.
        if (!holderInfo.GuardiansInfo.LoginGuardianAccountIndexes.Contains(index))
        {
            return new Empty();
        }

        if (State.LoginGuardianAccountMap[loginGuardianAccount.Value][input.GuardianAccount.Guardian.Verifier.Id] == null
            || State.LoginGuardianAccountMap[loginGuardianAccount.Value][input.GuardianAccount.Guardian.Verifier.Id] !=
            input.CaHash)
        {
            return new Empty();
        }

        holderInfo.GuardiansInfo.LoginGuardianAccountIndexes.Remove(index);
        State.LoginGuardianAccountMap[loginGuardianAccount.Value].Remove(input.GuardianAccount.Guardian.Verifier.Id);
        // not found, or removed and be registered by others later, quit to be idempotent
        var loginGuardianList = holderInfo.GuardiansInfo.LoginGuardianAccountIndexes.Select(i => guardians[i])
            .Where(g => g.Value == loginGuardianAccount.Value).ToList();

        Context.Fire(new LoginGuardianAccountRemoved
        {
            CaHash = input.CaHash,
            CaAddress = Context.ConvertVirtualAddressToContractAddress(input.CaHash),
            LoginGuardianAccount = loginGuardianAccount,
            Manager = Context.Sender
        });
        
        if (loginGuardianList.Count == 0)
        {
            State.GuardianAccountMap.Remove(loginGuardianAccount.Value);
            Context.Fire(new LoginGuardianAccountUnbound
            {
                CaHash = input.CaHash,
                CaAddress = Context.ConvertVirtualAddressToContractAddress(input.CaHash),
                LoginGuardianAccount = loginGuardianAccount.Value,
                Manager = Context.Sender
            });
        }

        return new Empty();
    }
    
    private int CheckLoginGuardianIsNotOccupied(GuardianAccount guardianAccount, Hash caHash)
    {
        var result = State.LoginGuardianAccountMap[guardianAccount.Value][guardianAccount.Guardian.Verifier.Id];
        if (result == null)
        {
            return CAContractConstants.LoginGuardianAccountIsNotOccupied;
        }

        return result == caHash
            ? CAContractConstants.LoginGuardianAccountIsYours
            : CAContractConstants.LoginGuardianAccountIsOccupiedByOthers;
    }
    
    private void FindGuardianAccountAndSet(GuardiansInfo guardiansInfo, GuardianAccount loginGuardianAccount)
    {
        var guardians = guardiansInfo.GuardianAccounts;

        var index = FindGuardianAccount(guardians, loginGuardianAccount);

        // if index == guardians.Count, shows that it is not found and be out of bounds.
        if (index >= guardians.Count) return;
        
        // Add the index in array.
        // To be idempotent.
        if (!guardiansInfo.LoginGuardianAccountIndexes.Contains(index))
        {
            guardiansInfo.LoginGuardianAccountIndexes.Add(index);
        }
    }
    
    private int FindGuardianAccount(RepeatedField<GuardianAccount> guardianAccounts,
        GuardianAccount loginGuardianAccount)
    {
        // Find the same guardian in guardians
        // Why don't use Select((g,i) => new {g,i})? Because contract don't allow.
        var index = 0;
        foreach (var guardianAccount in guardianAccounts)
        {
            if (guardianAccount.Equals(loginGuardianAccount))
            {
                break;
            }

            index++;
        }

        return index;
    }

    private bool LoginGuardianAccountIsInGuardians(RepeatedField<GuardianAccount> guardians,
        GuardianAccount guardianAccount)
    {
        return guardians.Any(t => t .Equals(guardianAccount) );
    }
}
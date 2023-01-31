using System;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace Portkey.Contracts.CA;

public partial class CAContract
{
    // For SocialRecovery
    public override Empty SocialRecovery(SocialRecoveryInput input)
    {
        // Assert(Context.ChainId == ChainHelper.ConvertBase58ToChainId("AELF"),
        //     "Social Recovery can only be acted at AElf mainchain.");
        Assert(input != null, "invalid input");
        Assert(input!.LoginGuardianAccount != null && !string.IsNullOrEmpty(input.LoginGuardianAccount),
            "invalid input login guardian account");
        Assert(input.Manager != null, "invalid input manager");
        Assert(input.Manager!.DeviceString != null && !string.IsNullOrEmpty(input.Manager.DeviceString),
            "invalid input deviceString");
        Assert(input.Manager.ManagerAddress != null, "invalid input managerAddress");
        var loginGuardianAccount = input.LoginGuardianAccount;
        var caHash = State.GuardianAccountMap[loginGuardianAccount];

        Assert(caHash != null, "CA Holder does not exist.");

        var holderInfo = State.HolderInfoMap[caHash];
        Assert(holderInfo != null, $"Not found holderInfo by caHash: {caHash}");
        Assert(holderInfo!.GuardiansInfo != null, $"No guardians found in this holder by caHash: {caHash}");
        var guardians = holderInfo.GuardiansInfo!.GuardianAccounts;

        Assert(input.GuardiansApproved.Count > 0, "invalid input Guardians Approved");

        var guardianApprovedAmount = 0;
        var guardianApprovedList = input.GuardiansApproved.DistinctBy(g => $"{g.Type}{g.Value}{g.VerificationInfo.Id}")
            .ToList();
        foreach (var guardian in guardianApprovedList)
        {
            //Whether the guardian exists in the holder info.
            if (!IsGuardianExist(caHash, guardian)) continue;
            //Check the verifier signature and data of the guardian to be approved.
            var isApproved = CheckVerifierSignatureAndData(guardian);
            if (isApproved)
            {
                guardianApprovedAmount++;
            }
        }

        IsJudgementStrategySatisfied(guardians.Count, guardianApprovedAmount,
            holderInfo.JudgementStrategy);

        // Manager exists
        if (holderInfo.Managers.Contains(input.Manager))
        {
            return new Empty();
        }

        State.HolderInfoMap[caHash].Managers.Add(input.Manager);
        SetDelegator(caHash, input.Manager);

        Context.Fire(new ManagerSocialRecovered()
        {
            CaHash = caHash,
            CaAddress = Context.ConvertVirtualAddressToContractAddress(caHash),
            Manager = input.Manager.ManagerAddress,
            DeviceString = input.Manager.DeviceString
        });

        return new Empty();
    }

    public override Empty AddManager(AddManagerInput input)
    {
        // Assert(Context.ChainId == ChainHelper.ConvertBase58ToChainId("AELF"),
        //     "Manager can only be added at AElf mainchain.");
        Assert(input != null, "invalid input");
        CheckManagerInput(input!.CaHash, input.Manager);
        //Assert(Context.Sender.Equals(input.Manager.ManagerAddress), "No permission to add");

        var holderInfo = State.HolderInfoMap[input.CaHash];
        Assert(holderInfo != null, $"Not found holderInfo by caHash: {input.CaHash}");
        Assert(holderInfo!.GuardiansInfo != null, $"No guardians found in this holder by caHash: {input.CaHash}");

        // Manager exists
        if (holderInfo!.Managers.Contains(input.Manager))
        {
            return new Empty();
        }

        holderInfo.Managers.Add(input.Manager);
        SetDelegator(input.CaHash, input.Manager);

        Context.Fire(new ManagerAdded
        {
            CaHash = input.CaHash,
            CaAddress = Context.ConvertVirtualAddressToContractAddress(input.CaHash),
            Manager = input.Manager.ManagerAddress,
            DeviceString = input.Manager.DeviceString
        });

        return new Empty();
    }

    public override Empty RemoveManager(RemoveManagerInput input)
    {
        // Assert(Context.ChainId == ChainHelper.ConvertBase58ToChainId("AELF"),
        //     "Manager can only be removed at AElf mainchain.");
        Assert(input != null, "invalid input");
        CheckManagerInput(input!.CaHash, input.Manager);
        //Assert(Context.Sender.Equals(input.Manager.ManagerAddress), "No permission to remove");

        var holderInfo = State.HolderInfoMap[input.CaHash];
        Assert(holderInfo != null, $"Not found holderInfo by caHash: {input.CaHash}");
        Assert(holderInfo!.GuardiansInfo != null, $"No guardians found in this holder by caHash: {input.CaHash}");

        // Manager does not exist
        if (!holderInfo!.Managers.Contains(input.Manager))
        {
            return new Empty();
        }

        holderInfo.Managers.Remove(input.Manager);
        RemoveDelegator(input.CaHash, input.Manager);

        Context.Fire(new ManagerRemoved
        {
            CaHash = input.CaHash,
            CaAddress = Context.ConvertVirtualAddressToContractAddress(input.CaHash),
            Manager = input.Manager.ManagerAddress,
            DeviceString = input.Manager.DeviceString
        });

        return new Empty();
    }

    private void CheckManagerInput(Hash hash, Manager manager)
    {
        Assert(hash != null, "invalid input CaHash");
        CheckManagerPermission(hash, Context.Sender);
        Assert(manager != null, "invalid input manager");
        Assert(!string.IsNullOrEmpty(manager!.DeviceString) && manager.ManagerAddress != null, "invalid input manager");
    }

    public override Empty ManagerForwardCall(ManagerForwardCallInput input)
    {
        Assert(input.CaHash != null, "CA hash is null.");
        Assert(input.ContractAddress != null && !String.IsNullOrEmpty(input.MethodName) && !input.Args.IsEmpty,
            "Invalid input.");
        CheckManagerPermission(input.CaHash, Context.Sender);
        Context.SendVirtualInline(input.CaHash, input.ContractAddress, input.MethodName, input.Args);
        return new Empty();
    }

    public override Empty ManagerTransfer(ManagerTransferInput input)
    {
        Assert(input.CaHash != null, "CA hash is null.");
        CheckManagerPermission(input.CaHash, Context.Sender);
        Assert(input.To != null && !string.IsNullOrEmpty(input.Symbol), "Invalid input.");
        Context.SendVirtualInline(input.CaHash, State.TokenContract.Value,
            nameof(State.TokenContract.Transfer),
            new TransferInput
            {
                To = input.To,
                Amount = input.Amount,
                Symbol = input.Symbol,
                Memo = input.Memo
            }.ToByteString());
        return new Empty();
    }

    public override Empty ManagerTransferFrom(ManagerTransferFromInput input)
    {
        Assert(input.CaHash != null, "CA hash is null.");
        CheckManagerPermission(input.CaHash, Context.Sender);
        Assert(input.From != null && input.To != null && !string.IsNullOrEmpty(input.Symbol),
            "Invalid input.");
        Context.SendVirtualInline(input.CaHash, State.TokenContract.Value,
            nameof(State.TokenContract.TransferFrom),
            new TransferFromInput
            {
                From = input.From,
                To = input.To,
                Amount = input.Amount,
                Symbol = input.Symbol,
                Memo = input.Memo
            }.ToByteString());
        return new Empty();
    }

    private void CheckManagerPermission(Hash caHash, Address managerAddress)
    {
        Assert(State.HolderInfoMap[caHash] != null, $"CA holder is null.CA hash:{caHash}");
        Assert(State.HolderInfoMap[caHash].Managers.Any(m => m.ManagerAddress == managerAddress), "No permission.");
    }
}
using System.Collections.Generic;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace Portkey.Contracts.CA;

public partial class CAContract : CAContractContainer.CAContractBase
{
    public override Empty Initialize(InitializeInput input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");
        State.Admin.Value = input.ContractAdmin ?? Context.Sender;
        State.CreatorControllers.Value = new ControllerList { Controllers = { input.ContractAdmin ?? Context.Sender } };
        State.ServerControllers.Value = new ControllerList { Controllers = { input.ContractAdmin ?? Context.Sender } };
        State.TokenContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
        State.MethodFeeController.Value = new AuthorityInfo
        {
            OwnerAddress = Context.Sender
        };
        State.Initialized.Value = true;

        return new Empty();
    }

    /// <summary>
    ///     The Create method can only be executed in AElf MainChain.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override Empty CreateCAHolder(CreateCAHolderInput input)
    {
        //Assert(Context.Sender == State.RegisterOrRecoveryController.Value,"No permission.");
        Assert(State.CreatorControllers.Value.Controllers.Contains(Context.Sender), "No permission");
        Assert(input != null, "Invalid input.");
        Assert(input!.GuardianApproved != null
               && !string.IsNullOrEmpty(input.GuardianApproved.Value),
            "invalid input guardian account");
        Assert(
            input.GuardianApproved!.VerificationInfo != null, "invalid verification");
        Assert(input.Manager != null, "invalid input manager");
        var guardianAccountValue = input.GuardianApproved.Value;
        var holderId = State.GuardianAccountMap[guardianAccountValue];

        // if CAHolder exists
        if (holderId != null) return new Empty();

        var holderInfo = new HolderInfo();
        holderId = HashHelper.ConcatAndCompute(Context.TransactionId, Context.PreviousBlockHash);

        holderInfo.CreatorAddress = Context.Sender;
        holderInfo.Managers.Add(input.Manager);
        SetDelegator(holderId, input.Manager);

        //Check verifier signature.
        Assert(CheckVerifierSignatureAndData(input.GuardianApproved), "Guardian verification failed.");

        var guardianAccount = new GuardianAccount
        {
            Value = input.GuardianApproved.Value,
            Guardian = new Guardian
            {
                Type = input.GuardianApproved.Type,
                Verifier = new Verifier
                {
                    Id = input.GuardianApproved.VerificationInfo!.Id
                }
            }
        };

        holderInfo.GuardiansInfo = new GuardiansInfo
        {
            GuardianAccounts = { guardianAccount },
            LoginGuardianAccountIndexes = { 0 }
        };

        holderInfo.JudgementStrategy = input.JudgementStrategy ?? Strategy.DefaultStrategy();

        // Where is the code for double check approved guardians?
        // Don't forget to assign GuardianApprovedCount
        IsJudgementStrategySatisfied(holderInfo.GuardiansInfo.GuardianAccounts.Count, 1, holderInfo.JudgementStrategy);

        State.HolderInfoMap[holderId] = holderInfo;
        State.GuardianAccountMap[guardianAccountValue] = holderId;
        State.LoginGuardianAccountMap[guardianAccountValue][input.GuardianApproved.VerificationInfo.Id] = holderId;

        // Log Event
        Context.Fire(new CAHolderCreated
        {
            Creator = Context.Sender,
            CaHash = holderId,
            CaAddress = Context.ConvertVirtualAddressToContractAddress(holderId),
            Manager = input.Manager!.ManagerAddress,
            DeviceString = input.Manager.DeviceString
        });

        Context.Fire(new LoginGuardianAccountAdded
        {
            CaHash = holderId,
            CaAddress = Context.ConvertVirtualAddressToContractAddress(holderId),
            LoginGuardianAccount = guardianAccount,
            Manager = input.Manager.ManagerAddress,
        });

        return new Empty();
    }

    private void IsJudgementStrategySatisfied(int guardianCount, int guardianApprovedCount, StrategyNode strategyNode)
    {
        var context = new StrategyContext()
        {
            Variables = new Dictionary<string, long>()
            {
                { CAContractConstants.GuardianCount, guardianCount },
                { CAContractConstants.GuardianApprovedCount, guardianApprovedCount }
            }
        };

        var judgementStrategy = StrategyFactory.Create(strategyNode);
        Assert((bool)judgementStrategy.Validate(context),
            $"Not Satisfied criterion to create a CA Holder：" +
            $"{CAContractConstants.GuardianCount}:{guardianCount}, " +
            $"{CAContractConstants.GuardianApprovedCount}:{guardianApprovedCount}");
    }

    private void SetDelegator(Hash holderId, Manager manager)
    {
        var delegations = new Dictionary<string, long>
        {
            [CAContractConstants.ELFTokenSymbol] = CAContractConstants.CADelegationAmount
        };

        Context.SendVirtualInline(holderId, State.TokenContract.Value,
            nameof(State.TokenContract.SetTransactionFeeDelegations),
            new SetTransactionFeeDelegationsInput
            {
                DelegatorAddress = manager.ManagerAddress,
                Delegations =
                {
                    delegations
                }
            });
    }

    private void SetDelegators(Hash holderId, RepeatedField<Manager> managers)
    {
        foreach (var manager in managers)
        {
            SetDelegator(holderId, manager);
        }
    }

    private void RemoveDelegator(Hash holderId, Manager manager)
    {
        Context.SendVirtualInline(holderId, State.TokenContract.Value,
            nameof(State.TokenContract.RemoveTransactionFeeDelegator),
            new RemoveTransactionFeeDelegatorInput
            {
                DelegatorAddress = manager.ManagerAddress
            });
    }

    private void RemoveDelegators(Hash holderId, RepeatedField<Manager> managers)
    {
        foreach (var manager in managers)
        {
            RemoveDelegator(holderId, manager);
        }
    }
}
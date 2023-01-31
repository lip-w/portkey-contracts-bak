using AElf.Sdk.CSharp.State;
using AElf.Standards.ACS1;
using AElf.Types;

namespace Portkey.Contracts.CA;

public partial class CAContractState : ContractState
{
    public SingletonState<bool> Initialized { get; set; }

    // public SingletonState<Address> RegisterOrRecoveryController { get; set; }
    //
    // public SingletonState<Address> SetConfigController { get; set; }

    /// <summary>
    /// Login Guardian Account -> Verifier Id -> HolderInfo Hash
    /// multiple Login Guardian Account to one HolderInfo Hash
    /// </summary>
    public MappedState<string, Hash, Hash> LoginGuardianAccountMap { get; set; }

    /// <summary>
    /// Login Guardian Account -> HolderInfo Hash
    /// multiple Login Guardian Account to one HolderInfo Hash
    /// </summary>
    public MappedState<string, Hash> GuardianAccountMap { get; set; }

    /// <summary>
    /// HolderInfo Hash -> HolderInfo
    /// All CA contracts
    /// </summary>
    public MappedState<Hash, HolderInfo> HolderInfoMap { get; set; }

    public SingletonState<Address> Admin { get; set; }
    public SingletonState<ControllerList> CreatorControllers { get; set; }
    public SingletonState<ControllerList> ServerControllers { get; set; }

    /// <summary>
    ///  Verifier list
    /// </summary>
    public SingletonState<VerifierServerList> VerifiersServerList { get; set; }

    /// <summary>
    ///  CAServer list
    /// only on MainChain
    /// </summary>
    public SingletonState<CAServerList> CaServerList { get; set; }

    public SingletonState<AuthorityInfo> MethodFeeController { get; set; }
    public MappedState<string, MethodFees> TransactionFees { get; set; }
}
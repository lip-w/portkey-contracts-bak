using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Portkey.Contracts.TokenClaim;

public class TokenClaimContractState: ContractState
{
    internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
    
    public SingletonState<bool> Initialized { get; set; }
    public MappedState<string, long> LimitAmountMap { get; set; }
    public MappedState<string, long> IntervalMinutesMap { get; set; }

    /// <summary>
    /// Symbol -> Take Address -> Latest Take Time.
    /// </summary>
    public MappedState<string, Address, Timestamp> LatestTakeTimeMap { get; set; }
    
    public Address Admin { get; set; }
}
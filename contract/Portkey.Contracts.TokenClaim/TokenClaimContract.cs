using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Portkey.Contracts.TokenClaim;

public partial class TokenClaimContract : TokenClaimContractContainer.TokenClaimContractBase
{
    public override Empty Initialize(InitializeInput input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");

        var nativeSymbol = Context.Variables.NativeSymbol;
        State.LimitAmountMap[nativeSymbol] = input.AmountLimit == 0 ? DefaultLimitAmount : input.AmountLimit;
        State.TokenContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

        State.Admin.Value = Context.Sender;
        State.Initialized.Value = true;
        return new Empty();
    }

    public override Empty ClaimToken(ClaimTokenInput input)
    {
        Assert(State.Initialized.Value, "Uninitialized.");
        var symbol = ReturnNativeSymbolIfEmpty(input.Symbol);

        Assert(symbol == Context.Variables.NativeSymbol, "Invalid symbol.");
        Assert(State.LimitAmountMap[symbol] == input.Amount, $"Cannot take {input.Amount} from {symbol}.");
        Assert(Context.Sender == Address.FromBase58(ProxyAddress), "Invalid address");

        State.TokenContract.Transfer.Send(new TransferInput
        {
            Symbol = symbol,
            Amount = input.Amount,
            To = Address.FromBase58(ProxyAddress)
        });

        Context.Fire(new TokenClaimed
        {
            Symbol = symbol,
            Amount = input.Amount,
            User = Context.Sender
        });

        return new Empty();
    }

    private string ReturnNativeSymbolIfEmpty(string symbol)
    {
        return string.IsNullOrEmpty(symbol) ? Context.Variables.NativeSymbol : symbol;
    }
}
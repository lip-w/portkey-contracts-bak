using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace Portkey.Contracts.Take;

public partial class TakeContract : TakeContractContainer.TakeContractBase
{
    public override Empty Initialize(InitializeInput input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");

        var nativeSymbol = Context.Variables.NativeSymbol;
        State.LimitAmountMap[nativeSymbol] = input.AmountLimit == 0 ? DefaultLimitAmount : input.AmountLimit;
        State.IntervalMinutesMap[nativeSymbol] =
            input.IntervalMinutes == 0 ? DefaultIntervalMinutes : input.IntervalMinutes;
        State.TokenContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

        State.Initialized.Value = true;
        return new Empty();
    }

    public override Empty Take(TakeInput input)
    {
        var symbol = ReturnNativeSymbolIfEmpty(input.Symbol);

        Assert(symbol == Context.Variables.NativeSymbol, "Invalid symbol.");
        Assert(State.LimitAmountMap[symbol] == input.Amount, $"Cannot take {input.Amount} from {symbol}.");

        var latestTakeTime = State.LatestTakeTimeMap[symbol][Context.Sender];
        if (latestTakeTime != null)
        {
            var nextAvailableTime = latestTakeTime.AddMinutes(State.IntervalMinutesMap[symbol]);
            Assert(Context.CurrentBlockTime >= nextAvailableTime,
                $"Can take {symbol} again after {nextAvailableTime}");
        }

        State.TokenContract.Transfer.Send(new TransferInput
        {
            Symbol = symbol,
            Amount = input.Amount,
            To = Context.Sender
        });

        State.LatestTakeTimeMap[symbol][Context.Sender] = Context.CurrentBlockTime;

        Context.Fire(new Taken
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
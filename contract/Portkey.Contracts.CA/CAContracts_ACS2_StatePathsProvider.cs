using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Standards.ACS2;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Portkey.Contracts.CA;

public partial class CAContract
{
    public override ResourceInfo GetResourceInfo(Transaction txn)
    {
        switch (txn.MethodName)
        {
            case nameof(ManagerTransfer):
            {
                var args = ManagerTransferInput.Parser.ParseFrom(txn.Params);
                var resourceInfo = new ResourceInfo
                {
                    WritePaths =
                    {
                        GetPath("Balances", Context.ConvertVirtualAddressToContractAddress(args.CaHash).ToString(), args.Symbol),
                        GetPath("Balances", args.To.ToString(), args.Symbol),
                        GetPath("MethodFeeFreeAllowancesMap", txn.From.ToString()),
                        GetPath("MethodFeeFreeAllowancesLastRefreshTimeMap", txn.From.ToString()),
                        GetPath("TransactionFeeDelegateesMap", txn.From.ToString())
                    },
                    ReadPaths =
                    {
                        GetPath("TokenInfos", args.Symbol),
                        GetPath("ChainPrimaryTokenSymbol"),
                        GetPath("MethodFeeFreeAllowancesConfig")
                    }
                };
                AddPathForTransactionFee(resourceInfo, txn.From.ToString());
                AddPathForDelegatees(resourceInfo, txn.From);
                return resourceInfo;
            }

            case nameof(ManagerTransferFrom):
            {
                var args = ManagerTransferFromInput.Parser.ParseFrom(txn.Params);
                var caAddress = Context.ConvertVirtualAddressToContractAddress(args.CaHash).ToString();
                var resourceInfo = new ResourceInfo
                {
                    WritePaths =
                    {
                        GetPath("Allowances", args.From.ToString(), caAddress, args.Symbol),
                        GetPath("Balances", args.From.ToString(), args.Symbol),
                        GetPath("Balances", args.To.ToString(), args.Symbol),
                        GetPath("MethodFeeFreeAllowancesMap", txn.From.ToString()),
                        GetPath("MethodFeeFreeAllowancesLastRefreshTimeMap", txn.From.ToString()),
                        GetPath("TransactionFeeDelegateesMap", txn.From.ToString())
                    },
                    ReadPaths =
                    {
                        GetPath("TokenInfos", args.Symbol),
                        GetPath("ChainPrimaryTokenSymbol"),
                        GetPath("LockWhiteLists", args.Symbol, caAddress),
                        GetPath("MethodFeeFreeAllowancesConfig")
                    }
                };
                AddPathForTransactionFee(resourceInfo, txn.From.ToString());
                AddPathForDelegatees(resourceInfo, txn.From);
                return resourceInfo;
            }

            default:
                return new ResourceInfo { NonParallelizable = true };
        }
    }

    private void AddPathForTransactionFee(ResourceInfo resourceInfo, string from)
    {
        var symbols = GetMethodFeeSymbols();
        var paths = symbols.Select(symbol => GetPath("Balances", from, symbol));
        foreach (var path in paths)
        {
            if (resourceInfo.WritePaths.Contains(path)) continue;
            resourceInfo.WritePaths.Add(path);
        }
    }

    private ScopedStatePath GetPath(params string[] parts)
    {
        return new ScopedStatePath
        {
            Address = Context.Self,
            Path = new StatePath
            {
                Parts =
                {
                    parts
                }
            }
        };
    } 
    
    private List<string> GetMethodFeeSymbols()
    {
        var symbols = new List<string>();
        var primaryTokenSymbol = State.TokenContract.GetPrimaryTokenSymbol.Call(new Empty()).Value;
        if (primaryTokenSymbol != string.Empty) symbols.Add(primaryTokenSymbol);
        return symbols;
    }
    
    private void AddPathForDelegatees(ResourceInfo resourceInfo, Address from)
    {
        var allDelegates = State.TokenContract.GetTransactionFeeDelegatees.Call(new GetTransactionFeeDelegateesInput
        {
            DelegatorAddress = from
        });
        if (allDelegates != null)
        {
            foreach (var delegateeAddress in allDelegates.DelegateeAddresses)
            {
                var add = delegateeAddress.ToString();
                AddPathForTransactionFee(resourceInfo, add);
                resourceInfo.WritePaths.Add(GetPath("MethodFeeFreeAllowancesMap", add));
                resourceInfo.WritePaths.Add(GetPath("MethodFeeFreeAllowancesLastRefreshTimeMap", add));
            }
        }
    }
}
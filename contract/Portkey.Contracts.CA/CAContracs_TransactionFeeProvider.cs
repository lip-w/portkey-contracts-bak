using AElf.Sdk.CSharp;
using AElf.Standards.ACS1;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Org.BouncyCastle.Crypto.Agreement.Srp;

namespace Portkey.Contracts.CA;

public partial class CAContract
{
    public Empty SetMethodFee(MethodFees input)
    {
        foreach (var methodFee in input.Fees) AssertValidToken(methodFee.Symbol, methodFee.BasicFee);
        Assert(Context.Sender == State.MethodFeeController.Value.OwnerAddress, "Unauthorized to set method fee.");
        State.TransactionFees[input.MethodName] = input;
        return new Empty();
    }

    public Empty ChangeMethodFeeController(AuthorityInfo input)
    {
        AssertSenderAddressWith(State.MethodFeeController.Value.OwnerAddress);
        State.MethodFeeController.Value = input;
        return new Empty();
    }


    public  MethodFees GetMethodFee(StringValue input)
     {
         return State.TransactionFees[input.Value];
     }
    
     public  AuthorityInfo GetMethodFeeController(Empty input)
     {
         return State.MethodFeeController.Value;
     }
    
    #region private methods


    private void AssertSenderAddressWith(Address address)
    {
        Assert(Context.Sender == address, "Unauthorized behavior.");
    }
    private void AssertValidToken(string symbol, long amount)
    {
        Assert(amount >= 0, "Invalid amount.");
        if (State.TokenContract.Value == null)
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

        Assert(State.TokenContract.IsTokenAvailableForMethodFee.Call(new StringValue { Value = symbol }).Value,
            $"Token {symbol} cannot set as method fee.");
    }

    #endregion
    
}
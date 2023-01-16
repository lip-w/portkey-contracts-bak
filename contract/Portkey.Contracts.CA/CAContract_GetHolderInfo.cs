using System.Linq;
using AElf;
using AElf.Types;

namespace Portkey.Contracts.CA;

public partial class CAContract
{
    public override GetHolderInfoOutput GetHolderInfo(GetHolderInfoInput input)
    {
        Assert(input != null, "input cannot be null!");
        // CaHash and loginGuardianAccount cannot be invalid at same time.
        Assert(
            input.CaHash != null || !string.IsNullOrEmpty(input.LoginGuardianAccount.Value) &&
            input.LoginGuardianAccount.Guardian.Verifier.Id != null,
            $"CaHash is null, and loginGuardianAccount is empty: {input.CaHash}, {input.LoginGuardianAccount.Value}");

        var output = new GetHolderInfoOutput();
        HolderInfo holderInfo;
        if (input.CaHash != null)
        {
            // use ca_hash to get holderInfo
            holderInfo = State.HolderInfoMap[input.CaHash];
            Assert(holderInfo != null,
                $"Holder by ca_hash: {input.CaHash} is not found!");

            output.CaHash = input.CaHash;
            output.Managers.AddRange(holderInfo?.Managers.Clone());
        }
        else
        {
            // use loginGuardianAccount to get holderInfo
            var loginGuardianAccount = input.LoginGuardianAccount;
            var caHash =
                State.LoginGuardianAccountMap[loginGuardianAccount.Value][loginGuardianAccount.Guardian.Verifier.Id];
            Assert(caHash != null,
                $"Not found ca_hash by a the loginGuardianAccount {input.LoginGuardianAccount.Value} with VerifierId {loginGuardianAccount.Guardian.Verifier.Id}.");

            holderInfo = State.HolderInfoMap[caHash];
            Assert(holderInfo != null,
                $"Holder by ca_hash: {input.CaHash} is not found!");

            output.CaHash = caHash;
            output.Managers.AddRange(holderInfo?.Managers.Clone());
        }

        output.CaAddress = CalculateCaAddress(output.CaHash);
        output.GuardiansInfo =
            holderInfo?.GuardiansInfo == null ? new GuardiansInfo() : holderInfo.GuardiansInfo.Clone();

        return output;
    }

    private Address CalculateCaAddress(Hash caHash)
    {
        return Address.FromPublicKey(Context.Self.Value.Concat(caHash.Value.ToByteArray().ComputeHash()).ToArray());
    }
}
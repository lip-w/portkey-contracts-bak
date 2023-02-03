namespace Portkey.Contracts.CA;

public partial class CAContract
{
    public override GetHolderInfoOutput GetHolderInfo(GetHolderInfoInput input)
    {
        Assert(input != null, "input cannot be null!");
        // CaHash and loginGuardianAccount cannot be invalid at same time.
        Assert(
            input!.CaHash != null || !string.IsNullOrEmpty(input.LoginGuardianAccount),
            $"CaHash is null, or loginGuardianAccount is empty: {input.CaHash}, {input.LoginGuardianAccount}");

        var output = new GetHolderInfoOutput();

        var caHash = input.CaHash ?? State.GuardianAccountMap[input.LoginGuardianAccount];
        Assert(caHash != null,
            $"Not found ca_hash by a the loginGuardianAccount {input.LoginGuardianAccount}");
        var holderInfo = State.HolderInfoMap[caHash];
        Assert(holderInfo != null,
            $"Holder is not found");
        output.CaHash = caHash;
        output.Managers.AddRange(holderInfo?.Managers.Clone());

        output.CaAddress = Context.ConvertVirtualAddressToContractAddress(output.CaHash);
        output.GuardiansInfo =
            holderInfo?.GuardiansInfo == null ? new GuardiansInfo() : holderInfo.GuardiansInfo.Clone();

        return output;
    }
}
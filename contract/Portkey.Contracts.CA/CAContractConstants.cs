namespace Portkey.Contracts.CA;

public static class CAContractConstants
{
    public const int LoginGuardianAccountIsOccupiedByOthers = 0;

    // >1 fine, == 0 , conflict.
    public const int LoginGuardianAccountIsNotOccupied = 1;
    public const int LoginGuardianAccountIsYours = 2;

    public const string GuardianApprovedCount = "guardianApprovedCount";
    public const string GuardianCount = "guardianCount";

    public const long TenThousand = 10000;

    public const string ELFTokenSymbol = "ELF";
    public const long CADelegationAmount = 10000000000000000;

    public const long DefaultContractDelegationFee = 100;
}
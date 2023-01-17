namespace Portkey.Contracts.CA;

public static class CAContractConstants
{
    public const int LoginGuardianAccountIsOccupiedByOthers = 0;

    // >1 fine, == 0 , conflict.
    public const int LoginGuardianAccountIsNotOccupied = 1;
    public const int LoginGuardianAccountIsYours = 2;
    public const int SecondsForOneDay = 86400; // 24*60*60

    public const int And = 100;
    public const int Or = 101;
    public const int Not = 102;
    public const int IfElse = 103;
    public const int LargerThan = 104;
    public const int NotLargerThan = 105;
    public const int LessThan = 106;
    public const int NotLessThan = 107;
    public const int Equal = 108;
    public const int NotEqual = 109;
    public const int RatioByTenThousand = 110;

    public const string GuardianApprovedCount = "guardianApprovedCount";
    public const string GuardianCount = "guardianCount";

    public const long TenThousand = 10000;

    public const string ELFTokenSymbol = "ELF";
    public const int CADelegationAmount = 100000000;
}
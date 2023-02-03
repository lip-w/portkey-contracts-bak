using System;
using System.Linq;
using AElf;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Portkey.Contracts.CA;

public partial class CAContract
{
    private bool CheckVerifierSignatureAndData(GuardianAccountInfo guardianAccountInfo)
    {
        //[type,guardianAccount,verificationTime,verifierAddress]
        var verificationDoc = guardianAccountInfo.VerificationInfo.VerificationDoc;
        if (verificationDoc == null || string.IsNullOrEmpty(verificationDoc))
        {
            return false;
        }

        var verifierDoc = verificationDoc.Split(",");
        if (verifierDoc.Length != 4)
        {
            return false;
        }

        //Check expired time 1h.
        var verificationTime = DateTime.SpecifyKind(Convert.ToDateTime(verifierDoc[2]), DateTimeKind.Utc);
        if (verificationTime.ToTimestamp().AddHours(1) <= Context.CurrentBlockTime ||
            !int.TryParse(verifierDoc[0], out var type) ||
            (int)guardianAccountInfo.Type != type ||
            guardianAccountInfo.Value != verifierDoc[1])
        {
            return false;
        }

        //Check verifier address and data.
        var verifierAddress = Address.FromBase58(verifierDoc[3]);
        var verificationInfo = guardianAccountInfo.VerificationInfo;
        var verifierServer =
            State.VerifiersServerList.Value.VerifierServers.FirstOrDefault(v => v.Id == verificationInfo.Id);

        //Recovery verifier address.
        var data = HashHelper.ComputeFrom(verificationInfo.VerificationDoc);
        var publicKey = Context.RecoverPublicKey(verificationInfo.Signature.ToByteArray(),
            data.ToByteArray());
        var verifierAddressFromPublicKey = Address.FromPublicKey(publicKey);

        return verifierServer != null && verifierAddressFromPublicKey == verifierAddress &&
               verifierServer.VerifierAddresses.Contains(verifierAddress);
    }

    private bool IsGuardianExist(Hash caHash, GuardianAccountInfo guardianAccountInfo)
    {
        var satisfiedGuardians = State.HolderInfoMap[caHash].GuardiansInfo.GuardianAccounts.FirstOrDefault(
            g =>
                g.Value == guardianAccountInfo.Value &&
                g.Guardian.Type == guardianAccountInfo.Type &&
                g.Guardian.Verifier.Id == guardianAccountInfo.VerificationInfo.Id
        );
        return satisfiedGuardians != null;
    }
}
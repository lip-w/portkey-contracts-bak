using System.Linq;
using AElf;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace Portkey.Contracts.CA;

public partial class CAContract
{
    public override Empty AddVerifierServerEndPoints(AddVerifierServerEndPointsInput input)
    {
        Assert(Context.Sender.Equals(State.Admin.Value),
            "Only Admin has permission to add VerifierServerEndPoints");
        Assert(input != null, "invalid input");
        Assert(input!.Name != null && !string.IsNullOrEmpty(input.Name), "invalid input name");
        Assert(input!.EndPoints != null && input.EndPoints.Count != 0, "invalid input endPoints");
        Assert(input!.ImageUrl != null && !string.IsNullOrEmpty(input.ImageUrl), "invalid input imageUrl");
        Assert(input!.VerifierAddressList != null && input.VerifierAddressList.Count != 0,
            "invalid input verifierAddressList");

        State.VerifiersServerList.Value ??= new VerifierServerList();

        var serverId = HashHelper.ConcatAndCompute(Context.TransactionId,
            HashHelper.ComputeFrom(Context.Self));

        var verifierServerList = State.VerifiersServerList.Value;
        var server = verifierServerList.VerifierServers
            .FirstOrDefault(server => server.Name == input.Name);

        if (server == null)
        {
            State.VerifiersServerList.Value.VerifierServers.Add(new VerifierServer
            {
                Id = serverId,
                Name = input.Name,
                ImageUrl = input.ImageUrl,
                EndPoints = { input.EndPoints },
                VerifierAddress = { input.VerifierAddressList }
            });
        }
        else
        {
            var count = server.EndPoints.Count;
            foreach (var endPoint in input.EndPoints!)
            {
                if (!server.EndPoints.Contains(endPoint))
                {
                    server.EndPoints.Add(endPoint);
                }
            }

            var countAddress = server.VerifierAddress.Count;
            foreach (var address in input.VerifierAddressList!)
            {
                if (!server.VerifierAddress.Contains(address))
                {
                    server.VerifierAddress.Add(address);
                }
            }

            // Nothing added
            if (server.EndPoints.Count == count && server.VerifierAddress.Count == countAddress)
            {
                return new Empty();
            }
        }

        Context.Fire(new VerifierServerEndPointsAdded
        {
            VerifierServer = new VerifierServer
            {
                Id = serverId,
                Name = input.Name,
                ImageUrl = input.ImageUrl,
                EndPoints = { input.EndPoints },
                VerifierAddress = { input.VerifierAddressList }
            }
        });

        return new Empty();
    }

    public override Empty RemoveVerifierServerEndPoints(RemoveVerifierServerEndPointsInput input)
    {
        Assert(Context.Sender.Equals(State.Admin.Value),
            "Only Admin has permission to remove VerifierServerEndPoints");
        Assert(input != null, "invalid input");
        Assert(input!.Id != null, "invalid input id");
        Assert(input.EndPoints != null && input.EndPoints.Count != 0, "invalid input endPoints");
        if (State.VerifiersServerList.Value == null) return new Empty();

        var server = State.VerifiersServerList.Value.VerifierServers
            .FirstOrDefault(server => server.Id == input.Id);
        if (server == null)
        {
            return new Empty();
        }

        var isExist = false;
        foreach (var endPoints in input.EndPoints!)
        {
            if (server.EndPoints.Contains(endPoints))
            {
                isExist = true;
                server.EndPoints.Remove(endPoints);
            }
        }

        // Nothing removed
        if (!isExist) return new Empty();

        Context.Fire(new VerifierServerEndPointsRemoved
        {
            VerifierServer = new VerifierServer
            {
                Id = input.Id,
                EndPoints = { input.EndPoints }
            }
        });

        return new Empty();
    }

    public override Empty RemoveVerifierServer(RemoveVerifierServerInput input)
    {
        Assert(Context.Sender.Equals(State.Admin.Value),
            "Only Admin has permission to remove VerifierServer");
        Assert(input != null, "invalid input");
        Assert(input!.Id != null, "invalid input id");
        if (State.VerifiersServerList.Value == null) return new Empty();

        var server = State.VerifiersServerList.Value.VerifierServers
            .FirstOrDefault(server => server.Id == input.Id);
        if (server == null)
        {
            return new Empty();
        }

        State.VerifiersServerList.Value.VerifierServers.Remove(server);

        Context.Fire(new VerifierServerRemoved
        {
            VerifierServer = server
        });

        return new Empty();
    }

    public override GetVerifierServersOutput GetVerifierServers(Empty input)
    {
        var output = new GetVerifierServersOutput();
        var verifierServerList = State.VerifiersServerList.Value;

        if (verifierServerList != null)
        {
            output.VerifierServers.Add(verifierServerList.VerifierServers);
        }

        return output;
    }
}
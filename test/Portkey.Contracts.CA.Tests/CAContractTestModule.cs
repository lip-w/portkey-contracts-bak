using System.Collections.Generic;
using System.IO;
using AElf.Boilerplate.TestBase;
using AElf.ContractDeployer;
using AElf.Contracts.MultiToken;
using AElf.ContractTestBase;
using AElf.ContractTestBase.ContractTestKit;
using AElf.EconomicSystem;
using AElf.GovernmentSystem;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace Portkey.Contracts.CA;

[DependsOn(typeof(MainChainDAppContractTestModule))]
public class CAContractTestModule : MainChainDAppContractTestModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IBlockTimeProvider, BlockTimeProvider>();
        context.Services
            .AddSingleton<IContractDeploymentListProvider, MainChainDAppContractTestDeploymentListProvider>();
    }

    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {
        var contractCodeProvider = context.ServiceProvider.GetService<IContractCodeProvider>() ??
                                   new ContractCodeProvider();
        var contractCodes = new Dictionary<string, byte[]>(contractCodeProvider.Codes)
        {
            {
                new CAContractInitializationProvider().ContractCodeName,
                File.ReadAllBytes(typeof(CAContract).Assembly.Location)
            },
        };
        contractCodeProvider.Codes = contractCodes;
    }
}
using System.Collections.Generic;
using System.IO;
using AElf.Boilerplate.TestBase;
using AElf.ContractTestBase;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace Portkey.Contracts.TokenClaim.Tests;

[DependsOn(typeof(MainChainDAppContractTestModule))]
public class TakeContractTestModule: MainChainDAppContractTestModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IContractInitializationProvider, TakeContractInitializationProvider>();
    }

    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {
        var contractCodeProvider = context.ServiceProvider.GetService<IContractCodeProvider>();
        var contractDllLocation = typeof(TokenClaimContract).Assembly.Location;
        var contractCodes = new Dictionary<string, byte[]>(contractCodeProvider.Codes)
        {
            {
                new TakeContractInitializationProvider().ContractCodeName,
                File.ReadAllBytes(contractDllLocation)
            }
        };
        contractCodeProvider.Codes = contractCodes;
    }
}
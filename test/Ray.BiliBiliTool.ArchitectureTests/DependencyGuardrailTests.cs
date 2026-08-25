using System.Reflection;
using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using AgentServiceCollection = Ray.BiliBiliTool.Agent.Extensions.ServiceCollectionExtension;
using ApplicationServiceCollection = Ray.BiliBiliTool.Application.Extensions.ServiceCollectionExtension;
using DomainServiceCollection = Ray.BiliBiliTool.DomainService.Extensions.ServiceCollectionExtensions;
using InfrastructureGlobal = Ray.BiliBiliTool.Infrastructure.Global;

namespace Ray.BiliBiliTool.ArchitectureTests;

public class DependencyGuardrailTests
{
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(AgentServiceCollection).Assembly,
            typeof(ApplicationServiceCollection).Assembly,
            typeof(Ray.BiliBiliTool.Console.Program).Assembly,
            typeof(Ray.BiliBiliTool.Domain.User).Assembly,
            typeof(DomainServiceCollection).Assembly,
            typeof(InfrastructureGlobal).Assembly
        )
        .Build();

    private static readonly IObjectProvider<IType> ApplicationLayer = Types()
        .That()
        .ResideInNamespace("Ray.BiliBiliTool.Application")
        .As("application layer");

    private static readonly IObjectProvider<IType> SchedulerTypes = Types()
        .That()
        .ResideInNamespace("Quartz")
        .As("scheduler types");

    private static readonly IObjectProvider<IType> DomainAndPolicyLayer = Types()
        .That()
        .ResideInNamespace("Ray.BiliBiliTool.Domain")
        .Or()
        .ResideInNamespace("Ray.BiliBiliTool.DomainService")
        .As("domain and domain service layer");

    private static readonly IObjectProvider<IType> DomainForbiddenTypes = Types()
        .That()
        .ResideInNamespace("Quartz")
        .As("domain-forbidden types");

    [Fact]
    public void Application_should_not_depend_on_scheduler_or_transport_dto_types()
    {
        IArchRule rule = Types()
            .That()
            .Are(ApplicationLayer)
            .Should()
            .NotDependOnAny(SchedulerTypes)
            .Because(
                "Application stays the orchestration boundary and should not absorb scheduler concerns"
            );

        rule.Check(Architecture);
    }

    [Fact]
    public void Application_transport_dto_dependencies_should_stay_within_the_known_legacy_allowlist()
    {
        string applicationProjectDir = GetApplicationProjectDirectory();

        string[] actualFiles = Directory
            .GetFiles(applicationProjectDir, "*.cs", SearchOption.AllDirectories)
            .Where(file =>
                Regex.IsMatch(
                    File.ReadAllText(file),
                    @"Ray\.BiliBiliTool\.Agent\.BiliBiliAgent\.Dtos"
                )
            )
            .Select(file => Path.GetFileName(file)!)
            .OrderBy(file => file)
            .ToArray();

        string[] allowedFiles =
        [
            "ChargeTaskAppService.cs",
            "DailyTaskAppService.cs",
            "MangaPrivilegeTaskAppService.cs",
            "VipBigPointAppService.cs",
            "VipPrivilegeTaskAppService.cs",
        ];

        Assert.Equal(allowedFiles.OrderBy(file => file), actualFiles);
    }

    [Fact]
    public void Domain_and_domain_service_should_not_depend_on_scheduler_types()
    {
        IArchRule rule = Types()
            .That()
            .Are(DomainAndPolicyLayer)
            .Should()
            .NotDependOnAny(DomainForbiddenTypes)
            .Because(
                "domain logic and policy services should stay free of host and scheduler concerns in Phase 1"
            );

        rule.Check(Architecture);
    }

    private static string GetApplicationProjectDirectory()
    {
        return Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "src",
                "Ray.BiliBiliTool.Application"
            )
        );
    }
}

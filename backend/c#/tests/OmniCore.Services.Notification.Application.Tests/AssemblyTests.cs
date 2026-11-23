namespace OmniCore.Services.Notification.Application.Tests;

public sealed class AssemblyTests
{
    [Fact]
    public void Application_assembly_loads()
    {
        Assert.NotNull(OmniCore.Services.Notification.Application.ApplicationAssemblyReference.Assembly);
    }
}

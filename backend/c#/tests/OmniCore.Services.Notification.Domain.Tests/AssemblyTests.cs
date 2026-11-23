namespace OmniCore.Services.Notification.Domain.Tests;

public sealed class AssemblyTests
{
    [Fact]
    public void Domain_assembly_loads()
    {
        Assert.NotNull(OmniCore.Services.Notification.Domain.DomainAssemblyReference.Assembly);
    }
}

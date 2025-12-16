using Talos.Server.Controllers.V2;
using Xunit;

namespace Talos.Server.Tests.Controllers;

public class CommunityControllerTests
{
    [Fact]
    public void Constructor_Initializes()
    {
        var controller = new CommunityController();
        Assert.NotNull(controller);
    }
}

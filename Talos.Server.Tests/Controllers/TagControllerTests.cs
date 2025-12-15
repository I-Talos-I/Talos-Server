using Talos.Server.Controllers.V2;
using Xunit;

namespace Talos.Server.Tests.Controllers;

public class TagControllerTests
{
    [Fact]
    public void Constructor_Initializes()
    {
        var controller = new TagController();
        Assert.NotNull(controller);
    }
}
